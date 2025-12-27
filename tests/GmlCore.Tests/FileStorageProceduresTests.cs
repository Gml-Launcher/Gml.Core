using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Gml.Core.Helpers.Files;
using Gml.Models.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Sentry;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.User;

namespace GmlCore.Tests;

[TestFixture]
public class FileStorageProceduresTests
{
    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "gml-filestorage-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);

        _launcher = new TestLauncherInfo
        {
            InstallationDirectory = _tempRoot,
            StorageSettings = new TestStorageSettings
            {
                StorageType = StorageType.LocalStorage,
                StorageHost = "",
                StorageLogin = "",
                StoragePassword = "",
                TextureProtocol = TextureProtocol.Http
            }
        };
        _storage = new FakeStorageService();
        _bugTracker = new FakeBugTracker();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true);
        }
        catch
        {
            /* ignore */
        }
    }

    private string _tempRoot = string.Empty;
    private TestLauncherInfo _launcher = null!;
    private FakeStorageService _storage = null!;
    private FakeBugTracker _bugTracker = null!;

    private FileStorageProcedures CreateSut()
    {
        return new FileStorageProcedures(_launcher, _storage, _bugTracker);
    }

    [Test]
    public async Task DownloadFileStream_LocalStorage_NotFound_ReturnsNull()
    {
        await using var output = new MemoryStream();
        var sut = CreateSut();

        var result = await sut.DownloadFileStream("missing-hash", output);

        Assert.That(result, Is.Null);
        Assert.That(output.Length, Is.EqualTo(0));
    }

    [Test]
    public async Task DownloadFileStream_LocalStorage_AttachmentsFile_CopiesToOutput()
    {
        // Arrange: create a file inside Attachments with GUID name
        var fileName = Guid.NewGuid().ToString();
        var attachmentsDir = Path.Combine(_launcher.InstallationDirectory, "Attachments");
        Directory.CreateDirectory(attachmentsDir);
        var fullPath = Path.Combine(attachmentsDir, fileName);
        var content = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(fullPath, content);

        var info = new LocalFileInfo
        {
            Name = fileName,
            Directory = attachmentsDir,
            FullPath = fullPath
        };
        await _storage.SetAsync(fileName, info);

        var sut = CreateSut();
        await using var output = new MemoryStream();

        // Act
        var result = await sut.DownloadFileStream(fileName, output);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(output.ToArray(), Is.EqualTo(content));
    }

    [Test]
    public async Task DownloadFileStream_LocalStorage_ProfileFile_UsesInstallationDirectory()
    {
        // Arrange: localFileInfo refers to a relative path (Directory) under InstallationDirectory
        var relativeDir = Path.Combine("clients", "profileA");
        var fileName = "options.txt";
        var expectedPath = Path.GetFullPath(Path.Combine(_launcher.InstallationDirectory, relativeDir));
        Directory.CreateDirectory(expectedPath);
        var fullFilePath = Path.Combine(expectedPath, fileName);
        var content = Encoding.UTF8.GetBytes("hello");
        await File.WriteAllBytesAsync(fullFilePath, content);

        var storageKey = "hash-123";
        var info = new LocalFileInfo
        {
            Name = fileName,
            Directory = Path.Combine(relativeDir, fileName),
            FullPath = "will-be-overwritten-by-code"
        };
        await _storage.SetAsync(storageKey, info);

        var sut = CreateSut();
        await using var output = new MemoryStream();

        // Act
        var result = await sut.DownloadFileStream(storageKey, output);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(output.ToArray(), Is.EqualTo(content));
    }

    [Test]
    public async Task LoadFile_LocalStorage_WritesFile_AndStoresMetadata_WhenNameProvided()
    {
        // Arrange
        var sut = CreateSut();
        var providedName = "myfile.bin";
        var input = new MemoryStream(new byte[] { 10, 20, 30 });

        // Act
        var returnedName = await sut.LoadFile(input, null, providedName);

        // Assert
        Assert.That(returnedName, Is.EqualTo(providedName));
        var expectedPath = Path.Combine(_launcher.InstallationDirectory, "Attachments", providedName);
        Assert.That(File.Exists(expectedPath), Is.True, "File not written to expected path");

        // Storage should have metadata
        var stored = await _storage.GetAsync<LocalFileInfo>(providedName);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Name, Is.EqualTo(providedName));
        Assert.That(stored.FullPath, Is.EqualTo(expectedPath));
    }

    [Test]
    public async Task GetFileStream_LocalStorage_FileExists_ReturnsStreamWithNameAndLength()
    {
        // Arrange
        var name = "data.dat";
        var dir = Path.Combine(_launcher.InstallationDirectory, "Attachments");
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, name);
        var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        await File.WriteAllBytesAsync(fullPath, bytes);

        await _storage.SetAsync(name, new LocalFileInfo
        {
            Name = name,
            Directory = dir,
            FullPath = fullPath
        });

        var sut = CreateSut();

        // Act
        var (stream, fileName, length) = await sut.GetFileStream(name);

        // Assert
        Assert.That(fileName, Is.EqualTo(name));
        Assert.That(length, Is.EqualTo(bytes.Length));
        Assert.That(stream.Position, Is.EqualTo(0));
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.That(ms.ToArray(), Is.EqualTo(bytes));
    }

    [Test]
    public async Task GetFileStream_LocalStorage_StorageEntryButFileMissing_ReturnsEmpty()
    {
        // Arrange: set storage but no physical file
        var name = "missing.bin";
        await _storage.SetAsync(name, new LocalFileInfo
        {
            Name = name,
            Directory = _launcher.InstallationDirectory,
            FullPath = Path.Combine(_launcher.InstallationDirectory, name)
        });
        var sut = CreateSut();

        // Act
        var (stream, fileName, length) = await sut.GetFileStream(name);

        // Assert
        Assert.That(fileName, Is.EqualTo(string.Empty));
        Assert.That(length, Is.EqualTo(0));
        Assert.That(stream.Length, Is.EqualTo(0));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public async Task CheckFileExists_LocalStorage_TrueWhenStored()
    {
        var key = "exists.key";
        await _storage.SetAsync(key, new LocalFileInfo { Name = key, Directory = "", FullPath = "" });
        var sut = CreateSut();

        var exists = await sut.CheckFileExists("ignored", key);
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task CheckFileExists_LocalStorage_FalseWhenMissing()
    {
        var sut = CreateSut();
        var exists = await sut.CheckFileExists("ignored", "nope");
        Assert.That(exists, Is.False);
    }

    // ===== Test doubles =====
    private class FakeStorageService : IStorageService
    {
        private readonly Dictionary<string, object?> _data = new();

        public Task SetAsync<T>(string key, T? value)
        {
            _data[key] = value;
            return Task.CompletedTask;
        }

        public Task<T?> GetAsync<T>(string key, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            if (_data.TryGetValue(key, out var value))
                return Task.FromResult((T?)value);
            return Task.FromResult(default(T?));
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

    private class TestStorageSettings : IStorageSettings
    {
        public StorageType StorageType { get; set; }
        public string StorageHost { get; set; } = string.Empty;
        public string StorageLogin { get; set; } = string.Empty;
        public string StoragePassword { get; set; } = string.Empty;
        public TextureProtocol TextureProtocol { get; set; }
        public bool SentryNeedAutoClear { get; set; }
        public TimeSpan SentryAutoClearPeriod { get; set; }
    }

    private class TestObservable : IObservable<IStorageSettings>
    {
        private readonly List<IObserver<IStorageSettings>> _observers = new();

        public IDisposable Subscribe(IObserver<IStorageSettings> observer)
        {
            if (!_observers.Contains(observer)) _observers.Add(observer);
            return new SimpleDisposable(() => _observers.Remove(observer));
        }

        public void OnNext(IStorageSettings value)
        {
            foreach (var o in _observers.ToArray()) o.OnNext(value);
        }

        public void OnCompleted()
        {
            foreach (var o in _observers.ToArray()) o.OnCompleted();
            _observers.Clear();
        }

        public void OnError(Exception ex)
        {
            foreach (var o in _observers.ToArray()) o.OnError(ex);
        }

        private class SimpleDisposable : IDisposable
        {
            private readonly Action _dispose;

            public SimpleDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }

    private class TestLauncherInfo : ILauncherInfo
    {
        public string Name { get; } = "test";
        public string BaseDirectory { get; } = string.Empty;
        public string InstallationDirectory { get; set; } = string.Empty;
        public IStorageSettings StorageSettings { get; set; } = new TestStorageSettings();
        public Dictionary<string, IVersionFile?> ActualLauncherVersion { get; set; } = new();
        public IGmlSettings Settings => throw new NotImplementedException();
        public IObservable<IStorageSettings> SettingsUpdated { get; } = new TestObservable();
        public IDictionary<string, string> AccessTokens { get; set; } = new Dictionary<string, string>();

        public void UpdateSettings(StorageType storageType, string storageHost, string storageLogin,
            string storagePassword, TextureProtocol textureProtocol, string curseForgeKey, string vkKey,
            TimeSpan sentryClearPeriod, bool sentryNeedAutoClear)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ILauncherBuild>> GetBuilds()
        {
            throw new NotImplementedException();
        }

        public Task<ILauncherBuild?> GetBuild(string name)
        {
            throw new NotImplementedException();
        }
    }

    private class FakeBugTracker : IBugTrackerProcedures
    {
        public List<Exception> Exceptions { get; } = new();

        public void CaptureException(IBugInfo bugInfo)
        {
            /* not needed */
        }

        public IBugInfo CaptureException(Exception exception)
        {
            Exceptions.Add(exception);
            return null!;
        }

        public Task<IEnumerable<IBugInfo>> GetAllBugs()
        {
            return Task.FromResult<IEnumerable<IBugInfo>>(Array.Empty<IBugInfo>());
        }

        public Task<IBugInfo?> GetBugId(Guid id)
        {
            return Task.FromResult<IBugInfo?>(null);
        }

        public Task<IEnumerable<IBugInfo>> GetFilteredBugs(Expression<Func<IStorageBug, bool>> filter)
        {
            return Task.FromResult<IEnumerable<IBugInfo>>(Array.Empty<IBugInfo>());
        }

        public Task SolveAllAsync()
        {
            return Task.CompletedTask;
        }
    }
}
