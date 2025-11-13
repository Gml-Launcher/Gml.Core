using System.Net;
using System.Reflection;
using Gml.Models.User;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Auth;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.User;

namespace GmlCore.Tests;

public class UserEntityTests
{
    private FakeGmlManager _manager = null!;
    private User _user = null!;

    [SetUp]
    public void SetUp()
    {
        _manager = new FakeGmlManager();
        _user = new User
        {
            Name = "TestUser",
            ServerUuid = "server-uuid",
            Manager = _manager
        };
    }

    [Test]
    public async Task Block_SetsFlags_And_CallsUpdateUser()
    {
        await _user.Block(true);

        Assert.That(_user.IsBanned, Is.True);
        Assert.That(_user.IsBannedPermanent, Is.True);
        Assert.That(_manager.UsersMock.UpdateCalls, Is.EqualTo(1));
        Assert.That(_manager.UsersMock.LastUpdatedUser, Is.SameAs(_user));
    }

    [Test]
    public async Task Unblock_ClearsIsBanned_And_ResetsPermanent_WhenTrue_And_CallsUpdate()
    {
        // Arrange: set as banned and permanent first
        _user.IsBanned = true;
        _user.IsBannedPermanent = true;

        // Act
        await _user.Unblock(true);

        // Assert
        Assert.That(_user.IsBanned, Is.False);
        Assert.That(_user.IsBannedPermanent, Is.False);
        Assert.That(_manager.UsersMock.UpdateCalls, Is.EqualTo(1));
        Assert.That(_manager.UsersMock.LastUpdatedUser, Is.SameAs(_user));
    }

    [Test]
    public async Task Unblock_ClearsIsBanned_LeavesPermanent_WhenFalse_And_CallsUpdate()
    {
        _user.IsBanned = true;
        _user.IsBannedPermanent = true;

        await _user.Unblock(false);

        Assert.That(_user.IsBanned, Is.False);
        Assert.That(_user.IsBannedPermanent, Is.True);
        Assert.That(_manager.UsersMock.UpdateCalls, Is.EqualTo(1));
        Assert.That(_manager.UsersMock.LastUpdatedUser, Is.SameAs(_user));
    }

    [Test]
    public async Task SaveUserAsync_CallsUpdateUser_WithSameInstance()
    {
        await _user.SaveUserAsync();

        Assert.That(_manager.UsersMock.UpdateCalls, Is.EqualTo(1));
        Assert.That(_manager.UsersMock.LastUpdatedUser, Is.SameAs(_user));
    }

    [Test]
    public async Task DownloadAndInstallSkinAsync_SetsUrlsAndGuid_And_BuildsExternalUrl_WithHostAndPort()
    {
        // Arrange
        var originalUrl = "http://textures.local/original/path";
        _manager.IntegrationsMock.TextureProviderMock.NextSkinUrl = originalUrl;

        // Act
        await _user.DownloadAndInstallSkinAsync("http://source/skin.png", "external.example.com:1234");

        // Assert
        Assert.That(_user.TextureSkinUrl, Is.EqualTo(originalUrl));
        Assert.That(_user.TextureSkinGuid, Is.Not.Null);
        Assert.That(_user.TextureSkinGuid, Is.Not.Empty);

        Assert.That(_user.ExternalTextureSkinUrl, Is.Not.Null.And.Not.Empty);
        var ext = new Uri(_user.ExternalTextureSkinUrl!);
        Assert.That(ext.Host, Is.EqualTo("external.example.com"));
        Assert.That(ext.Port, Is.EqualTo(1234));
        Assert.That(ext.AbsolutePath, Does.StartWith("/api/v1/integrations/texture/skins/"));
        Assert.That(ext.AbsolutePath, Does.Contain(_user.TextureSkinGuid));
    }

