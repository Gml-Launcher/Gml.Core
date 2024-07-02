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
using Gml.Core.Services.Storage;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Helpers.Launcher;

public class LauncherProcedures : ILauncherProcedures
{
    private readonly ILauncherInfo _launcherInfo;
    private readonly IStorageService _storage;
    private readonly IFileStorageProcedures _files;
    private ISubject<string> _buildLogs = new Subject<string>();
    private readonly Subject<string> _logsBuffer;
    public IObservable<string> BuildLogs => _buildLogs;

    public LauncherProcedures(ILauncherInfo launcherInfo, IStorageService storage, IFileStorageProcedures files)
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

        _launcherInfo = launcherInfo;
        _storage = storage;
        _files = files;
    }

    public async Task<string> CreateVersion(IVersionFile version, OsType osTypeEnum)
    {
        if (version.File is null)
        {
            throw new ArgumentNullException(nameof(version.File));
        }

        version.Guid = await _files.LoadFile(version.File, "launcher");

        _launcherInfo.ActualLauncherVersion[osTypeEnum] = version;

        await _storage.SetAsync(StorageConstants.ActualVersion, version.Guid);
        await _storage.SetAsync(StorageConstants.ActualVersionInfo, _launcherInfo.ActualLauncherVersion);

        await version.File.DisposeAsync();
        version.File = null;

        return version.Guid;

    }

    public async Task Build(string version)
    {
        var projectPath = new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "Launcher", version)).GetDirectories().First().FullName;
        var launcherDirectory = new DirectoryInfo(Path.Combine(projectPath, "src", "Gml.Launcher"));

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException("Нет исходников для формирования бинарных файлов!");
        }

        var allowedVersions = new List<string>
        {
            "win-x86",
            "win-x64",
            "linux-x64"
        };

        var buildFolder = await CreateBuilds(allowedVersions, projectPath, launcherDirectory);

    }

    private async Task<object> CreateBuilds(List<string> allowedVersions, string projectPath, DirectoryInfo launcherDirectory)
    {
        var dotnetPath = _launcherInfo.Settings.SystemProcedures.BuildDotnetPath;

        foreach (var version in allowedVersions)
        {
            ProcessStartInfo? processStartInfo = default;
            var command = string.Empty;

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = $"/c {dotnetPath} publish ./src/Gml.Launcher/ -r {version} -p:PublishSingleFile=true --self-contained false -p:IncludeNativeLibrariesForSelfExtract=true";
                processStartInfo = new ProcessStartInfo("cmd", command)
                {
                    WorkingDirectory = projectPath
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                command = $"{dotnetPath} publish ./src/Gml.Launcher/ -r {version} -p:PublishSingleFile=true --self-contained false -p:IncludeNativeLibrariesForSelfExtract=true";
                processStartInfo = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"")
                {
                    WorkingDirectory = projectPath
                };
            }

            if (processStartInfo != null)
            {
                // Ensure redirection is enabled
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;

                var process = new Process
                {
                    StartInfo = processStartInfo
                };

                process.OutputDataReceived += (sender, e) => _logsBuffer.OnNext($"[{DateTime.Now:HH:m:ss:fff}] [INFO] {e.Data}");
                process.ErrorDataReceived += (sender, e) => _logsBuffer.OnNext($"[{DateTime.Now:HH:m:ss:fff}] [INFO] {e.Data}");

                process.Start();

                // Call BeginOutputReadLine and BeginErrorReadLine after the process has started
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
        }

        var publishDirectory = launcherDirectory.GetDirectories("publish", SearchOption.AllDirectories);

        var buildsFolder = new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "LauncherBuilds",
            $"build-{DateTime.Now:dd-MM-yyyy HH-mm-ss}"));

        if (!buildsFolder.Exists)
        {
            buildsFolder.Create();
        }

        foreach (DirectoryInfo dir in publishDirectory)
        {
            var newFolder = new DirectoryInfo(Path.Combine(buildsFolder.FullName, dir.Parent.Name));
            if (!newFolder.Exists)
            {
                newFolder.Create();
            }

            CopyDirectory(dir, newFolder);
        }

        return buildsFolder.FullName;
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
