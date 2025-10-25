using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Launcher;
using Gml.Core.Services.GitHub;
using Gml.Core.Services.Storage;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.GitHub;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Helpers.Launcher;

public class LauncherProcedures : ILauncherProcedures
{
    private readonly ILauncherInfo _launcherInfo;
    private readonly IStorageService _storage;
    private readonly IFileStorageProcedures _files;
    private readonly GmlManager _gmlManager;
    private readonly IGitHubService _githubService;
    private ISubject<string> _buildLogs = new Subject<string>();
    private readonly Subject<string> _logsBuffer;
    public IObservable<string> BuildLogs => _buildLogs;
    private const string _launcherGitHub = "https://github.com/Gml-Launcher/Gml.Launcher";

    private string[] _allowedVersions =
    [
        "win-x86",
        "win-x64",
        "win-arm",
        "win-arm64",
        "linux-musl-x64",
        "linux-arm",
        "linux-arm64",
        "linux-x64",
        "osx-x64",
        "osx-arm64",
    ];

    public LauncherProcedures(
        ILauncherInfo launcherInfo,
        IStorageService storage,
        IFileStorageProcedures files,
        GmlManager gmlManager)
    {
        _logsBuffer = new Subject<string>();

        _logsBuffer
            .Buffer(TimeSpan.FromSeconds(2))
            .Select(items => string.Join(Environment.NewLine, items))
            .Subscribe(combinedText =>
            {
                if (!string.IsNullOrEmpty(combinedText))
                {
                    _buildLogs.OnNext(combinedText);
                }
            });

        _gmlManager = gmlManager;
        _launcherInfo = launcherInfo;
        _storage = storage;
        _files = files;
        _githubService = new GitHubService(launcherInfo.Settings.HttpClient, gmlManager);
    }

    public async Task<string> CreateVersion(IVersionFile version, ILauncherBuild launcherBuild)
    {
        var versions = Directory
            .GetDirectories(launcherBuild.Path)
            .Select(c => new DirectoryInfo(c));

        foreach (var versionInfo in versions)
        {
            var localVersion = version.Clone() as IVersionFile;

            var splitVersionInfo = versionInfo.Name.Split('-');
            var osName = splitVersionInfo.First();
            var osArch = splitVersionInfo.Last();

            var executeFile = versionInfo.GetFiles("*.*")
                .FirstOrDefault(file => !file.Extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase));

            if (executeFile != null)
            {
                await using var stream = File.OpenRead(executeFile.FullName);
                localVersion!.Guid = await _files.LoadFile(
                    stream,
                    Path.Combine("launcher", osName, osArch),
                    $"{versionInfo.Name}-{executeFile.Name}");
            }

            _launcherInfo.ActualLauncherVersion[versionInfo.Name] = localVersion;
            await _storage.SetAsync(StorageConstants.ActualVersion, version.Version);
            await _storage.SetAsync(StorageConstants.ActualVersionInfo, _launcherInfo.ActualLauncherVersion);
        }

