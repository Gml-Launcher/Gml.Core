using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core.Files;
using CmlLib.Core.Java;
using Gml.Core.Helpers.Mirrors;
using Gml.Core.Services.System;
using Gml.Models.Bootstrap;
using Gml.Models.Mirror;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.System;

public class SystemProcedures(IGmlSettings gmlSettings) : ISystemProcedures
{
    private readonly Subject<string> _downloadLogs = new();
    private readonly MinecraftJavaManifestResolver _javaManifestResolver = new(gmlSettings.HttpClient);
    private string _installationDirectory;
    private IEnumerable<MinecraftJavaManifestMetadata>? _javaManifestMetadata;
    public string? BuildDotnetPath { get; private set; }

    public IObservable<string> DownloadLogs => _downloadLogs;

    public string DefaultInstallation
    {
        get
        {
            if (string.IsNullOrEmpty(_installationDirectory))
                _installationDirectory = GetDefaultInstallationPath();

            return _installationDirectory;
        }
    }

    public string CleanFolderName(string name)
    {
        var cleanedName =
            new string(Array.FindAll(name.ToCharArray(),
                c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));

        cleanedName = Path.GetInvalidFileNameChars()
            .Aggregate(cleanedName,
                (current, c) => current.Replace(c.ToString(), "_"));

        return cleanedName;
    }

    public string GetDefaultInstallationPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : Environment.CurrentDirectory;
    }

    public async Task<bool> InstallDotnet()
    {
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64 ||
            RuntimeInformation.OSArchitecture == Architecture.Arm ||
            RuntimeInformation.OSArchitecture == Architecture.Armv6)
        {
            _downloadLogs.OnNext("Не поддерживается сборка на ARM архитектуре");
            return false;
        }

        if (BuildDotnetPath is not null && File.Exists(BuildDotnetPath)) return true;

        _downloadLogs.OnNext("Check install Dotnet");

        try
        {
            var system = SystemService.GetPlatform();
            var dotnetName = system == "windows" ? "dotnet.exe" : "dotnet";
            var dotnetDirectory = Path.Combine(gmlSettings.InstallationDirectory, "DotnetBuild");
            var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
            var dotnetPath = Path.Combine(dotnetDirectoryPath, dotnetName);
            if (!Directory.Exists(dotnetDirectory) || !File.Exists(dotnetPath))
            {
                _downloadLogs.OnNext("Starting install Dotnet");
                Directory.CreateDirectory(dotnetDirectory);
                _downloadLogs.OnNext("Get active mirrors...");
                var mirror = await GetAvailableMirrorAsync(MirrorsHelper.DotnetMirrors);
                var tempZipFilePath = Path.Combine(dotnetDirectory, "dotnet.zip");
                await DownloadFileAsync(mirror, tempZipFilePath);
                ExtractZipFile(tempZipFilePath, dotnetDirectoryPath);
                if (system == "linux") SetFileExecutable(dotnetPath);
            }

            BuildDotnetPath = dotnetPath;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return false;
        }

        return true;
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
                CreateNoWindow = true
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

    public async Task<IEnumerable<IBootstrapProgram>> GetJavaVersions()
    {
        _javaManifestMetadata ??= await _javaManifestResolver.GetAllManifests();

        var javaVersions = _javaManifestMetadata
            .OrderBy(c => c.VersionName)
            .GroupBy(c => new
            {
                Name = c.Component,
                MajorVersion = TryParseMajorVersion(c.GetMajorVersion()),
                Version = c.VersionName
            });

        return javaVersions.Select(c => new JavaBootstrapProgram(c.Key.Name, c.Key.Version!, c.Key.MajorVersion));
    }

    public async Task DownloadFileAsync(string url, string destinationFilePath)
    {
        _downloadLogs.OnNext($"Starting download: {url}");
        using var response =
            await gmlSettings.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var totalBytesRead = 0L;
        var lastReportedProgress = -1;

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(),
            fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
                8192, true);
        var buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;

            if (totalBytes > 0)
            {
                var progress = (int)((double)totalBytesRead / totalBytes * 100);
                if (progress != lastReportedProgress)
                {
                    _downloadLogs.OnNext($"Downloaded: {progress}%");
                    lastReportedProgress = progress;
                }
            }
        }
    }

    public void ExtractZipFile(string zipFilePath, string extractPath)
    {
        if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        File.Delete(zipFilePath);
    }

    public async Task<string> GetAvailableMirrorAsync(IDictionary<string, string[]> mirrorUrls)
    {
        if (mirrorUrls.TryGetValue(SystemService.GetPlatform(), out var mirrors))
        {
            List<MirrorPingModel> mirrorsPing = [];

            foreach (var url in mirrors)
                try
                {
                    var mirrorPing = new Ping();
                    var uri = new Uri(url);
                    var domain = uri.Host;

                    var ping = await mirrorPing.SendPingAsync(domain);

                    mirrorPing.Dispose();

                    mirrorsPing.Add(new MirrorPingModel { Url = url, RoundtripTime = ping.RoundtripTime });
                }
                catch (Exception)
                {
                    // Ignore the exception and try the next URL
                }

            mirrorsPing = mirrorsPing.OrderBy(x => x.RoundtripTime).ToList();

            foreach (var pingModel in mirrorsPing)
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(3000));
                    var response = await gmlSettings.HttpClient.GetAsync(pingModel.Url,
                        HttpCompletionOption.ResponseHeadersRead, cts.Token);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        await using var stream = await response.Content.ReadAsStreamAsync();
                        var buffer = new byte[8192];
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (bytesRead > 0)
                            return pingModel.Url;
                    }
                }
                catch (HttpRequestException)
                {
                    // Ignore the exception and try the next URL
                }
                catch (TaskCanceledException)
                {
                    // Catch timeout exception and continue to the next URL
                }
                catch (Exception)
                {
                    // Ignore other exceptions and try the next URL
                }
        }

        throw new Exception("Нет доступных зеркал для загрузки файлов");
    }

    private int TryParseMajorVersion(string? majorVersion)
    {
        return int.TryParse(majorVersion, out var result) ? result : 0;
    }
}
