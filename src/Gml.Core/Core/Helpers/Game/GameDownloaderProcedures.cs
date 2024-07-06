using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CmlLib.Core;
using Gml.Common;
using Gml.Core.Services.Storage;
using Gml.Models.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace Gml.Core.Helpers.Game
{
    public class GameDownloaderProcedures : IGameDownloaderProcedures
    {
        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storageService;
        private readonly IGameProfile _profile;
        private Dictionary<GameLoader, Func<string, Task<string>>> _downloadMethods;
        private ConcurrentDictionary<string, string> _fileHashCache = new();

        private readonly GameDownloader _gameLoader;

        public GameDownloaderProcedures(
            ILauncherInfo launcherInfo,
            IStorageService storageService,
            IGameProfile profile)
        {
            _launcherInfo = launcherInfo;
            _storageService = storageService;
            _profile = profile;
            _gameLoader = new GameDownloader(profile, launcherInfo);
        }

        public IObservable<double> FullPercentages => _gameLoader.FullPercentages;
        public IObservable<double> LoadPercentages => _gameLoader.LoadPercentages;
        public IObservable<string> LoadLog => _gameLoader.LoadLog;
        public IObservable<Exception> LoadException => _gameLoader.LoadException;

        public Task<string> DownloadGame(string version, string? launchVersion, GameLoader loader)
        {
            return _gameLoader.DownloadGame(loader, version, launchVersion);
        }

        public async Task<Process> CreateProcess(IStartupOptions startupOptions, IUser user, bool needDownload,
            string[] jvmArguments)
        {
            Process process = await _gameLoader.GetProcessAsync(startupOptions, user, needDownload, jvmArguments);

            return process;
        }

        public async Task<IFileInfo[]> GetAllFiles()
        {
            List<string> directoryFiles = new List<string>();
            var anyLauncher = _gameLoader.AnyLauncher;

            directoryFiles.AddRange(Directory.GetFiles(anyLauncher.MinecraftPath.Assets, "*.*",
                SearchOption.AllDirectories));
            directoryFiles.AddRange(Directory.GetFiles(anyLauncher.MinecraftPath.Runtime, "*.*",
                SearchOption.AllDirectories));
            directoryFiles.AddRange(Directory.GetFiles(Path.Combine(anyLauncher.MinecraftPath.BasePath), "*.*",
                SearchOption.AllDirectories));

            var basePath = Path.Combine(anyLauncher.MinecraftPath.BasePath);
            var excludedDirectories = new[] { "client", "libraries", "resources" }
                .Select(x => Path.Combine(basePath, x))
                .ToList();

            var filesInBasePath = Directory.EnumerateFiles(basePath); // Getting files in base directory

            var allFiles = filesInBasePath
                .Concat(
                    Directory.EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                        .Where(dir => !excludedDirectories.Any(dir.StartsWith))
                        .SelectMany(Directory.EnumerateFiles)
                )
                .ToList();

            directoryFiles.AddRange(allFiles);

            var localFilesInfo = await GetHashFiles(directoryFiles, []);
            localFilesInfo = localFilesInfo
                .GroupBy(c => c.Hash)
                .Select(c => c.First())
                .ToArray();

            return localFilesInfo;
        }

        public bool GetLauncher(string launcherKey, out object launcher)
        {
            return _gameLoader.GetLauncher(launcherKey, out launcher);
        }

        public async Task<IEnumerable<IFileInfo>> GetLauncherFiles(string osName, string osArchitecture)
        {
            if (!_gameLoader.GetLauncher($"{osName}/{osArchitecture}", out var dynamicLauncher)
                || dynamicLauncher is not MinecraftLauncher launcher)
            {
                throw new NotSupportedException("Operation system not supported");
            }

            var downloadFiles = new List<IFileInfo>();
            var systemFiles = new List<string>();

            var runtimeFolder = Directory
                .GetDirectories(
                    launcher.MinecraftPath.Runtime, $"{osName}??{osArchitecture}", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(runtimeFolder) && osName == "linux")
            {
                runtimeFolder = Directory
                    .GetDirectories(
                        launcher.MinecraftPath.Runtime, $"{osName}", SearchOption.AllDirectories)
                    .FirstOrDefault();
            }

            if (runtimeFolder is null)
            {
                return [];
            }

            // All assets, ToDo: Change to current profile assets
            systemFiles.AddRange(Directory.GetFiles(launcher.MinecraftPath.Assets, "*.*",
                SearchOption.AllDirectories));

            // add runtime (Java)
            systemFiles.AddRange(Directory.GetFiles(runtimeFolder, "*.*",
                SearchOption.AllDirectories));

            // add client
            systemFiles.AddRange(
                Directory.GetFiles(launcher.MinecraftPath.Versions, "*.*", SearchOption.AllDirectories));

            // add libraries
            var librariesDirectory = Path.Combine(launcher.MinecraftPath.BasePath, "libraries", osName, osArchitecture);
            var customLibraries = Path.Combine(launcher.MinecraftPath.BasePath, "libraries", "custom");

            var basePath = Path.Combine(launcher.MinecraftPath.BasePath);
            var excludedDirectories = new[] { "client", "libraries", "resources" }
                .Select(x => Path.Combine(basePath, x))
                .ToList();

            var filesInBasePath = Directory.EnumerateFiles(basePath); // Getting files in base directory

            var allFiles = filesInBasePath
                .Concat(
                    Directory.EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                        .Where(dir => !excludedDirectories.Any(dir.StartsWith))
                        .SelectMany(Directory.EnumerateFiles)
                )
                .ToList();

            systemFiles.AddRange(allFiles);

            if (Directory.Exists(librariesDirectory))
            {
                systemFiles.AddRange(Directory.GetFiles(librariesDirectory, "*.*", SearchOption.AllDirectories));
            }

            if (Directory.Exists(customLibraries))
            {
                systemFiles.AddRange(Directory.GetFiles(customLibraries, "*.*", SearchOption.AllDirectories));
            }

            return await GetHashFiles(systemFiles, []);
        }

        private async Task<LocalFileInfo[]> GetHashFiles(IEnumerable<string> files, string[] additionalPath)
        {
            var localFilesInfo = await Task.WhenAll(files.AsParallel().Select(c =>
            {
                string hash;
                if (_fileHashCache.TryGetValue(c, out var value))
                {
                    hash = value;
                }
                else
                {
                    using (var algorithm = new SHA256Managed())
                    {
                        hash = SystemHelper.CalculateFileHash(c, algorithm);
                        _fileHashCache[c] = hash;
                    }
                }

                var path = additionalPath.Aggregate(string.Empty, Path.Combine);
                path = Path.Combine(path, c.Replace(_launcherInfo.InstallationDirectory, string.Empty));
                return Task.FromResult(new LocalFileInfo
                {
                    Name = Path.GetFileName(c),
                    Directory = path,
                    FullPath = c,
                    Size = c.Length,
                    Hash = hash
                });
            }));
            return localFilesInfo;
        }
    }
}
