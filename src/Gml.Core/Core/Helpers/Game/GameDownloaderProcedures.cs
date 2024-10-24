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
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Models.System;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Sentry;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace Gml.Core.Helpers.Game
{
    public class GameDownloaderProcedures : IGameDownloaderProcedures
    {
        private readonly ILauncherInfo _launcherInfo;
        private readonly GameDownloader _gameLoader;
        private readonly IStorageService _storageService;
        private readonly IGameProfile _profile;
        private readonly IBugTrackerProcedures _bugTracker;
        private Dictionary<GameLoader, Func<string, Task<string>>> _downloadMethods;
        private ConcurrentDictionary<string, string> _fileHashCache = new();

        public IObservable<double> FullPercentages => _gameLoader.FullPercentages;
        public IObservable<double> LoadPercentages => _gameLoader.LoadPercentages;
        public IObservable<string> LoadLog => _gameLoader.LoadLog;
        public IObservable<Exception> LoadException => _gameLoader.LoadException;

        public GameDownloaderProcedures(ILauncherInfo launcherInfo,
            IStorageService storageService,
            IGameProfile profile,
            INotificationProcedures notifications, IBugTrackerProcedures bugTracker)
        {
            _launcherInfo = launcherInfo;
            _storageService = storageService;
            _profile = profile;
            _bugTracker = bugTracker;
            _gameLoader = new GameDownloader(profile, launcherInfo, notifications);

            LoadException.Subscribe(CaptureException);
        }

        private void CaptureException(Exception bug)
        {
            var bugInfo = _bugTracker.CaptureException(bug);

            _bugTracker.CaptureException(bugInfo);
        }

        public Task<string> DownloadGame(string version, string? launchVersion, GameLoader loader, IBootstrapProgram? bootstrapProgram)
        {
            return _gameLoader.DownloadGame(loader, version, launchVersion, bootstrapProgram);
        }

        public async Task<Process> CreateProcess(IStartupOptions startupOptions, IUser user, bool needDownload,
            string[] jvmArguments, string[] gameArguments)
        {
            var process = await _gameLoader.GetProcessAsync(startupOptions, user, needDownload, jvmArguments, gameArguments);

            return process;
        }

        public async Task<IFileInfo[]> GetAllFiles(bool needRestoreCache = false)
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

            var localFilesInfo = await GetHashFiles(directoryFiles, [], needRestoreCache);

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

        public async Task<ICollection<IFileInfo>> GetLauncherFiles(string osName, string osArchitecture)
        {
            if (!_gameLoader.GetLauncher($"{osName}/{osArchitecture}", out var dynamicLauncher)
                || dynamicLauncher is not MinecraftLauncher launcher)
            {
                throw new NotSupportedException("Operation system not supported");
            }

            var downloadFiles = new List<IFileInfo>();
            var systemFiles = new List<string>();

            if (!GetJavaRuntimeFolder(osName, osArchitecture, launcher, out var runtimeFolder))
                return [];

            if (!GetAssetsFolder(osName, osArchitecture, launcher, out var assetsFolder))
                return [];

            if (!GetLibrariesFolder(osName, osArchitecture, launcher, out var librariesDirectory))
                return [];

            if (!GetCustomLibrariesFolder(launcher, out var customLibraries))
                return [];

            // All assets, ToDo: Change to current profile assets
            systemFiles.AddRange(Directory.GetFiles(assetsFolder, "*.*", SearchOption.AllDirectories));

            // add runtime (Java)
            systemFiles.AddRange(Directory.GetFiles(runtimeFolder, "*.*", SearchOption.AllDirectories));

            // add client
            systemFiles.AddRange(
                Directory.GetFiles(launcher.MinecraftPath.Versions, "*.*", SearchOption.AllDirectories));

            // add libraries
            var basePath = Path.Combine(launcher.MinecraftPath.BasePath);
            var excludedDirectories = new[] { "client", "libraries" }
                .Select(x => Path.Combine(basePath, x))
                .ToList();

            var filesInBasePath = Directory.EnumerateFiles(basePath); // Getting files in base directory

            var otherFiles = filesInBasePath
                .Concat(
                    Directory.EnumerateDirectories(basePath, "*.*", SearchOption.AllDirectories)
                        .Where(dir => !excludedDirectories.Any(dir.StartsWith))
                        .SelectMany(Directory.EnumerateFiles)
                )
                .ToList();

            systemFiles.AddRange(otherFiles);

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

        private bool GetCustomLibrariesFolder(MinecraftLauncher launcher, out string folder)
        {
            folder = Path.Combine(launcher.MinecraftPath.BasePath, "libraries", "custom");
            return true;
        }

        private bool GetLibrariesFolder(string osName, string osArchitecture, MinecraftLauncher launcher, out string folder)
        {
            folder = Path.Combine(launcher.MinecraftPath.BasePath, "libraries", osName, osArchitecture);

            return true;
        }

        private bool GetAssetsFolder(string osName, string osArchitecture, MinecraftLauncher launcher, out string assetsFolder)
        {
            assetsFolder = launcher.MinecraftPath.Assets;

            return true;
        }

        private static bool GetJavaRuntimeFolder(string osName, string osArchitecture, MinecraftLauncher launcher,
            out string runtimeFolder)
        {
            runtimeFolder = Directory
                .GetDirectories(
                    launcher.MinecraftPath.Runtime, $"{osName}??{osArchitecture}", SearchOption.AllDirectories)
                .FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(runtimeFolder) && osName == "linux")
            {
                runtimeFolder = Directory
                    .GetDirectories(
                        launcher.MinecraftPath.Runtime, $"{osName}", SearchOption.AllDirectories)
                    .FirstOrDefault() ?? string.Empty;
            }

            return !string.IsNullOrEmpty(runtimeFolder);
        }

        private async Task<LocalFileInfo[]> GetHashFiles(IEnumerable<string> files, string[] additionalPath,
            bool needRestoreCache = false)
        {
            var localFilesInfo = await Task.WhenAll(files.AsParallel().Select(c =>
            {
                string hash;
                if (!needRestoreCache && _fileHashCache.TryGetValue(c, out var value))
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
                    Size = new FileInfo(c).Length,
                    Hash = hash
                });
            }));
            return localFilesInfo;
        }
    }
}
