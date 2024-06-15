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

        public GameDownloaderProcedures(ILauncherInfo launcherInfo, IStorageService storageService,
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

        public Task<string> DownloadGame(string version, GameLoader loader)
        {
            return _gameLoader.DownloadGame(loader, version);
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

            if (runtimeFolder is null)
            {
                throw new NotSupportedException("Operation system not supported");
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

        // private readonly ConcurrentDictionary<string, MinecraftLauncher> _launchers = new();
        //
        // private readonly ILauncherInfo _launcherInfo;
        // private readonly CustomMinecraftPath _minecraftPath;
        // private readonly IStorageService _storage;
        //
        // private IEnumerable<IVersion>? _allVersions;
        // private ConcurrentDictionary<string, IEnumerable<ForgeVersion>> _forgeVersions = new();
        // private ConcurrentDictionary<string, string> _fileHashCache = new();
        // private readonly SyncProgress<ByteProgress> _byteProgress;
        // private readonly SyncProgress<InstallerProgressChangedEventArgs> _fileProgress;
        // private VersionMetadataCollection? _vanillaVersions;
        // private IReadOnlyCollection<string>? _fabricVersions;
        // private IReadOnlyList<LiteLoaderVersion>? _liteLoaderVersions;
        // private static readonly string[] _platforms = new[] { "windows", "osx", "linux" };
        // private static readonly string[] _architectures = new[] { "32", "64", "arm", "arm64" };
        // private static string? _buildJavaPath;
        //
        // private static Dictionary<string, string[]> _javaMirrors = new()
        // {
        //     {
        //         "linux", [
        //             "https://mirror.recloud.tech/openjdk-22_linux-x64_bin.zip",
        //             "http://localhost/openjdk-22_linux-x64_bin.zip",
        //         ]
        //     },
        //     {
        //         "windows", [
        //             "https://mirror.recloud.tech/openjdk-22_windows-x64_bin.zip",
        //             "http://localhost/openjdk-22_windows-x64_bin.zip",
        //         ]
        //     },
        // };
        //
        //
        // public GameDownloaderProcedures(ILauncherInfo launcherInfo, IStorageService storage, IGameProfile profile)
        // {
        //     _launcherInfo = launcherInfo;
        //     _storage = storage;
        //
        //     if (profile == GameProfile.Empty)
        //         return;
        //
        //     foreach (var platform in _platforms)
        //     {
        //         foreach (var architecture in _architectures)
        //         {
        //             profile.ClientPath = Path.Combine(launcherInfo.InstallationDirectory, "clients", profile.Name);
        //
        //             var minecraftPath = new CustomMinecraftPath(launcherInfo.InstallationDirectory, profile.ClientPath,
        //                 platform, architecture);
        //             var launcherParameters = MinecraftLauncherParameters.CreateDefault(minecraftPath);
        //             var platformName = $"{platform}/{architecture}";
        //
        //             var platformLauncher = new MinecraftLauncher(launcherParameters);
        //             platformLauncher.RulesContext =
        //                 new RulesEvaluatorContext(new LauncherOSRule(platform, architecture, string.Empty));
        //
        //             _launchers.TryAdd(platformName, platformLauncher);
        //         }
        //     }
        //     // ToDo: Заменить на свой класс
        //     // _launcher.FileChanged += fileInfo => FileChanged?.Invoke(fileInfo.FileName ?? string.Empty);
        //     // _launcher.ProgressChanged += (sender, args) => ProgressChanged?.Invoke(sender, args);
        //
        //     _byteProgress = new SyncProgress<ByteProgress>(e =>
        //     {
        //         Debug.WriteLine(e.ToRatio() * 100 + "%");
        //         ProgressChanged?.Invoke(e.ToRatio() * 100);
        //     });
        //
        //     _fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
        //     {
        //         // Debug.WriteLine(e.Name);
        //     });
        // }
        //
        // public IVersion? InstallationVersion { get; private set; }
        //
        // public event IGameDownloaderProcedures.FileDownloadChanged? FileChanged;
        // public event IGameDownloaderProcedures.ProgressDownloadChanged? ProgressChanged;
        //
        // public async Task<string> DownloadGame(
        //     string version,
        //     GameLoader loader)
        // {
        //     await CheckBuildJava();
        //
        //     if (string.IsNullOrEmpty(version))
        //         throw new ArgumentNullException(nameof(version));
        //
        //     switch (loader)
        //     {
        //         case GameLoader.Vanilla:
        //             foreach (var launcher in _launchers.Values)
        //             {
        //                 try
        //                 {
        //                     await launcher.InstallAsync(version, _fileProgress, _byteProgress, CancellationToken.None)
        //                         .AsTask();
        //                 }
        //                 catch (Exception exception)
        //                 {
        //                     Console.WriteLine(exception);
        //                 }
        //             }
        //
        //             return version;
        //         case GameLoader.Forge:
        //
        //             string versionName = string.Empty;
        //
        //             IEnumerable<ForgeVersion>? forgeVersions = default;
        //
        //             foreach (var launcher in _launchers.Values)
        //             {
        //                 try
        //                 {
        //                     Debug.WriteLine(
        //                         $"Downloading: {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch}");
        //                     var versionMapper = new ForgeInstallerVersionMapper();
        //                     var forge = new ForgeInstaller(launcher);
        //
        //                     var minecraftVersion = version.Split('-').First();
        //                     var forgeVersion = version.Split('-')[1].Replace("Forge", string.Empty);
        //
        //                     forgeVersions ??= await forge.GetForgeVersions(minecraftVersion);
        //
        //                     var bestVersion =
        //                         forgeVersions.FirstOrDefault(v => v.ForgeVersionName == forgeVersion) ??
        //                         forgeVersions.FirstOrDefault(v => v.IsRecommendedVersion) ??
        //                         forgeVersions.FirstOrDefault(v => v.IsLatestVersion) ??
        //                         forgeVersions.FirstOrDefault() ??
        //                         throw new InvalidOperationException("Cannot find any version");
        //
        //                     versionName = await forge.Install(bestVersion, new ForgeInstallOptions
        //                     {
        //                         SkipIfAlreadyInstalled = false,
        //                         ByteProgress = _byteProgress,
        //                         JavaPath = _buildJavaPath
        //                     });
        //                 }
        //                 catch (Exception exception)
        //                 {
        //                     Console.WriteLine(exception);
        //                 }
        //             }
        //
        //
        //             //
        //             // return await forge.Install(downloadedVersion, true, (CmlLib.Core.OsType)(int)osType, osArch)
        //             //     .ConfigureAwait(false);
        //
        //             return versionName;
        //
        //             break;
        //
        //
        //         case GameLoader.Fabric:
        //
        //             var fabricLoader = new FabricInstaller(new HttpClient());
        //
        //             foreach (var launcher in _launchers.Values)
        //             {
        //                 var versionInfo = await fabricLoader.Install(version, launcher.MinecraftPath);
        //                 InstallationVersion = new MinecraftVersion(versionInfo);
        //                 await launcher.CreateProcessAsync(versionInfo, new MLaunchOption());
        //             }
        //
        //             break;
        //         case GameLoader.LiteLoader:
        //             var liteLoader = new LiteLoaderInstaller(new HttpClient());
        //
        //             _liteLoaderVersions ??= await liteLoader.GetAllLiteLoaders();
        //
        //             var bestLiteLoaderVersion = _liteLoaderVersions
        //                                             .Select(c => c)
        //                                             .Where(c => c.BaseVersion == version || c.Version == version)
        //                                             .FirstOrDefault()
        //                                         ?? throw new InvalidOperationException(
        //                                             "Выбранная версия не поддерживается");
        //
        //             foreach (var launcher in _launchers.Values)
        //             {
        //                 if (launcher.RulesContext.OS.Name == "osx")
        //                 {
        //                     continue;
        //                 }
        //
        //                 var versionMetaData = await launcher.GetVersionAsync(bestLiteLoaderVersion.BaseVersion!);
        //
        //                 var versionInfo = await liteLoader.Install(bestLiteLoaderVersion, versionMetaData, launcher.MinecraftPath);
        //                 InstallationVersion = new MinecraftVersion(versionInfo);
        //                 await launcher.InstallAndBuildProcessAsync(versionInfo, new MLaunchOption());
        //             }
        //             break;
        //
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
        //     }
        //
        //     return InstallationVersion?.Id;
        // }
        //
        // private async Task CheckBuildJava()
        // {
        //     if (_buildJavaPath is not null && File.Exists(_buildJavaPath))
        //     {
        //         return;
        //     }
        //
        //     var system = SystemService.GetPlatform();
        //
        //     var javaName = system == "windows" ? "java.exe" : "java";
        //
        //     var javaDirectory = Path.Combine(_launcherInfo.InstallationDirectory, "JavaBuild");
        //     var jdkPath = Path.Combine(javaDirectory, "jdk-22");
        //     var javaPath = Path.Combine(jdkPath, "jdk-22", "bin", javaName);
        //
        //     if (!Directory.Exists(javaDirectory) || !File.Exists(javaPath))
        //     {
        //         Directory.CreateDirectory(javaDirectory);
        //
        //         // ToDo: Вынести пинг в другой класс
        //         var mirror = await GetAvailableMirrorAsync(system);
        //
        //         _byteProgress.Report(new ByteProgress(0, 100));
        //
        //         var tempZipFilePath = Path.Combine(javaDirectory, "java.zip");
        //
        //         await DownloadFileAsync(mirror, tempZipFilePath, _byteProgress);
        //
        //         Debug.WriteLine("Download complete. Extracting...");
        //
        //         ExtractZipFile(tempZipFilePath, jdkPath);
        //
        //         Debug.WriteLine($"Extraction complete. Java executable path: {javaPath}");
        //
        //         if (system == "linux")
        //         {
        //             SetFileExecutable(javaPath);
        //         }
        //     }
        //
        //     _buildJavaPath = javaPath;
        // }
        //
        // public static void SetFileExecutable(string filePath)
        // {
        //     var process = new Process
        //     {
        //         StartInfo = new ProcessStartInfo
        //         {
        //             FileName = "chmod",
        //             Arguments = $"+x {filePath}",
        //             RedirectStandardOutput = true,
        //             RedirectStandardError = true,
        //             UseShellExecute = false,
        //             CreateNoWindow = true,
        //         }
        //     };
        //     process.Start();
        //     process.WaitForExit();
        //     if (process.ExitCode != 0)
        //     {
        //         var error = process.StandardError.ReadToEnd();
        //         Console.WriteLine($"Error setting executable permissions: {error}");
        //     }
        // }
        //
        // public static void ExtractZipFile(string zipFilePath, string extractPath)
        // {
        //     if (Directory.Exists(extractPath))
        //     {
        //         Directory.Delete(extractPath, true);
        //     }
        //
        //     ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        //     File.Delete(zipFilePath);
        // }
        //
        // public static async Task DownloadFileAsync(string url, string destinationFilePath,
        //     IProgress<ByteProgress> progress)
        // {
        //     using HttpClient client = new();
        //     using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        //     response.EnsureSuccessStatusCode();
        //
        //     var totalBytes = response.Content.Headers.ContentLength ?? 0L;
        //
        //     using Stream contentStream = await response.Content.ReadAsStreamAsync(),
        //         fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
        //             8192, true);
        //
        //     var buffer = new byte[8192];
        //     long totalRead = 0L;
        //     int bytesRead;
        //
        //
        //     while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        //     {
        //         await fileStream.WriteAsync(buffer, 0, bytesRead);
        //         totalRead += bytesRead;
        //     }
        // }
        //
        // public static async Task<string> GetAvailableMirrorAsync(string platform)
        // {
        //     if (_javaMirrors.TryGetValue(platform, out string[] mirrors))
        //     {
        //         using HttpClient client = new();
        //         foreach (string url in mirrors)
        //         {
        //             try
        //             {
        //                 HttpResponseMessage response = await client.GetAsync(url);
        //                 if (response.IsSuccessStatusCode)
        //                 {
        //                     return url;
        //                 }
        //             }
        //             catch (HttpRequestException)
        //             {
        //                 // Ignore the exception and try the next URL
        //             }
        //             catch (Exception)
        //             {
        //                 // Ignore the exception and try the next URL
        //             }
        //         }
        //     }
        //
        //     return null;
        // }
        //
        //
        // public async Task<bool> IsFullLoaded(IGameProfile baseProfile, IStartupOptions? startupOptions = null)
        // {
        //     if (await baseProfile.CheckClientExists() == false)
        //         return false;
        //
        //     if (startupOptions != null && await baseProfile.CheckOsTypeLoaded(startupOptions) == false)
        //         return false;
        //
        //     return true;
        // }
        //
        // public async Task<Process> CreateProfileProcess(IGameProfile baseProfile, IStartupOptions startupOptions,
        //     IUser user,
        //     bool forceDownload, string[]? jvmArguments)
        // {
        //     var session = new MSession(user.Name, user.AccessToken, user.Uuid);
        //
        //     if (!_launchers.TryGetValue($"{startupOptions.OsName}/{startupOptions.OsArch}", out var launcher))
        //     {
        //         throw new NotSupportedException();
        //     }
        //
        //     if (forceDownload)
        //     {
        //         foreach (var anyLauncher in _launchers.Values)
        //         {
        //             try
        //             {
        //                 await anyLauncher.CreateProcessAsync(baseProfile.LaunchVersion, new MLaunchOption
        //                 {
        //                     MinimumRamMb = startupOptions.MinimumRamMb,
        //                     MaximumRamMb = startupOptions.MaximumRamMb,
        //                     FullScreen = startupOptions.FullScreen,
        //                     ScreenHeight = startupOptions.ScreenHeight,
        //                     ScreenWidth = startupOptions.ScreenWidth,
        //                     ServerIp = startupOptions.ServerIp,
        //                     ServerPort = startupOptions.ServerPort,
        //                     Session = session
        //                 }).AsTask();
        //             }
        //             catch (Exception e)
        //             {
        //                 //ToDo: Logging
        //                 Debug.WriteLine(
        //                     $"Не удалось восстановить профиль {baseProfile.Name}, {anyLauncher.RulesContext.OS.Name}, {anyLauncher.RulesContext.OS.Arch}");
        //             }
        //         }
        //     }
        //
        //     return await launcher.BuildProcessAsync(baseProfile.LaunchVersion, new MLaunchOption
        //     {
        //         MinimumRamMb = startupOptions.MinimumRamMb,
        //         MaximumRamMb = startupOptions.MaximumRamMb,
        //         FullScreen = startupOptions.FullScreen,
        //         ScreenHeight = startupOptions.ScreenHeight,
        //         ScreenWidth = startupOptions.ScreenWidth,
        //         ServerIp = startupOptions.ServerIp,
        //         ServerPort = startupOptions.ServerPort,
        //         Session = session,
        //         PathSeparator = startupOptions.OsName == "windows" ? ";" : ":"
        //     }).AsTask();
        // }
        //
        // public Task<bool> CheckClientExists(IGameProfile baseProfile)
        // {
        //     var jarFilePath = Path.Combine(
        //         baseProfile.ClientPath,
        //         "client",
        //         baseProfile.GameVersion,
        //         $"{baseProfile.GameVersion}.jar");
        //
        //     var fileInfo = new FileInfo(jarFilePath);
        //
        //     return Task.FromResult(fileInfo.Exists);
        // }
        //
        // public Task<bool> CheckOsTypeLoaded(IGameProfile baseProfile, IStartupOptions startupOptions)
        // {
        //     var jarFilePath = Path.Combine(
        //         baseProfile.ClientPath,
        //         "client",
        //         baseProfile.GameVersion,
        //         "natives");
        //
        //
        //     return Task.FromResult(false);
        // }
        //
        // internal async Task<string> ValidateMinecraftVersion(string version, GameLoader loader)
        // {
        //     ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        //     ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 |
        //                                            SecurityProtocolType.Tls;
        //
        //     if (InstallationVersion != null)
        //         return InstallationVersion.Id;
        //
        //     switch (loader)
        //     {
        //         case GameLoader.Vanilla:
        //             InstallationVersion ??= await _launchers.First().Value.GetVersionAsync(version);
        //             break;
        //
        //         case GameLoader.Forge:
        //             var anyLauncher = _launchers.First().Value;
        //
        //             var versionMapper = new ForgeInstallerVersionMapper();
        //             var forge = new ForgeInstaller(anyLauncher);
        //
        //             var minecraftVersion = version.Split('-').First();
        //             var forgeVersion = version.Split('-').Last();
        //
        //             var forgeVersions = await forge.GetForgeVersions(minecraftVersion);
        //
        //             var bestVersion =
        //                 forgeVersions.FirstOrDefault(v => v.ForgeVersionName == forgeVersion) ??
        //                 forgeVersions.FirstOrDefault(v => v.IsRecommendedVersion) ??
        //                 forgeVersions.FirstOrDefault(v => v.IsLatestVersion) ??
        //                 forgeVersions.FirstOrDefault() ??
        //                 throw new InvalidOperationException("Cannot find any version");
        //
        //             var mappedVersion = versionMapper.CreateInstaller(bestVersion);
        //
        //             InstallationVersion = new MinecraftVersion(mappedVersion.VersionName);
        //
        //             break;
        //
        //
        //         case GameLoader.Fabric:
        //             var fabricLoader = new FabricInstaller(new HttpClient());
        //
        //             _fabricVersions ??= await fabricLoader.GetSupportedVersionNames();
        //
        //             var fabricBestVersion =
        //                 _fabricVersions.FirstOrDefault(c => c == version)
        //                 ?? throw new InvalidOperationException("Выбранная версия не поддерживается");
        //
        //             InstallationVersion = new MinecraftVersion(fabricBestVersion);
        //
        //             break;
        //         case GameLoader.LiteLoader:
        //
        //             var liteLoaderVersionLoader = new LiteLoaderInstaller(new HttpClient());
        //
        //             _liteLoaderVersions ??= await liteLoaderVersionLoader.GetAllLiteLoaders();
        //
        //             version = version
        //                 .Split('-')
        //                 .First()
        //                 .Split('_')
        //                 .First();
        //
        //             var bestLiteLoaderVersion = _liteLoaderVersions
        //                                             .Select(c => c)
        //                                             .Where(c => c.BaseVersion == version)
        //                                             .Select(c => c.Version)
        //                                             .FirstOrDefault()
        //                                         ?? throw new InvalidOperationException(
        //                                             "Выбранная версия не поддерживается");
        //
        //             InstallationVersion = new MinecraftVersion(bestLiteLoaderVersion);
        //
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
        //     }
        //
        //     return InstallationVersion.Id;
        // }
        //
        // public async Task<IEnumerable<GmlCore.Interfaces.Versions.IVersion>> GetAllVersions()
        // {
        //     // var launcherInfo = new MinecraftLauncher(new MinecraftPath());
        //     //
        //     // _allVersions ??= (await launcherInfo.GetAllVersionsAsync()).Select(c => new MineVersion
        //     //     { Name = c.Name, IsRelease = c.Type == "release" });
        //
        //     // return _allVersions;
        //     return [];
        // }
        //
        // public async Task<IEnumerable<string>> GetAllowVersions(GameLoader gameLoader, string? minecraftVersion)
        // {
        //     var anyLauncher = new MinecraftLauncher();
        //
        //     switch (gameLoader)
        //     {
        //         case GameLoader.Undefined:
        //             break;
        //         case GameLoader.Vanilla:
        //
        //             _vanillaVersions ??= await anyLauncher.GetAllVersionsAsync();
        //             return _vanillaVersions.Where(c => c.Type == "release").Select(c => c.Name);
        //
        //         case GameLoader.Forge:
        //
        //             var forge = new ForgeInstaller(anyLauncher);
        //             var versionMapper = new ForgeInstallerVersionMapper();
        //
        //             if (!_forgeVersions.Any(c => c.Key == minecraftVersion))
        //             {
        //                 _forgeVersions[minecraftVersion] = await forge.GetForgeVersions(minecraftVersion);
        //             }
        //
        //             return _forgeVersions[minecraftVersion]
        //                 .Select(c => versionMapper.CreateInstaller(c).VersionName);
        //
        //
        //             break;
        //         case GameLoader.Fabric:
        //
        //             var fabricLoader = new FabricInstaller(new HttpClient());
        //             _fabricVersions ??= await fabricLoader.GetSupportedVersionNames();
        //
        //             return _fabricVersions;
        //
        //         case GameLoader.LiteLoader:
        //             var liteLoaderVersionLoader = new LiteLoaderInstaller(new HttpClient());
        //
        //             _liteLoaderVersions ??= await liteLoaderVersionLoader.GetAllLiteLoaders();
        //
        //             return _liteLoaderVersions
        //                 .Select(c => c)
        //                 .Where(c => c.BaseVersion == minecraftVersion)
        //                 .Select(c => c.Version)!;
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(gameLoader), gameLoader, null);
        //     }
        //
        //     return [];
        // }
        //
        // public async Task<IEnumerable<IFileInfo>> GetAssets(string profileGameVersion, string osName,
        //     string osArchitecture)
        // {
        //     if (!_launchers.TryGetValue($"{osName}/{osArchitecture}", out var launcher))
        //     {
        //         throw new NotSupportedException();
        //     }
        //
        //     var files = Directory.GetFiles(launcher.MinecraftPath.Assets, "*.*", SearchOption.AllDirectories);
        //
        //     var localFilesInfo = await GetHashFiles(files, []);
        //
        //     return localFilesInfo;
        // }
        //
        // private async Task<LocalFileInfo[]> GetHashFiles(IEnumerable<string> files, string[] additionalPath)
        // {
        //     var localFilesInfo = await Task.WhenAll(files.AsParallel().Select(c =>
        //     {
        //         string hash;
        //
        //         if (_fileHashCache.TryGetValue(c, out var value))
        //         {
        //             hash = value;
        //         }
        //         else
        //         {
        //             using (var algorithm = new SHA256Managed())
        //             {
        //                 hash = SystemHelper.CalculateFileHash(c, algorithm);
        //                 _fileHashCache[c] = hash;
        //             }
        //         }
        //
        //         var path = additionalPath.Aggregate(string.Empty, Path.Combine);
        //
        //         path = Path.Combine(path, c.Replace(_launcherInfo.InstallationDirectory, string.Empty));
        //
        //         return Task.FromResult(new LocalFileInfo
        //         {
        //             Name = Path.GetFileName(c),
        //             Directory = path,
        //             FullPath = c,
        //             Size = c.Length,
        //             Hash = hash
        //         });
        //     }));
        //     return localFilesInfo;
        // }
        //
        // public async Task<IEnumerable<IFileInfo>> GetMods(string profilePath)
        // {
        //     var files = Directory.GetFiles(profilePath, "*.*", SearchOption.AllDirectories);
        //
        //     var localFilesInfo = await GetHashFiles(files, []);
        //
        //     return localFilesInfo;
        // }
        //
        // public async Task<IEnumerable<IFileInfo>> GetProfileFiles(string profileName, string profileDirectory)
        // {
        //     var files = Directory.GetFiles(profileDirectory, "*.*", SearchOption.AllDirectories);
        //
        //     var localFilesInfo = await GetHashFiles(files, ["clients", profileName]);
        //
        //     return localFilesInfo;
        // }
        //
        // public async Task<IEnumerable<IFileInfo>> GetLibraries(string libraries)
        // {
        //     var files = Directory.GetFiles(libraries, "*.*", SearchOption.AllDirectories);
        //
        //     var localFilesInfo = await GetHashFiles(files, []);
        //
        //     return localFilesInfo;
        // }
        //
        // public async Task<IEnumerable<IFileInfo>> GetJavaFiles(
        //     string profileVersion,
        //     string osName,
        //     string osArchitecture)
        // {
        //     if (!_launchers.TryGetValue($"{osName}/{osArchitecture}", out var launcher))
        //     {
        //         throw new NotSupportedException();
        //     }
        //
        //     //launcher.JavaPathResolver.GetInstalledJavaVersions()
        //
        //     var version = await launcher.GetVersionAsync(profileVersion);
        //
        //     var javaDirectory = Path.Combine(_launcherInfo.InstallationDirectory, "runtime");
        //
        //     var versionDirectory =
        //         Directory.GetDirectories(javaDirectory, $"{osName}??{osArchitecture}").FirstOrDefault();
        //
        //     if (versionDirectory is null)
        //     {
        //         throw new NotSupportedException();
        //     }
        //
        //     var files = Directory.GetFiles(versionDirectory, "*.*", SearchOption.AllDirectories);
        //
        //     var localFilesInfo = await GetHashFiles(files, []);
        //
        //     return localFilesInfo;
        // }
        //
        // public async Task<IFileInfo[]> GetAllFiles()
        // {
        //     List<string> directoryFiles = new List<string>();
        //
        //     var anyLauncher = _launchers.Values.First();
        //
        //     directoryFiles.AddRange(Directory.GetFiles(anyLauncher.MinecraftPath.Assets, "*.*",
        //         SearchOption.AllDirectories));
        //     directoryFiles.AddRange(Directory.GetFiles(anyLauncher.MinecraftPath.Runtime, "*.*",
        //         SearchOption.AllDirectories));
        //     directoryFiles.AddRange(Directory.GetFiles(Path.Combine(anyLauncher.MinecraftPath.BasePath), "*.*",
        //         SearchOption.AllDirectories));
        //
        //     var localFilesInfo = await GetHashFiles(directoryFiles, []);
        //
        //     localFilesInfo = localFilesInfo
        //         .GroupBy(c => c.Hash)
        //         .Select(c => c.First())
        //         .ToArray();
        //
        //     return localFilesInfo;
        // }
        //
        // public bool GetLauncher(string launcher, out dynamic? minecraftLauncher)
        // {
        //     var isSuccess = _launchers.TryGetValue(launcher, out var dynamicLauncher);
        //
        //     minecraftLauncher = dynamicLauncher;
        //
        //     return isSuccess;
        // }
        //
        // public async Task<IEnumerable<IFileInfo>> ConvertFiles(IEnumerable<string> directoryFiles)
        // {
        //     return await GetHashFiles(directoryFiles, []);
        // }
    }
}