        return version.Guid;
    }

    public async Task<bool> Build(string version, string[] osNameVersions)
    {
        var projectPath = new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "Launcher", version))
            .FullName;
        var launcherDirectory = new DirectoryInfo(Path.Combine(projectPath, "src", "Gml.Launcher"));

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException("Нет исходников для формирования бинарных файлов!");
        }

        var buildFolder = await CreateBuilds(osNameVersions, projectPath, launcherDirectory);

        return buildFolder.IsSuccess;
    }

    public bool CanCompile(string version, out string message)
    {
        var versionDirectory = Path.Combine(_launcherInfo.InstallationDirectory, "Launcher", version);

        if (!Directory.Exists(versionDirectory))
        {
            message =
                $"Не загружена сборка профиля для версии \"{version}\", загрузите ее на сервер в папку: \"{Path.Combine(_launcherInfo.InstallationDirectory, "Launcher", version)}\"";
            return false;
        }

        var projectPath = new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "Launcher", version))
            .FullName;

        if (string.IsNullOrEmpty(projectPath))
        {
            message = $"Не удалось найти проект по пути: {projectPath}";
            return false;
        }

        var projectDirectory = new DirectoryInfo(projectPath);

        var projects = projectDirectory.GetFiles("*.csproj", SearchOption.AllDirectories);

        if (!projects.Any(c => c.Name.StartsWith("Gml.Client")))
        {
            message =
                $"Не удалось найти проект по пути: Gml.Client. Убедитесь, что проект загружен на сервер полностью. " +
                "Подробная инструкция доступна на wiki.recloud.tech: \n" +
                "Клиентская часть / Сборка лаунчера / Сборка из панели / Загрузка исходных файлов / Пункт 2. Загрузка";
            return false;
        }

        if (!projects.Any(c => c.Name.StartsWith("GamerVII.Notification.Avalonia")))
        {
            message =
                $"Не удалось найти проект по пути: Gml.Client. Убедитесь, что проект загружен на сервер полностью";
            return false;
        }

        message = "Success";
        return true;
    }

    public Task<IEnumerable<string>> GetPlatforms()
    {
        return Task.FromResult<IEnumerable<string>>(_allowedVersions);
    }

    public async Task Download(string version, string host, string folderName)
    {
        try
        {
            _buildLogs.OnNext("Preparing launcher downloading...");
            var projectPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "Launcher", version);

            if (Directory.Exists(projectPath))
            {
                await _gmlManager.Notifications
                    .SendMessage("Лаунчер уже существует в папке, удалите его перед сборкой", NotificationType.Error);
                return;
            }

            projectPath = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "Launcher");

            var allowedVersions = await _githubService
                .GetRepositoryTags("Gml-Launcher", "Gml.Launcher");

            if (allowedVersions.All(c => c != version))
            {
                await _gmlManager.Notifications
                    .SendMessage($"Полученная версия лаунчера \"{version}\" не поддерживается", NotificationType.Error);
                return;
            }

            _buildLogs.OnNext("Start downloading...");
            var logsDispose = _githubService.Logs.Subscribe(log => _buildLogs.OnNext(log));
            var newFolder = await _githubService.DownloadProject(projectPath, version, _launcherGitHub);
            logsDispose.Dispose();

            _buildLogs.OnNext("Edit configs...");
            await _githubService.EditLauncherFiles(newFolder, host, folderName);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            await _gmlManager.Notifications.SendMessage("Ошибка при загрузке клиента лаунчера", exception);
        }
    }

    public Task<IReadOnlyCollection<string>> GetVersions()
    {
        return _githubService.GetRepositoryTags("Gml-Launcher", "Gml.Launcher");
    }


    private async Task<(bool IsSuccess, string Path)> CreateBuilds(string[] versions, string projectPath,
        DirectoryInfo launcherDirectory)
    {
        var dotnetPath = _launcherInfo.Settings.SystemProcedures.BuildDotnetPath;
        var statusCode = 0;

        foreach (var version in versions.Where(version => _allowedVersions.Contains(version)))
        {
            var processStartInfo = GetProcessStartInfo(dotnetPath, version, projectPath);
            if (processStartInfo != null)
            {
                statusCode = await ExecuteProcessAsync(processStartInfo);
            }
        }

        var publishDirectory = launcherDirectory.GetDirectories("publish", SearchOption.AllDirectories);
        var buildsFolder = CreateBuildsFolder();

        foreach (var dir in publishDirectory)
        {
            var newFolder = new DirectoryInfo(Path.Combine(buildsFolder.FullName, dir.Parent.Name));
            if (!newFolder.Exists)
            {
                newFolder.Create();
            }

            CopyDirectory(dir, newFolder);
        }

        return (statusCode == 0, buildsFolder.FullName);
    }

    private ProcessStartInfo? GetProcessStartInfo(string dotnetPath, string version, string projectPath)
    {
        var publishArgs = $"publish ./src/Gml.Launcher/ -r {version} -c Release -f net8.0 " +
                          "-p:PublishSingleFile=true --self-contained true " +
                          "-p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true " +
                          "-p:PublishReadyToRun=true";

        // var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        //     ? $"/c {publishArgs}"
        //     : $"-c \"{publishArgs}\"";
        //
        // var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/bash";

        return new ProcessStartInfo(dotnetPath, publishArgs)
        {
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private async Task<int> ExecuteProcessAsync(ProcessStartInfo processStartInfo)
    {
        var process = new Process { StartInfo = processStartInfo };
        process.OutputDataReceived +=
            (sender, e) => _logsBuffer.OnNext($"[{DateTime.Now:HH:mm:ss:fff}] [INFO] {e.Data}");
        process.ErrorDataReceived +=
            (sender, e) => _logsBuffer.OnNext($"[{DateTime.Now:HH:mm:ss:fff}] [ERROR] {e.Data}");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

#if NETSTANDARD2_1
        process.WaitForExit();
#else
    await process.WaitForExitAsync();
#endif
        return process.ExitCode;
    }

    private DirectoryInfo CreateBuildsFolder()
    {
        var buildsFolder = new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "LauncherBuilds",
            $"build-{DateTime.Now:dd-MM-yyyy HH-mm-ss}"));
        if (!buildsFolder.Exists)
        {
            buildsFolder.Create();
        }

        return buildsFolder;
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        if (!destination.Exists)
        {
            destination.Create();
        }

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destination.FullName, file.Name), true);
        }

        foreach (DirectoryInfo subDir in source.GetDirectories())
        {
            CopyDirectory(subDir, new DirectoryInfo(Path.Combine(destination.FullName, subDir.Name)));
        }
    }
}
