using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.NeoForge.Installers;
using CmlLib.Core.Installer.NeoForge.Versions;
using CmlLib.Core.Installers;
using CmlLib.Core.Java;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.LiteLoader;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Rules;
using CmlLib.Core.Version;
using Gml.Core.Helpers.Mirrors;
using Gml.Core.Services.System;
using Gml.Models.CmlLib;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.User;
using ICSharpCode.SharpZipLib.Zip;

namespace Gml.Core.Helpers.Game;

public class GameDownloader
{
    private readonly string[] _architectures = { "32", "64", "arm", "arm64" };
    private readonly ILauncherInfo _launcherInfo;

    private readonly ConcurrentDictionary<string, MinecraftLauncher> _launchers = new();
    private readonly INotificationProcedures _notifications;
    private readonly string[] _platforms = { "windows", "osx", "linux" };
    private readonly IGameProfile _profile;
    private readonly ISystemProcedures _systemProcedures;
    private string? _buildJavaPath;
    private readonly SyncProgress<ByteProgress> _byteProgress;
    private CancellationTokenSource _cancellationTokenSource;
    private IBootstrapProgram? _bootstrapProgram;
    private int _currentStep;
    private readonly Dictionary<GameLoader, Func<string, string?, CancellationToken, Task<string>>> _downloadMethods;
    private readonly Subject<Exception> _exception = new();
    private readonly SyncProgress<InstallerProgressChangedEventArgs> _fileProgress;
    private readonly Subject<double> _fullPercentages = new();
    private readonly Subject<string> _loadLog = new();
    private readonly Subject<double> _loadPercentages = new();
    private int _steps;

    public GameDownloader(IGameProfile profile, ILauncherInfo launcherInfo, INotificationProcedures notifications)
    {
        _downloadMethods = new Dictionary<GameLoader, Func<string, string?, CancellationToken, Task<string>>>
        {
            { GameLoader.Vanilla, DownloadVanilla },
            { GameLoader.Forge, DownloadForge },
            { GameLoader.Fabric, DownloadFabric },
            { GameLoader.Quilt, DownloadQuilt },
            { GameLoader.LiteLoader, DownloadLiteLoader },
            { GameLoader.NeoForge, DownloadNeoForge }
        };

        _profile = profile;
        _launcherInfo = launcherInfo;
        _notifications = notifications;
        _systemProcedures = launcherInfo.Settings.SystemProcedures;

        _byteProgress = new SyncProgress<ByteProgress>(e => { _loadPercentages.OnNext(e.ToRatio() * 100); });

        var progressSubject = new Subject<string>();

        progressSubject
            .Merge(_systemProcedures.DownloadLogs)
            .Buffer(TimeSpan.FromMilliseconds(300))
            .Select(items => string.Join(Environment.NewLine, items))
            .Subscribe(combinedText =>
            {
                if (!string.IsNullOrEmpty(combinedText)) _loadLog.OnNext(combinedText);
            });

        _fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
        {
            if (!string.IsNullOrEmpty(e.Name))
                progressSubject.OnNext($"[{DateTime.Now:HH:m:ss:fff}] [INFO] {e.Name}");
        });

