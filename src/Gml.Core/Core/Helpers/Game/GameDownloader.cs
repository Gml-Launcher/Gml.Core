using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.LiteLoader;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Rules;
using CmlLib.Core.Version;
using Gml.Core.Services.System;
using Gml.Models.CmlLib;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.User;

namespace Gml.Core.Helpers.Game;

public class GameDownloader
{
    private readonly IGameProfile _profile;
    private readonly ILauncherInfo _launcherInfo;

    public IObservable<double> FullPercentages => _fullPercentages;
    public IObservable<double> LoadPercentages => _loadPercentages;
    public IObservable<string> LoadLog => _loadLog;
    public IObservable<Exception> LoadException => _exception;
    public MinecraftLauncher AnyLauncher => _launchers.Values.First();

    private readonly ConcurrentDictionary<string, MinecraftLauncher> _launchers = new();
    private readonly string[] _platforms = { "windows", "osx", "linux" };
    private readonly string[] _architectures = { "32", "64", "arm", "arm64" };
    private Subject<double> _loadPercentages = new();
    private Subject<double> _fullPercentages = new();
    private Subject<string> _loadLog = new();
    private Subject<Exception> _exception = new();
    private SyncProgress<ByteProgress> _byteProgress;
    private SyncProgress<InstallerProgressChangedEventArgs> _fileProgress;
    private Dictionary<GameLoader, Func<string, string?, CancellationToken, Task<string>>> _downloadMethods;
    private CancellationTokenSource _cancellationTokenSource;
    private string? _buildJavaPath;
    private int _steps;
    private int _currentStep;



    private static Dictionary<string, string[]> _javaMirrors = new()
    {
        {
            "linux", [
                "https://mirror.recloud.tech/openjdk-22_linux-x64_bin.zip",
                "https://mirror.recloud.host/openjdk-22_linux-x64_bin.zip",
                "https://mr-1.recloud.tech/openjdk-22_linux-x64_bin.zip",
                "http://localhost/openjdk-22_linux-x64_bin.zip",
            ]
        },
        {
            "windows", [
                "https://mirror.recloud.tech/openjdk-22_windows-x64_bin.zip",
                "https://mirror.recloud.host/openjdk-22_windows-x64_bin.zip",
                "https://mr-1.recloud.tech/openjdk-22_windows-x64_bin.zip",
                "http://localhost/openjdk-22_windows-x64_bin.zip",
            ]
        },
    };

    public GameDownloader(IGameProfile profile, ILauncherInfo launcherInfo)
    {
        _downloadMethods = new Dictionary<GameLoader, Func<string, string?, CancellationToken, Task<string>>>
        {
            { GameLoader.Vanilla, DownloadVanilla },
            { GameLoader.Forge, DownloadForge },
            { GameLoader.Fabric, DownloadFabric },
            { GameLoader.LiteLoader, DownloadLiteLoader },
            { GameLoader.NeoForge, DownloadNeoForge }
        };

        _profile = profile;
        _launcherInfo = launcherInfo;

        _byteProgress = new SyncProgress<ByteProgress>(e =>
        {
            _loadPercentages.OnNext(e.ToRatio() * 100);
        });

        var progressSubject = new Subject<string>();

        progressSubject
            .Buffer(TimeSpan.FromSeconds(2))
            .Select(items => string.Join(Environment.NewLine, items))
            .Subscribe(combinedText =>
            {
                if (!string.IsNullOrEmpty(combinedText))
                {
                    _loadLog.OnNext(combinedText);
                }
            });

        _fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
        {
            if (e.Name != null)
                progressSubject.OnNext($"[{DateTime.Now:HH:m:ss:fff}] [INFO] {e.Name}");
        });