    [Test]
    public async Task DownloadAndInstallSkinAsync_SetsExternalUrl_WithHostOnly_DefaultPort()
    {
        var originalUrl = "http://textures.local/foo";
        _manager.IntegrationsMock.TextureProviderMock.NextSkinUrl = originalUrl;

        await _user.DownloadAndInstallSkinAsync("http://src/skin.png", "public.host");

        Assert.That(_user.ExternalTextureSkinUrl, Is.Not.Null.And.Not.Empty);
        var ext = new Uri(_user.ExternalTextureSkinUrl!);
        Assert.That(ext.Host, Is.EqualTo("public.host"));
        // Default HTTP port should be 80
        Assert.That(ext.Port, Is.EqualTo(80));
        Assert.That(ext.Scheme, Is.EqualTo("http"));
    }

    [Test]
    public async Task DownloadAndInstallSkinAsync_EmptyProviderResult_SetsEmptyGuid_And_NoExternalUrl()
    {
        _manager.IntegrationsMock.TextureProviderMock.NextSkinUrl = string.Empty;

        await _user.DownloadAndInstallSkinAsync("http://src/skin.png", "public.host");

        Assert.That(_user.TextureSkinGuid, Is.EqualTo(string.Empty));
        Assert.That(_user.ExternalTextureSkinUrl, Is.Null.Or.Empty);
    }

    [Test]
    public async Task DownloadAndInstallCloakAsync_SetsUrlsAndGuid_And_BuildsExternalUrl_WithHostReplacement()
    {
        var originalUrl = "http://textures.local/original/cape";
        _manager.IntegrationsMock.TextureProviderMock.NextCloakUrl = originalUrl;

        await _user.DownloadAndInstallCloakAsync("http://source/cape.png", "ext.host:9000");

        Assert.That(_user.TextureCloakUrl, Is.EqualTo(originalUrl));
        Assert.That(_user.TextureCloakGuid, Is.Not.Null.And.Not.Empty);
        Assert.That(_user.ExternalTextureCloakUrl, Is.Not.Null.And.Not.Empty);

        var ext = new Uri(_user.ExternalTextureCloakUrl!);
        Assert.That(ext.Host, Is.EqualTo("ext.host"));
        Assert.That(ext.Port, Is.EqualTo(9000));
        Assert.That(ext.AbsolutePath, Does.StartWith("/api/v1/integrations/texture/capes/"));
        Assert.That(ext.AbsolutePath, Does.Contain(_user.TextureCloakGuid));
    }

    [Test]
    public async Task DownloadAndInstallCloakAsync_EmptyProviderResult_SetsEmptyGuid_And_NoExternalUrl()
    {
        _manager.IntegrationsMock.TextureProviderMock.NextCloakUrl = "";

        await _user.DownloadAndInstallCloakAsync("http://source/cape.png", "ext.host");

        Assert.That(_user.TextureCloakGuid, Is.EqualTo(string.Empty));
        Assert.That(_user.ExternalTextureCloakUrl, Is.Null.Or.Empty);
    }

