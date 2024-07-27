using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CmlLib.Core.Files;
using CmlLib.Core.Java;
using Gml.Core.Helpers.Mirrors;
using Gml.Core.Launcher;
using Gml.Core.Services.System;
using Gml.Models.Bootstrap;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.System
{
    public class SystemProcedures(IGmlSettings gmlSettings) : ISystemProcedures
    {
        private string _installationDirectory;
        private string? _buildDotnetPath;
        private readonly MinecraftJavaManifestResolver _javaManifestResolver = new(gmlSettings.HttpClient);
        private IEnumerable<MinecraftJavaManifestMetadata>? _javaManifestMetadata;
        public string? BuildDotnetPath => _buildDotnetPath;

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
            if (_buildDotnetPath is not null && File.Exists(_buildDotnetPath))
            {
                return true;
            }

            try
            {
                var system = SystemService.GetPlatform();
                var dotnetName = system == "windows" ? "dotnet.exe" : "dotnet";
                var dotnetDirectory = Path.Combine(gmlSettings.InstallationDirectory, "DotnetBuild");
                var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
                var dotnetPath = Path.Combine(dotnetDirectoryPath, dotnetName);
                if (!Directory.Exists(dotnetDirectory) || !File.Exists(dotnetPath))
                {
                    Directory.CreateDirectory(dotnetDirectory);
                    var mirror = await GetAvailableMirrorAsync(MirrorsHelper.DotnetMirrors);
                    var tempZipFilePath = Path.Combine(dotnetDirectory, "dotnet.zip");
                    await DownloadFileAsync(mirror, tempZipFilePath);
                    ExtractZipFile(tempZipFilePath, dotnetDirectoryPath);
                    if (system == "linux")
                    {
                        SetFileExecutable(dotnetPath);
                    }
                }

                _buildDotnetPath = dotnetPath;
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

        public async Task<IEnumerable<IBootstrapProgram>> GetJavaVersions()
        {
            _javaManifestMetadata ??= await _javaManifestResolver.GetAllManifests();

            var javaVersions = _javaManifestMetadata
                .OrderBy(c => c.VersionName)
                .GroupBy(c => new
                {
                    Name = c.Component,
                    MajorVersion = int.Parse(c.GetMajorVersion() ?? "0"),
                    Version = c.VersionName
                });

            return javaVersions.Select(c => new JavaBootstrapProgram(c.Key.Name, c.Key.Version!, c.Key.MajorVersion!));
        }

        public async Task DownloadFileAsync(string url, string destinationFilePath)
        {
            using HttpResponseMessage response = await gmlSettings.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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

        public async Task<string> GetAvailableMirrorAsync(IDictionary<string, string[]> mirrorUrls)
        {
            if (mirrorUrls.TryGetValue(SystemService.GetPlatform(), out string[] mirrors))
            {
                foreach (string url in mirrors)
                {
                    try
                    {
                        HttpResponseMessage response = await gmlSettings.HttpClient.GetAsync(url);
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
    }
}