        foreach (var platform in _platforms)
        {
            foreach (var architecture in _architectures)
            {
                profile.ClientPath = Path.Combine(launcherInfo.InstallationDirectory, "clients", profile.Name);
                var minecraftPath = new CustomMinecraftPath(launcherInfo.InstallationDirectory, profile.ClientPath,
                    platform, architecture);
                var launcherParameters = MinecraftLauncherParameters.CreateDefault(minecraftPath);
                var platformName = $"{platform}/{architecture}";
                var platformLauncher = new MinecraftLauncher(launcherParameters);
                platformLauncher.RulesContext =
                    new RulesEvaluatorContext(new LauncherOSRule(platform, architecture, string.Empty));
                _launchers.TryAdd(platformName, platformLauncher);
            }
        }
    }

    public async Task<string> DownloadGame(GameLoader loader, string version, string? launchVersion)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _profile.State = ProfileState.Loading;

        if (!_downloadMethods.ContainsKey(loader))
            throw new ArgumentOutOfRangeException(nameof(loader), loader, null);

        _loadPercentages.OnNext(0);
        _fullPercentages.OnNext(0);
        _steps = _launchers.Count + 1;
        _currentStep = 0;

        await CheckBuildJava();
        OnStep();

        return await _downloadMethods[loader](version, launchVersion, _cancellationTokenSource.Token);
    }

    private void OnStep()
    {
        double percentage = (double)_currentStep / _steps * 100;

        _fullPercentages.OnNext(percentage);

        _currentStep++;
    }

    private async Task<string> DownloadVanilla(string version, string? launchVersion, CancellationToken cancellationToken)
    {
        foreach (var launcher in _launchers.Values)
        {
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                await launcher.InstallAsync(version, _fileProgress, _byteProgress, cancellationToken)
                    .AsTask();
            }
            catch (Exception exception)
            {
                _exception.OnNext(exception);
            }
            finally
            {
                OnStep();
            }
        }

        return await Task.FromResult(version);
    }

    private async Task<string> DownloadForge(string version, string? launchVersion, CancellationToken cancellationToken)
    {
        _loadLog.OnNext("Load starting...");
        string loadVersion = string.Empty;
        ForgeVersion? bestVersion = default;
        ForgeVersion[]? forgeVersions = default;

        foreach (var launcher in _launchers.Values)
        {
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                var forge = new ForgeInstaller(launcher);

                forgeVersions ??= (await forge.GetForgeVersions(version)).ToArray();

                bestVersion ??=
                    forgeVersions.FirstOrDefault(v => v.ForgeVersionName == launchVersion) ??
                    forgeVersions.FirstOrDefault(v => v.IsRecommendedVersion) ??
                    forgeVersions.FirstOrDefault(v => v.IsLatestVersion) ??
                    forgeVersions.FirstOrDefault();

                if (bestVersion is null)
                {
                    throw new InvalidOperationException("Cannot find any version");
                }

                loadVersion = await forge.Install(bestVersion, new ForgeInstallOptions
                {
                    SkipIfAlreadyInstalled = false,
                    ByteProgress = _byteProgress,
                    FileProgress = _fileProgress,
                    JavaPath = _buildJavaPath,
                    CancellationToken = cancellationToken
                });

                // var process = await launcher.CreateProcessAsync(loadVersion, new MLaunchOption()).AsTask();
            }
            catch (Exception exception)
            {
                _exception.OnNext(exception);
                _loadLog.OnNext(
                    $"Launcher for {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} not installed!");
            }
            finally
            {
                OnStep();
            }
        }

        return await Task.FromResult(loadVersion);
    }

    private async Task<string> DownloadNeoForge(string version, string? launchVersion, CancellationToken cancellationToken)
    {
        _loadLog.OnNext("Load starting...");
        string loadVersion = string.Empty;
        NeoForgeVersion? bestVersion = default;
        NeoForgeVersion[]? forgeVersions = default;

        foreach (var launcher in _launchers.Values)
        {
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                var forge = new NeoForgeInstaller(launcher);

                forgeVersions ??= (await forge.GetForgeVersions(version)).ToArray();

                launchVersion = launchVersion!.Split("-").Last();

                bestVersion ??=
                    forgeVersions.FirstOrDefault(v => v.VersionName == launchVersion) ??
                    forgeVersions.FirstOrDefault();

                if (bestVersion is null)
                {
                    throw new InvalidOperationException("Cannot find any version");
                }

                loadVersion = await forge.Install(bestVersion, new NeoForgeInstallOptions
                {
                    SkipIfAlreadyInstalled = false,
                    ByteProgress = _byteProgress,
                    FileProgress = _fileProgress,
                    JavaPath = _buildJavaPath,
                    CancellationToken = cancellationToken
                });

                var process = await launcher.CreateProcessAsync(loadVersion, new MLaunchOption()).AsTask();
            }
            catch (Exception exception)
            {
                _exception.OnNext(exception);
                _loadLog.OnNext(
                    $"Launcher for {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} not installed!");
            }
            finally
            {
                OnStep();
            }
        }

        return await Task.FromResult(loadVersion);
    }

    private async Task<string> DownloadFabric(string version, string? launchVersion, CancellationToken cancellationToken)
    {
        var versionInfo = string.Empty;
        var fabricLoader = new FabricInstaller(new HttpClient());

        foreach (var launcher in _launchers.Values)
        {
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                versionInfo = await fabricLoader.Install(version, launcher.MinecraftPath);

                await launcher.InstallAndBuildProcessAsync(
                    versionInfo, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
            }
            catch (Exception exception)
            {
                _exception.OnNext(exception);
                _loadLog.OnNext(
                    $"Launcher for {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} not installed!");
            }
            finally
            {
                OnStep();
            }
        }

        return versionInfo;
    }

    private async Task<string> DownloadLiteLoader(string version, string? launchVersion, CancellationToken cancellationToken)
    {
        var versionInfo = string.Empty;
        var liteLoader = new LiteLoaderInstaller(new HttpClient());
        var liteLoaderVersions = await liteLoader.GetAllLiteLoaders();

        foreach (var launcher in _launchers.Values)
        {

            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                var bestLiteLoaderVersion = liteLoaderVersions
                                                .Select(c => c)
                                                .Where(c => c.BaseVersion == version || c.Version == version)
                                                .FirstOrDefault()
                                            ?? throw new InvalidOperationException(
                                                "Выбранная версия не поддерживается");

                if (launcher.RulesContext.OS.Name == "osx")
                {
                    continue;
                }

                var versionMetaData = await launcher.GetVersionAsync(bestLiteLoaderVersion.BaseVersion!, cancellationToken);
                versionInfo = await liteLoader.Install(bestLiteLoaderVersion, versionMetaData, launcher.MinecraftPath);
                await launcher.InstallAndBuildProcessAsync(
                    versionInfo, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
            }
            catch (Exception exception)
            {
                _exception.OnNext(exception);
                _loadLog.OnNext(
                    $"Launcher for {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} not installed!");
            }
            finally
            {
                OnStep();
            }
        }

        return versionInfo;
    }

    private async Task CheckBuildJava()
    {
        if (_buildJavaPath is not null && File.Exists(_buildJavaPath))
        {
            return;
        }

        var system = SystemService.GetPlatform();
        var javaName = system == "windows" ? "java.exe" : "java";
        var javaDirectory = Path.Combine(_launcherInfo.InstallationDirectory, "JavaBuild");
        var jdkPath = Path.Combine(javaDirectory, "jdk-22");
        var javaPath = Path.Combine(jdkPath, "jdk-22", "bin", javaName);
        if (!Directory.Exists(javaDirectory) || !File.Exists(javaPath))
        {
            _loadLog.OnNext("Java not installed...");
            Directory.CreateDirectory(javaDirectory);
            // ToDo: Вынести пинг в другой класс
            _loadLog.OnNext("Get Java mirror...");
            var mirror = await GetAvailableMirrorAsync(system);
            _byteProgress.Report(new ByteProgress(0, 100));
            var tempZipFilePath = Path.Combine(javaDirectory, "java.zip");
            _loadLog.OnNext("Start downloading java...");
            await DownloadFileAsync(mirror, tempZipFilePath);
            _loadLog.OnNext("Download complete. Extracting...");
            ExtractZipFile(tempZipFilePath, jdkPath);
            _loadLog.OnNext($"Extraction complete. Java executable path: {javaPath}");
            if (system == "linux")
            {
                SetFileExecutable(javaPath);
            }
        }

        _buildJavaPath = javaPath;
    }

    public async Task<string> GetAvailableMirrorAsync(string platform)
    {
        if (_javaMirrors.TryGetValue(platform, out string[] mirrors))
        {
            using HttpClient client = new();
            foreach (string url in mirrors)
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return url;
                    }
                }
                catch (HttpRequestException)
                {
                    // Ignore the exception and try the next URL
                }
                catch (Exception)
                {
                    // Ignore the exception and try the next URL
                }
            }
        }

        return null;
    }

    public async Task DownloadFileAsync(string url, string destinationFilePath)
    {
        using HttpClient client = new();
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using Stream contentStream = await response.Content.ReadAsStreamAsync(),
            fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
                8192, true);
        var buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
        }
    }
    public void ExtractZipFile(string zipFilePath, string extractPath)
    {
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }
        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        File.Delete(zipFilePath);
    }

    public void SetFileExecutable(string filePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x {filePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            Console.WriteLine($"Error setting executable permissions: {error}");
        }
    }

    public async Task<Process> GetProcessAsync(IStartupOptions startupOptions, IUser user, bool needDownload, string[] jvmArguments)
    {
        if (!_launchers.TryGetValue($"{startupOptions.OsName}/{startupOptions.OsArch}", out var launcher))
        {
            throw new NotSupportedException("Operation system not supported");
        }

        var session = new MSession(user.Name, user.AccessToken, user.Uuid);

        if (needDownload)
        {
            foreach (var anyLauncher in _launchers.Values)
            {
                try
                {
                    await anyLauncher.InstallAndBuildProcessAsync(_profile.LaunchVersion, new MLaunchOption
                    {
                        MinimumRamMb = startupOptions.MinimumRamMb,
                        MaximumRamMb = startupOptions.MaximumRamMb,
                        FullScreen = startupOptions.FullScreen,
                        ScreenHeight = startupOptions.ScreenHeight,
                        ScreenWidth = startupOptions.ScreenWidth,
                        ServerIp = startupOptions.ServerIp,
                        ServerPort = startupOptions.ServerPort,
                        Session = session,
                        ExtraJvmArguments = jvmArguments.Select(c => new MArgument(c))
                    }).AsTask();
                }
                catch (Exception exception)
                {
                    _exception.OnNext(exception);
                    _loadLog.OnNext(
                        $"Не удалось восстановить профиль {_profile.Name}, {anyLauncher.RulesContext.OS.Name}, {anyLauncher.RulesContext.OS.Arch}");
                }
            }
        }

        return await launcher.BuildProcessAsync(_profile.LaunchVersion, new MLaunchOption
        {
            MinimumRamMb = startupOptions.MinimumRamMb,
            MaximumRamMb = startupOptions.MaximumRamMb,
            FullScreen = startupOptions.FullScreen,
            ScreenHeight = startupOptions.ScreenHeight,
            ScreenWidth = startupOptions.ScreenWidth,
            ServerIp = startupOptions.ServerIp,
            ServerPort = startupOptions.ServerPort,
            Session = session,
            PathSeparator = startupOptions.OsName == "windows" ? ";" : ":",
            ExtraJvmArguments = jvmArguments.Select(c => new MArgument(c))
        }).AsTask();
    }

    public bool GetLauncher(string launcherKey, out object launcher)
    {
        var isSuccess = _launchers.TryGetValue(launcherKey, out var returnLauncher);

        launcher = returnLauncher;

        return isSuccess;
    }
}