    [Test]
    public void IsValid_InternalProperty_ReflectsExpiredDate()
    {
        // Access internal property via reflection
        var prop = typeof(User).GetProperty("IsValid", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(prop, Is.Not.Null);

        _user.ExpiredDate = DateTime.MinValue;
        Assert.That((bool)prop!.GetValue(_user)!, Is.False);

        _user.ExpiredDate = DateTime.Now.AddMinutes(-1); // past (align with implementation using DateTime.Now)
        Assert.That((bool)prop.GetValue(_user)!, Is.False);

        _user.ExpiredDate = DateTime.Now.AddMinutes(10); // future (align with implementation using DateTime.Now)
        Assert.That((bool)prop.GetValue(_user)!, Is.True);
    }

    // ===== Test Doubles =====
    private class FakeGmlManager : IGmlManager
    {
        public FakeUsersProcedures UsersMock { get; } = new();
        public FakeIntegrations IntegrationsMock { get; } = new();

        public ILauncherInfo LauncherInfo => throw new NotImplementedException();
        public IBugTrackerProcedures BugTracker => throw new NotImplementedException();
        public IProfileProcedures Profiles => throw new NotImplementedException();
        public IFileStorageProcedures Files => throw new NotImplementedException();
        public IServicesIntegrationProcedures Integrations => IntegrationsMock;
        public IUserProcedures Users => UsersMock;
        public ILauncherProcedures Launcher => throw new NotImplementedException();
        public IProfileServersProcedures Servers => throw new NotImplementedException();
        public INotificationProcedures Notifications => throw new NotImplementedException();
        public IModsProcedures Mods => throw new NotImplementedException();
        public IStorageService Storage => throw new NotImplementedException();

        public void RestoreSettings<T>() where T : IVersionFile
        {
            throw new NotImplementedException();
        }
    }

    private class FakeUsersProcedures : IUserProcedures
    {
        public int UpdateCalls { get; private set; }
        public IUser? LastUpdatedUser { get; private set; }

        public Task<IUser> GetAuthData(string login, string password, string device, string protocol,
            IPAddress? address, string? customUuid, string? hwid, bool isSlim)
        {
            throw new NotImplementedException();
        }

        public Task<IUser?> GetUserByUuid(string uuid)
        {
            throw new NotImplementedException();
        }

        public Task<IUser?> GetUserByName(string userName)
        {
            throw new NotImplementedException();
        }

        public Task<IUser?> GetUserBySkinGuid(string guid)
        {
            throw new NotImplementedException();
        }

        public Task<IUser?> GetUserByCloakGuid(string guid)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateUser(string userUuid, string uuid, string accessToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanJoinToServer(IUser user, string serverId)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IUser>> GetUsers()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IUser>> GetUsers(int take, int offset, string findName)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IUser>> GetUsers(IEnumerable<string> userUuids)
        {
            throw new NotImplementedException();
        }

        public Task UpdateUser(IUser user)
        {
            UpdateCalls++;
            LastUpdatedUser = user;
            return Task.CompletedTask;
        }

        public Task RemoveUser(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task StartSession(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task EndSession(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetSkin(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetCloak(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetHead(IUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IUser?> GetUserByAccessToken(string accessToken)
        {
            throw new NotImplementedException();
        }

        public Task BlockHardware(IEnumerable<string?> hwids)
        {
            throw new NotImplementedException();
        }

        public Task UnblockHardware(IEnumerable<string?> hwids)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckContainsHardware(IHardware hardware)
        {
            throw new NotImplementedException();
        }
    }

    private class FakeIntegrations : IServicesIntegrationProcedures
    {
        public FakeTextureProvider TextureProviderMock { get; } = new();

        public ITextureProvider TextureProvider
        {
            get => TextureProviderMock;
            set => throw new NotImplementedException();
        }

        public INewsListenerProvider NewsProvider { get; set; } = null!;

        public Task<AuthType> GetAuthType()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IAuthServiceInfo>> GetAuthServices()
        {
            throw new NotImplementedException();
        }

        public Task<IAuthServiceInfo?> GetActiveAuthService()
        {
            throw new NotImplementedException();
        }

        public Task<IAuthServiceInfo?> GetAuthService(AuthType authType)
        {
            throw new NotImplementedException();
        }

        public Task SetActiveAuthService(IAuthServiceInfo? service)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSkinServiceAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetCloakServiceAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetSkinServiceAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task SetCloakServiceAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetSentryService()
        {
            throw new NotImplementedException();
        }

        public Task SetSentryService(string url)
        {
            throw new NotImplementedException();
        }

        public Task UpdateDiscordRpc(IDiscordRpcClient client)
        {
            throw new NotImplementedException();
        }

        public Task<IDiscordRpcClient?> GetDiscordRpc()
        {
            throw new NotImplementedException();
        }
    }

    private class FakeTextureProvider : ITextureProvider
    {
        public string NextSkinUrl { get; set; } = string.Empty;
        public string NextCloakUrl { get; set; } = string.Empty;

        public Task<string> SetSkin(IUser user, string skinUrl)
        {
            return Task.FromResult(NextSkinUrl);
        }

        public Task<string> SetCloak(IUser user, string skinUrl)
        {
            return Task.FromResult(NextCloakUrl);
        }

        public Task<Stream> GetSkinStream(string? textureUrl)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetCloakStream(string? userTextureSkinUrl)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetHeadByNameStream(string? userName)
        {
            throw new NotImplementedException();
        }
    }
}