        foreach (var platform in _platforms)
        foreach (var architecture in _architectures)
        {
            profile.ClientPath = Path.Combine(launcherInfo.InstallationDirectory, "clients", profile.Name);
            var minecraftPath = new CustomMinecraftPath(launcherInfo.InstallationDirectory, profile.ClientPath,
                platform, architecture);
            var launcherParameters = MinecraftLauncherParameters.CreateDefault(minecraftPath);
            var platformName = $"{platform}/{architecture}";
            var platformLauncher = new MinecraftLauncher(launcherParameters)
            {
                RulesContext = new RulesEvaluatorContext(new LauncherOSRule(platform, architecture, string.Empty))
            };

            _launchers.TryAdd(platformName, platformLauncher);
        }
    }

    public IObservable<double> FullPercentages => _fullPercentages;
    public IObservable<double> LoadPercentages => _loadPercentages;
    public IObservable<string> LoadLog => _loadLog;
    public IObservable<Exception> LoadException => _exception;
    public MinecraftLauncher AnyLauncher => _launchers.Values.First();

    public async Task<string> DownloadGame(GameLoader loader, string version, string? launchVersion,
        IBootstrapProgram? bootstrapProgram)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _profile.State = ProfileState.Loading;

        if (!_downloadMethods.ContainsKey(loader))
        {
            await _notifications.SendMessage("Ошибка", "Попытка создать профиль с неверным загрузчиком", NotificationType.Error);
            throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
        }

        _bootstrapProgram = bootstrapProgram;
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
        var percentage = (double)_currentStep / _steps * 100;

        _fullPercentages.OnNext(percentage);

        _currentStep++;
    }

    private async Task<string> DownloadVanilla(string version, string? launchVersion,
        CancellationToken cancellationToken)
    {
        IVersion? installVersion = default;

        foreach (var launcher in _launchers.Values)
            try
            {
                if (_bootstrapProgram is not null && installVersion is null)
                {
                    installVersion = await launcher.GetVersionAsync(version, cancellationToken).AsTask();
                    // installVersion.ChangeJavaVersion(new JavaVersion(_bootstrapProgram.Name, _bootstrapProgram.MajorVersion));
                }

                installVersion ??= new MinecraftVersion(version);

                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                await launcher.InstallAsync(installVersion, _fileProgress, _byteProgress, cancellationToken)
                    .AsTask();
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
            {
                var title = $"{_profile.Name} {launcher.RulesContext.OS.Name} [{launcher.RulesContext.OS.Arch}] пропущен";
                var details =
                    $"Создание профиля {_profile.Name} пропущено для операционной системы {launcher.RulesContext.OS.Name} " +
                    $"с архитектурой {launcher.RulesContext.OS.Arch}. " +
                    $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                await _notifications.SendMessage(title, details, NotificationType.Warn);
                _exception.OnNext(exception);
                _loadLog.OnNext(details);
            }
            catch (Exception exception)
            {
                await _notifications.SendMessage("Ошибка", exception);
                _exception.OnNext(exception);
            }
            finally
            {
                OnStep();
            }

        return await Task.FromResult(version);
    }

    private async Task<string> DownloadForge(string version, string? launchVersion, CancellationToken cancellationToken)
    {
        _loadLog.OnNext("Load starting...");
        var loadVersion = string.Empty;
        ForgeVersion? bestVersion = default;
        ForgeVersion[]? forgeVersions = default;
        JavaVersion? javaVersion = default;

        foreach (var launcher in _launchers.Values)
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
                    await _notifications.SendMessage("Ошибка", "Не удалось определить версию для загрузки",
                        NotificationType.Error);
                    throw new InvalidOperationException("Cannot find any version");
                }

                if (javaVersion is null && _bootstrapProgram is not null)
                {
                    javaVersion = new JavaVersion(_bootstrapProgram.Name, _bootstrapProgram.MajorVersion.ToString());
                }

                loadVersion = await forge.Install(bestVersion, new ForgeInstallOptions
                {
                    SkipIfAlreadyInstalled = false,
                    ByteProgress = _byteProgress,
                    FileProgress = _fileProgress,
                    JavaPath = _buildJavaPath,
                    CancellationToken = cancellationToken,
                    // JavaVersion = javaVersion
                });
                // var process = await launcher.CreateProcessAsync(loadVersion, new MLaunchOption()).AsTask();
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
            {
                var title = $"{_profile.Name} {launcher.RulesContext.OS.Name} [{launcher.RulesContext.OS.Arch}] пропущен";
                var details =
                    $"Создание профиля {_profile.Name} пропущено для операционной системы {launcher.RulesContext.OS.Name} " +
                    $"с архитектурой {launcher.RulesContext.OS.Arch}. " +
                    $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                await _notifications.SendMessage(title, details, NotificationType.Warn);
                _exception.OnNext(exception);
                _loadLog.OnNext(details);
            }
            catch (Exception exception)
            {
                var message =
                    $"Клиент для {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} не был установлен!";
                await _notifications.SendMessage(message, exception);
                _exception.OnNext(exception);
                _loadLog.OnNext(message);
            }
            finally
            {
                OnStep();
            }

        return await Task.FromResult(loadVersion);
    }

    private async Task<string> DownloadNeoForge(string version, string? launchVersion,
        CancellationToken cancellationToken)
    {
        _loadLog.OnNext("Load starting...");
        var loadVersion = string.Empty;
        NeoForgeVersion? bestVersion = default;
        NeoForgeVersion[]? forgeVersions = default;
        JavaVersion? javaVersion = default;

        foreach (var launcher in _launchers.Values)
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
                    await _notifications.SendMessage("Ошибка", "Не удалось определить версию для загрузки",
                        NotificationType.Error);
                    throw new InvalidOperationException("Cannot find any version");
                }


                if (javaVersion is null && _bootstrapProgram is not null)
                {
                    javaVersion = new JavaVersion(_bootstrapProgram.Name, _bootstrapProgram.MajorVersion.ToString());
                }

                loadVersion = await forge.Install(bestVersion, new NeoForgeInstallOptions
                {
                    SkipIfAlreadyInstalled = false,
                    ByteProgress = _byteProgress,
                    FileProgress = _fileProgress,
                    JavaPath = _buildJavaPath,
                    CancellationToken = cancellationToken,
                    // JavaVersion = javaVersion
                });

                var process = await launcher.CreateProcessAsync(loadVersion, new MLaunchOption()).AsTask();
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
            {
                var title = $"{_profile.Name} {launcher.RulesContext.OS.Name} [{launcher.RulesContext.OS.Arch}] пропущен";
                var details =
                    $"Создание профиля {_profile.Name} пропущено для операционной системы {launcher.RulesContext.OS.Name} " +
                    $"с архитектурой {launcher.RulesContext.OS.Arch}. " +
                    $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                await _notifications.SendMessage(title, details, NotificationType.Warn);
                _exception.OnNext(exception);
                _loadLog.OnNext(details);
            }
            catch (Exception exception)
            {
                var message =
                    $"Клиент для {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} не был установлен!";
                await _notifications.SendMessage(message, exception);
                _exception.OnNext(exception);
                _loadLog.OnNext(message);
            }
            finally
            {
                OnStep();
            }

        return await Task.FromResult(loadVersion);
    }

    private async Task<string> DownloadFabric(string version, string? launchVersion,
        CancellationToken cancellationToken)
    {
        var versionName = string.Empty;
        var fabricLoader = new FabricInstaller(new HttpClient());
        FabricLoader? fabricVersion = default;
        JavaVersion? javaVersion = default;
        IVersion? downloadVersion = default;

        foreach (var launcher in _launchers.Values)
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");

                if (launcher.Versions?.FirstOrDefault(c => c.Name == launchVersion) is {} hasVersion)
                {

                    versionName = launchVersion;
                    await launcher.InstallAndBuildProcessAsync(hasVersion.Name, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
                    continue;
                }

                if (fabricVersion is null)
                {
                    var versionLoaders = await fabricLoader.GetLoaders(version);
                    fabricVersion = versionLoaders.First(c => c.Version == launchVersion);
                }

                versionName = await fabricLoader.Install(version, fabricVersion.Version!, launcher.MinecraftPath);
                downloadVersion ??= await launcher.GetVersionAsync(versionName,  cancellationToken);

                if (javaVersion is null && _bootstrapProgram is not null)
                {
                    javaVersion = new JavaVersion(_bootstrapProgram.Name, _bootstrapProgram.MajorVersion.ToString());
                    // downloadVersion.ChangeJavaVersion(javaVersion);
                    // downloadVersion.ParentVersion?.ChangeJavaVersion(javaVersion);
                }

                await launcher.InstallAndBuildProcessAsync(versionName, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
            {
                var title = $"{_profile.Name} {launcher.RulesContext.OS.Name} [{launcher.RulesContext.OS.Arch}] пропущен";
                var details =
                    $"Создание профиля {_profile.Name} пропущено для операционной системы {launcher.RulesContext.OS.Name} " +
                    $"с архитектурой {launcher.RulesContext.OS.Arch}. " +
                    $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                await _notifications.SendMessage(title, details, NotificationType.Warn);
                _exception.OnNext(exception);
                _loadLog.OnNext(details);
            }
            catch (Exception exception)
            {
                var message =
                    $"Клиент для {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} не был установлен!";
                await _notifications.SendMessage(message, exception);
                _exception.OnNext(exception);
                _loadLog.OnNext(message);
            }
            finally
            {
                OnStep();
            }

        return versionName;
    }

    private async Task<string> DownloadQuilt(string version, string? launchVersion,
        CancellationToken cancellationToken)
    {
        var versionName = string.Empty;
        var quiltInstaller = new QuiltInstaller(new HttpClient());
        QuiltLoader? fabricVersion = default;
        JavaVersion? javaVersion = default;
        IVersion? downloadVersion = default;

        foreach (var launcher in _launchers.Values)
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");

                if (launcher.Versions?.FirstOrDefault(c => c.Name == launchVersion) is {} hasVersion)
                {
                    versionName = launchVersion;
                    await launcher.InstallAndBuildProcessAsync(hasVersion.Name, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
                    continue;
                }

                if (fabricVersion is null)
                {
                    var versionLoaders = await quiltInstaller.GetLoaders(version);
                    fabricVersion = versionLoaders.First(c => c.Version == launchVersion);
                }

                versionName = await quiltInstaller.Install(version, fabricVersion.Version!, launcher.MinecraftPath);
                downloadVersion ??= await launcher.GetVersionAsync(versionName,  cancellationToken);

                if (javaVersion is null && _bootstrapProgram is not null)
                {
                    javaVersion = new JavaVersion(_bootstrapProgram.Name, _bootstrapProgram.MajorVersion.ToString());
                    // downloadVersion.ChangeJavaVersion(javaVersion);
                    // downloadVersion.ParentVersion?.ChangeJavaVersion(javaVersion);
                }

                await launcher.InstallAndBuildProcessAsync(versionName, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
            {
                var title = $"{_profile.Name} {launcher.RulesContext.OS.Name} [{launcher.RulesContext.OS.Arch}] пропущен";
                var details =
                    $"Создание профиля {_profile.Name} пропущено для операционной системы {launcher.RulesContext.OS.Name} " +
                    $"с архитектурой {launcher.RulesContext.OS.Arch}. " +
                    $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                await _notifications.SendMessage(title, details, NotificationType.Warn);
                _exception.OnNext(exception);
                _loadLog.OnNext(details);
            }
            catch (Exception exception)
            {
                var message =
                    $"Клиент для {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} не был установлен!";
                await _notifications.SendMessage(message, exception);
                _exception.OnNext(exception);
                _loadLog.OnNext(message);
            }
            finally
            {
                OnStep();
            }

        return versionName;
    }

    private async Task<string> DownloadLiteLoader(string version, string? launchVersion,
        CancellationToken cancellationToken)
    {
        var versionName = string.Empty;
        JavaVersion? javaVersion = default;
        IVersion? downloadVersion = default;
        var liteLoader = new LiteLoaderInstaller(new HttpClient());
        var liteLoaderVersions = await liteLoader.GetAllLiteLoaders();

        foreach (var launcher in _launchers.Values)
            try
            {
                _loadLog.OnNext($"Downloading: {launcher.RulesContext.OS.Name}, arch: {launcher.RulesContext.OS.Arch}");
                var bestLiteLoaderVersion = liteLoaderVersions
                                                .Select(c => c)
                                                .Where(c => c.BaseVersion == version || c.Version == version)
                                                .FirstOrDefault()
                                            ?? throw new InvalidOperationException(
                                                "Выбранная версия не поддерживается");

                if (launcher.RulesContext.OS.Name == "osx") continue;

                var versionMetaData =
                    await launcher.GetVersionAsync(bestLiteLoaderVersion.BaseVersion!, cancellationToken);
                versionName = await liteLoader.Install(bestLiteLoaderVersion, versionMetaData, launcher.MinecraftPath);

                downloadVersion ??= await launcher.GetVersionAsync(versionName,  cancellationToken);

                if (javaVersion is null && _bootstrapProgram is not null)
                {
                    javaVersion = new JavaVersion(_bootstrapProgram.Name, _bootstrapProgram.MajorVersion.ToString());
                    // downloadVersion.ChangeJavaVersion(javaVersion);
                    // downloadVersion.ParentVersion?.ChangeJavaVersion(javaVersion);
                }

                await launcher.InstallAndBuildProcessAsync(versionName, new MLaunchOption(), _fileProgress, _byteProgress, cancellationToken);
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
            {
                var title = $"{_profile.Name} {launcher.RulesContext.OS.Name} [{launcher.RulesContext.OS.Arch}] пропущен";
                var details =
                    $"Создание профиля {_profile.Name} пропущено для операционной системы {launcher.RulesContext.OS.Name} " +
                    $"с архитектурой {launcher.RulesContext.OS.Arch}. " +
                    $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                await _notifications.SendMessage(title, details, NotificationType.Warn);
                _exception.OnNext(exception);
                _loadLog.OnNext(details);
            }
            catch (Exception exception)
            {
                var message =
                    $"Клиент для {launcher.RulesContext.OS.Name}, {launcher.RulesContext.OS.Arch} не был установлен!";
                await _notifications.SendMessage(message, exception);
                _exception.OnNext(exception);
                _loadLog.OnNext(message);
            }
            finally
            {
                OnStep();
            }

        return versionName;
    }

    private async Task CheckBuildJava()
    {
        if (_buildJavaPath is not null && File.Exists(_buildJavaPath)) return;

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
            var mirrorUrl = await _systemProcedures.GetAvailableMirrorAsync(MirrorsHelper.JavaMirrors);
            _byteProgress.Report(new ByteProgress(0, 100));
            var tempZipFilePath = Path.Combine(javaDirectory, "java.zip");
            _loadLog.OnNext("Start downloading java...");
            await _systemProcedures.DownloadFileAsync(mirrorUrl, tempZipFilePath);
            _loadLog.OnNext("Download complete. Extracting...");
            _systemProcedures.ExtractZipFile(tempZipFilePath, jdkPath);
            _loadLog.OnNext($"Extraction complete. Java executable path: {javaPath}");
            if (system == "linux") _systemProcedures.SetFileExecutable(javaPath);
        }

        _buildJavaPath = javaPath;
    }

    public async Task<Process> GetProcessAsync(IStartupOptions startupOptions, IUser user, bool needDownload,
        string[] jvmArguments, string[] gameArguments)
    {
        if (!_launchers.TryGetValue($"{startupOptions.OsName}/{startupOptions.OsArch}", out var launcher))
        {
            await _notifications.SendMessage("Ошибка", "Выбранная операционная система не поддерживается",
                NotificationType.Error);
            throw new NotSupportedException("Operation system not supported");
        }

        var session = new MSession(user.Name, user.AccessToken, user.Uuid);

        if (needDownload)
            foreach (var anyLauncher in _launchers.Values)
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
                        ExtraGameArguments = gameArguments.Select(c => new MArgument(c)),
                        ExtraJvmArguments = jvmArguments.Select(c => new MArgument(c))
                    }).AsTask();
                }
                catch (Exception exception) when (exception is DirectoryNotFoundException or ZipException or KeyNotFoundException)
                {
                    var title = $"{_profile.Name} {anyLauncher.RulesContext.OS.Name} [{anyLauncher.RulesContext.OS.Arch}] пропущен";
                    var details =
                        $"Создание профиля {_profile.Name} пропущено для операционной системы {anyLauncher.RulesContext.OS.Name} " +
                        $"с архитектурой {anyLauncher.RulesContext.OS.Arch}. " +
                        $"Версия Minecraft {_profile.GameVersion} не поддерживает эту конфигурацию.";
                    await _notifications.SendMessage(title, details, NotificationType.Warn);
                    _exception.OnNext(exception);
                    _loadLog.OnNext(details);
                }
                catch (Exception exception)
                {
                    var message =
                        $"Не удалось восстановить профиль {_profile.Name}, {anyLauncher.RulesContext.OS.Name}, {anyLauncher.RulesContext.OS.Arch}";
                    await _notifications.SendMessage(message, exception);
                    _exception.OnNext(exception);
                    _loadLog.OnNext(message);
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
            ExtraGameArguments = gameArguments.Select(c => new MArgument(c)),
            ExtraJvmArguments = jvmArguments.Select(c => new MArgument(c))
        }).AsTask();
    }

    public bool GetLauncher(string launcherKey, out object launcher)
    {
        var isSuccess = _launchers.TryGetValue(launcherKey, out var returnLauncher);

        launcher = returnLauncher;

        return isSuccess;
    }

    public async Task<bool> Validate()
    {
        foreach (var launcher in _launchers)
        {
            try
            {
                var version = _profile.LaunchVersion ?? throw new Exception("Profile not set");

                await launcher.Value.BuildProcessAsync(version, new MLaunchOption());
            }
            catch (ZipException exception)
            {

            }
            catch (Exception exception)
            {
                _exception.OnNext(exception);
                return false;
            }
        }

        return true;
    }
}
