using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Text.Json;
using Gml;
using Gml.Core.Constants;
using Gml.Core.Helpers.Launcher;
using Gml.Models.Launcher;
using Gml.Models.Storage;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Sentry;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace GmlCore.Tests;

[TestFixture]
public class LauncherProceduresTests
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
            try
            {
                Directory.Delete(_tempRoot, true);
            }
            catch
            {
                /* ignore */
            }
    }

    private string _tempRoot = string.Empty;

    private (LauncherProcedures sut, FakeLauncherInfo flInfo, InMemoryStorage storage, FakeFileStorage files)
        CreateSut()
    {
        var settings = new TestSettings(_tempRoot);
        var gmlManager = new GmlManager(settings);

        var launcherInfo = new FakeLauncherInfo(settings);
        var storage = new InMemoryStorage();
        var files = new FakeFileStorage();

        var sut = new LauncherProcedures(launcherInfo, storage, files, gmlManager);
        return (sut, launcherInfo, storage, files);
    }

    [Test]
    public void CanCompile_VersionDirectoryMissing_ReturnsFalseAndMessage()
    {
        var (sut, flInfo, _, _) = CreateSut();
        var version = "1.0.0";

        var result = sut.CanCompile(version, out var message);

        Assert.That(result, Is.False);
        Assert.That(message, Does.Contain($"Не загружена сборка профиля для версии \"{version}\""));
    }

    [Test]
    public void CanCompile_MissingRequiredProjects_ReturnsFalseWithMessages()
    {
        var (sut, flInfo, _, _) = CreateSut();
        var version = "2.0.0";
        var basePath = Path.Combine(flInfo.InstallationDirectory, "Launcher", version);
        Directory.CreateDirectory(basePath);

        // Create an unrelated project
        File.WriteAllText(Path.Combine(basePath, "Unrelated.csproj"), "<Project></Project>");

        var ok = sut.CanCompile(version, out var message1);
        Assert.That(ok, Is.False);
        Assert.That(message1, Does.Contain("Gml.Client"));

        // Add Gml.Client project but not GamerVII.Notification.Avalonia
        File.WriteAllText(Path.Combine(basePath, "Gml.Client.App.csproj"), "<Project></Project>");

        ok = sut.CanCompile(version, out var message2);
        Assert.That(ok, Is.False);
        Assert.That(message2,
            Does.Contain("Gml.Client")); // message text references Gml.Client in both branches currently
    }

    [Test]
    public void CanCompile_AllRequiredProjectsPresent_ReturnsTrue()
    {
        var (sut, flInfo, _, _) = CreateSut();
        var version = "3.0.0";
        var basePath = Path.Combine(flInfo.InstallationDirectory, "Launcher", version);
        Directory.CreateDirectory(basePath);

        // Create both required projects at any depth
        var sub = Directory.CreateDirectory(Path.Combine(basePath, "src"));
        File.WriteAllText(Path.Combine(sub.FullName, "Gml.Client.Core.csproj"), "<Project></Project>");
        File.WriteAllText(Path.Combine(basePath, "GamerVII.Notification.Avalonia.csproj"), "<Project></Project>");

        var ok = sut.CanCompile(version, out var message);

        Assert.That(ok, Is.True);
        Assert.That(message, Is.EqualTo("Success"));
    }

    [Test]
    public async Task GetPlatforms_ReturnsExpectedRuntimes()
    {
        var (sut, _, _, _) = CreateSut();

        var platforms = (await sut.GetPlatforms()).ToArray();

        var expected = new[]
        {
            "win-x86", "win-x64", "win-arm", "win-arm64",
            "linux-musl-x64", "linux-arm", "linux-arm64", "linux-x64",
            "osx-x64", "osx-arm64"
        };

        Assert.That(platforms, Is.EquivalentTo(expected));
    }

    [Test]
    public async Task CreateVersion_UploadsFilesAndUpdatesStorage()
    {
        var (sut, flInfo, storage, files) = CreateSut();

        // Prepare a fake build folder structure
        var build = new LauncherBuild
        {
            Name = "build-1",
            Path = Path.Combine(_tempRoot, "builds")
        };
        Directory.CreateDirectory(build.Path);
        var runtimeDir = Directory.CreateDirectory(Path.Combine(build.Path, "win-x64"));

        // Create files: an exe and a pdb
        var exePath = Path.Combine(runtimeDir.FullName, "Gml.Launcher.exe");
        File.WriteAllText(exePath, "binary");
        File.WriteAllText(Path.Combine(runtimeDir.FullName, "Gml.Launcher.pdb"), "pdb");

        var version = new LauncherVersion
        {
            Version = "4.2.0",
            Title = "Test",
            Description = "Desc",
            Guid = "seed-guid"
        };

        var returnedGuid = await sut.CreateVersion(version, build);

        Assert.That(returnedGuid, Is.EqualTo(version.Guid));

        // Verify LoadFile was called with expected folder and filename
        Assert.That(files.Calls.Count, Is.EqualTo(1));
        var call = files.Calls[0];
        Assert.That(call.folder, Is.EqualTo(Path.Combine("launcher", "win", "x64")));
        Assert.That(call.defaultFileName, Is.EqualTo($"win-x64-{Path.GetFileName(exePath)}"));

        // Storage should be updated
        var actualVersion = await storage.GetAsync<string>(StorageConstants.ActualVersion);
        Assert.That(actualVersion, Is.EqualTo("4.2.0"));

        var actualVersionInfo =
            await storage.GetAsync<Dictionary<string, IVersionFile?>>(StorageConstants.ActualVersionInfo);
        Assert.That(actualVersionInfo, Is.Not.Null);
        Assert.That(actualVersionInfo!.ContainsKey("win-x64"), Is.True);
        var storedVersion = actualVersionInfo["win-x64"]!;
        Assert.That(storedVersion.Version, Is.EqualTo("4.2.0"));
        // The clone's Guid should be set to value returned by LoadFile
        Assert.That(storedVersion.Guid, Is.EqualTo(files.Calls[0].returnedGuid));

        // Also LauncherInfo should reflect the same
        Assert.That(flInfo.ActualLauncherVersion.ContainsKey("win-x64"), Is.True);
        Assert.That(flInfo.ActualLauncherVersion["win-x64"]!.Guid, Is.EqualTo(files.Calls[0].returnedGuid));
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
        public IObservable<string> DownloadLogs => throw new NotImplementedException();

        public string CleanFolderName(string name)
        {
            return name;
        }

        public string GetDefaultInstallationPath()
        {
            return DefaultInstallation;
        }

        public Task<bool> InstallDotnet()
        {
            return Task.FromResult(true);
        }

        public Task<string> GetAvailableMirrorAsync(IDictionary<string, string[]> mirrorUrls)
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

    private class FakeLauncherInfo : ILauncherInfo
    {
        public FakeLauncherInfo(IGmlSettings settings)
        {
            Settings = settings;
            Name = settings.Name;
            BaseDirectory = settings.BaseDirectory;
            InstallationDirectory = settings.InstallationDirectory;
            StorageSettings = new StorageSettings();
            ActualLauncherVersion = new Dictionary<string, IVersionFile?>();
            SettingsUpdated = new Subject<IStorageSettings>();
            AccessTokens = new ConcurrentDictionary<string, string>();
        }

        public string Name { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }
        public IStorageSettings StorageSettings { get; set; }
        public Dictionary<string, IVersionFile?> ActualLauncherVersion { get; set; }
        public IGmlSettings Settings { get; }
        public IObservable<IStorageSettings> SettingsUpdated { get; }
        public IDictionary<string, string> AccessTokens { get; set; }

        public void UpdateSettings(StorageType storageType, string storageHost, string storageLogin,
            string storagePassword, TextureProtocol textureProtocol, string curseForgeKey, string vkKey)
        {
        }

        public Task<IEnumerable<ILauncherBuild>> GetBuilds()
        {
            return Task.FromResult<IEnumerable<ILauncherBuild>>(Array.Empty<ILauncherBuild>());
        }

        public Task<ILauncherBuild?> GetBuild(string name)
        {
            return Task.FromResult<ILauncherBuild?>(null);
        }
    }

    private class InMemoryStorage : IStorageService
    {
        private readonly Dictionary<string, object?> _data = new();

        public Task SetAsync<T>(string key, T? value)
        {
            _data[key] = value;
            return Task.CompletedTask;
        }

        public Task<T?> GetAsync<T>(string key, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            if (_data.TryGetValue(key, out var obj))
            {
                if (obj is T t)
                    return Task.FromResult<T?>(t);
                if (obj is string s && typeof(T) == typeof(string))
                    return Task.FromResult((T?)(object?)s);
            }

            return Task.FromResult(default(T));
        }

        // Unused members for these tests
        public Task<T?> GetUserAsync<T>(string login, JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task<int> SaveRecord<T>(T record)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetUserByNameAsync<T>(string userName, JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetUserByAccessToken<T>(string accessToken, JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetUserByUuidAsync<T>(string uuid, JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetUserByCloakAsync<T>(string guid, JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetUserBySkinAsync<T>(string guid, JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task SetUserAsync<T>(string login, string uuid, T value)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions,
            IEnumerable<string> userUuids)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions, int take, int offset,
            string findName)
        {
            throw new NotImplementedException();
        }

        public Task AddBugAsync(IBugInfo bugInfo)
        {
            throw new NotImplementedException();
        }

        public Task ClearBugsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetBugsAsync<T>()
        {
            throw new NotImplementedException();
        }

        public Task<IBugInfo?> GetBugIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IBugInfo>> GetFilteredBugsAsync(Expression<Func<IStorageBug, bool>> filter)
        {
            throw new NotImplementedException();
        }

        public Task RemoveUserByUuidAsync(string userUuid)
        {
            throw new NotImplementedException();
        }

        public Task AddLockedHwid(IHardware hardware)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLockedHwid(IHardware hardware)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsLockedHwid(IHardware hardware)
        {
            throw new NotImplementedException();
        }
    }

    private class FakeFileStorage : IFileStorageProcedures
    {
        public readonly List<(string folder, string defaultFileName, string returnedGuid)> Calls = new();

        public Task<IFileInfo?> DownloadFileStream(string fileHash, Stream outputStream)
        {
            throw new NotImplementedException();
        }

        public Task<string> LoadFile(Stream fileStream, string? folder = null, string? defaultFileName = null,
            Dictionary<string, string>? tags = null)
        {
            var ret = $"guid-{defaultFileName}";
            Calls.Add((folder ?? string.Empty, defaultFileName ?? string.Empty, ret));
            return Task.FromResult(ret);
        }

        public Task<(Stream File, string fileName, long Length)> GetFileStream(string fileHash, string? folder = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckFileExists(string folder, string fileHash)
        {
            throw new NotImplementedException();
        }
    }
}
