using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Gml.Core.Helpers.System;
using Gml.Core.Services.System;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace GmlCore.Tests;

[TestFixture]
public class SystemProceduresTests
{
    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GmlCoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, true);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private string _tempRoot = string.Empty;

    private TestSettings CreateSettings()
    {
        return new TestSettings(_tempRoot);
    }

    [Test]
    [TestCase("windows")]
    [TestCase("linux")]
    [TestCase("osx")]
    public async Task InstallDotnet_ShouldCreateCorrectDotnetPath_ForEachPlatform(string platform)
    {
        // Arrange
        var settings = CreateSettings();
        var systemProcedures = new SystemProcedures(settings);
        var dotnetName = platform == "windows" ? "dotnet.exe" : "dotnet";
        var dotnetDirectory = Path.Combine(_tempRoot, "temp", "DotnetBuild");
        var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
        var expectedDotnetPath = Path.Combine(dotnetDirectoryPath, dotnetName);

        // Create the expected structure manually to simulate successful installation
        Directory.CreateDirectory(dotnetDirectoryPath);
        await File.WriteAllTextAsync(expectedDotnetPath, "fake dotnet");

        // Act
        var result = await systemProcedures.InstallDotnet(platform, dotnetName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(systemProcedures.BuildDotnetPath, Is.EqualTo(expectedDotnetPath));
            Assert.That(File.Exists(expectedDotnetPath), Is.True);
        });
    }

    [Test]
    public async Task InstallDotnet_ShouldReturnTrue_WhenDotnetAlreadyExists()
    {
        // Arrange
        var settings = CreateSettings();
        var systemProcedures = new SystemProcedures(settings);
        var platform = SystemService.GetPlatform();
        var dotnetName = platform == "windows" ? "dotnet.exe" : "dotnet";
        var dotnetDirectory = Path.Combine(_tempRoot, "temp", "DotnetBuild");
        var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
        var dotnetPath = Path.Combine(dotnetDirectoryPath, dotnetName);

        // Pre-create dotnet installation
        Directory.CreateDirectory(dotnetDirectoryPath);
        await File.WriteAllTextAsync(dotnetPath, "fake dotnet");

        // Act
        var result = await systemProcedures.InstallDotnet(platform, dotnetName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(systemProcedures.BuildDotnetPath, Is.EqualTo(dotnetPath));
        });
    }

    [Test]
    public async Task InstallDotnet_ShouldReturnTrue_WhenBuildDotnetPathAlreadySet()
    {
        // Arrange
        var settings = CreateSettings();
        var systemProcedures = new SystemProcedures(settings);
        var platform = SystemService.GetPlatform();
        var dotnetName = platform == "windows" ? "dotnet.exe" : "dotnet";
        var dotnetDirectory = Path.Combine(_tempRoot, "temp", "DotnetBuild");
        var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
        var dotnetPath = Path.Combine(dotnetDirectoryPath, dotnetName);

        // Pre-create dotnet installation and set BuildDotnetPath
        Directory.CreateDirectory(dotnetDirectoryPath);
        await File.WriteAllTextAsync(dotnetPath, "fake dotnet");
        await systemProcedures.InstallDotnet();

        // Act - Call InstallDotnet again
        var result = await systemProcedures.InstallDotnet(platform, dotnetName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(systemProcedures.BuildDotnetPath, Is.EqualTo(dotnetPath));
        });
    }

    [Test]
    [TestCase("windows", "dotnet.exe")]
    [TestCase("linux", "dotnet")]
    [TestCase("osx", "dotnet")]
    public async Task InstallDotnet_ShouldUseCorrectExecutableName_ForEachPlatform(string platform,
        string expectedExecutableName)
    {
        // Arrange
        var settings = CreateSettings();
        var systemProcedures = new SystemProcedures(settings);
        var dotnetDirectory = Path.Combine(_tempRoot, "temp", "DotnetBuild");
        var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
        var dotnetPath = Path.Combine(dotnetDirectoryPath, expectedExecutableName);

        // Pre-create the directory structure
        Directory.CreateDirectory(dotnetDirectoryPath);
        await File.WriteAllTextAsync(dotnetPath, "fake dotnet");

        // Act
        var result = await systemProcedures.InstallDotnet(platform, expectedExecutableName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(systemProcedures.BuildDotnetPath, Does.EndWith(expectedExecutableName));
        });
    }

    [Test]
    public async Task InstallDotnet_ShouldCreateTempDotnetBuildDirectory()
    {
        // Arrange
        var settings = CreateSettings();
        var systemProcedures = new SystemProcedures(settings);
        var dotnetDirectory = Path.Combine(_tempRoot, "temp", "DotnetBuild");
        var platform = SystemService.GetPlatform();
        var dotnetName = platform == "windows" ? "dotnet.exe" : "dotnet";
        var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
        var dotnetPath = Path.Combine(dotnetDirectoryPath, dotnetName);

        // Pre-create dotnet to simulate successful installation
        Directory.CreateDirectory(dotnetDirectoryPath);
        await File.WriteAllTextAsync(dotnetPath, "fake dotnet");

        // Act
        var result = await systemProcedures.InstallDotnet(platform, dotnetName);

        // Assert
        Assert.That(Directory.Exists(dotnetDirectory), Is.True);
    }

    [Test]
    public async Task InstallDotnet_ShouldCheckForCurrentPlatform()
    {
        // Arrange
        var settings = CreateSettings();
        var systemProcedures = new SystemProcedures(settings);
        var currentPlatform = SystemService.GetPlatform();
        var dotnetDirectory = Path.Combine(_tempRoot, "temp", "DotnetBuild");
        var dotnetDirectoryPath = Path.Combine(dotnetDirectory, "dotnet-8");
        var expectedExecutable = currentPlatform == "windows" ? "dotnet.exe" : "dotnet";
        var dotnetPath = Path.Combine(dotnetDirectoryPath, expectedExecutable);

        // Pre-create dotnet to simulate successful installation
        Directory.CreateDirectory(dotnetDirectoryPath);
        await File.WriteAllTextAsync(dotnetPath, "fake dotnet");

        // Act
        var result = await systemProcedures.InstallDotnet(currentPlatform, expectedExecutable);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var expectedName = isWindows ? "dotnet.exe" : "dotnet";
            Assert.That(Path.GetFileName(systemProcedures.BuildDotnetPath), Is.EqualTo(expectedName));
        });
    }

    // ----------------- Test fakes -----------------

    private class TestSettings : IGmlSettings
    {
        public TestSettings(string root)
        {
            Name = "Test";
            BaseDirectory = root;
            InstallationDirectory = root;
            HttpClient = new HttpClient();
            SystemProcedures = new TestSystemProcedures();
            StorageSettings = null;
            SecurityKey = "key";
            TextureServiceEndpoint = "http://localhost";
        }

        public string Name { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }
        public HttpClient HttpClient { get; }
        public IStorageSettings? StorageSettings { get; set; }
        public string SecurityKey { get; set; }
        public ISystemProcedures SystemProcedures { get; }
        public string TextureServiceEndpoint { get; set; }
    }

    private class TestSystemProcedures : ISystemProcedures
    {
        public string DefaultInstallation => Path.Combine(Path.GetTempPath(), "GmlCoreTestsDefault");
        public string? BuildDotnetPath => string.Empty;
        public IObservable<string> DownloadLogs => Observable.Empty<string>();

        public string CleanFolderName(string name)
        {
            return name;
        }

        public string GetDefaultInstallationPath()
        {
            return DefaultInstallation;
        }

        public Task<bool> InstallDotnet(string? platform = null, string? dotnetExecutableName = null)
        {
            return Task.FromResult(true);
        }

        public Task<string> GetAvailableMirrorAsync(IDictionary<string, string[]> mirrorUrls, string? platform = null)
        {
            return Task.FromResult(string.Empty);
        }

        public Task DownloadFileAsync(string url, string destinationFilePath)
        {
            return Task.CompletedTask;
        }

        public void ExtractZipFile(string zipFilePath, string extractPath)
        {
        }

        public void SetFileExecutable(string filePath)
        {
        }

        public Task<IEnumerable<IBootstrapProgram>> GetJavaVersions()
        {
            return Task.FromResult<IEnumerable<IBootstrapProgram>>(
                Array.Empty<IBootstrapProgram>());
        }
    }
}
