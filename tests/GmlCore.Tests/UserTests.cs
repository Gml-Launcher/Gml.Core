using System.Net;
using Gml;
using Gml.Core.Launcher;
using Gml.Core.User;
using GmlCore.Interfaces;

namespace GmlCore.Tests;

public class UserProceduresTests
{
    private const string LauncherName = "GmlServer";
    private const string SecurityKey = "gfweagertghuysergfbsuyerbgiuyserg";
    private const string TestUserName = "testUser";
    private const string TestUserUuid = "F8766986-42F6-3344-B8B4-3AF65B24FA64";
    private const string TestServerUuid = "8D4E6F2A-1B3C-4D5E-9F8A-7C6B5D4E3F2A";

    private IGmlManager _gmlManager;

    [SetUp]
    public async Task SetupOnce()
    {
        _gmlManager = new GmlManager(new GmlSettings(LauncherName, SecurityKey, httpClient: new HttpClient())
        {
            TextureServiceEndpoint = "http://gml-web-skins:8085"
        });

        var service = await _gmlManager.Integrations.GetAuthServices();
        await _gmlManager.Integrations.SetActiveAuthService(service.First(s => s.Name == "Any"));

        var user = await _gmlManager.Users.GetUserByUuid(TestUserUuid);

        if (user is null)
        {
            await _gmlManager.Users.GetAuthData(TestUserName, TestUserName, "Desktop", "1.0", IPAddress.Parse("127.0.0.1"), null, null);
        }
    }

    [Test]
    public async Task GetAuthData_ReturnsValidUser()
    {
        // Arrange
        string login = TestUserName;
        string password = "testPassword";
        string device = "Desktop";
        string protocol = "1.0";
        IPAddress address = IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _gmlManager.Users.GetAuthData(login, password, device, protocol, address, null, null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(TestUserName));
        Assert.That(result.Uuid, Is.EqualTo(TestUserUuid));
    }

    [Test]
    public async Task GetAuthData_WithCustomUuid_ReturnsValidUser()
    {
        // Arrange
        string login = TestUserName;
        string password = "testPassword";
        string device = "Desktop";
        string protocol = "1.0";
        string customUuid = "custom-uuid";

        // Act
        var result = await _gmlManager.Users.GetAuthData(login, password, device, protocol, null, customUuid, null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Uuid, Is.EqualTo(customUuid));
    }

    [Test]
    public async Task GetUserByUuid_ExistingUuid_ReturnsUser()
    {
        // Arrange
        string uuid = TestUserUuid;

        // Act
        var result = await _gmlManager.Users.GetUserByUuid(uuid);

        // Assert
        Assert.NotNull(result);
        Assert.That(result.Uuid, Is.EqualTo(uuid));
    }

    [Test]
    public async Task GetUserByUuid_NonExistingUuid_ReturnsNull()
    {
        // Arrange
        string uuid = "non-existing-uuid";

        // Act
        var result = await _gmlManager.Users.GetUserByUuid(uuid);

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task GetUserByName_ExistingName_ReturnsUser()
    {
        // Act
        var result = await _gmlManager.Users.GetUserByName(TestUserName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(TestUserName));
    }

    [Test]
    public async Task GetUserByName_NonExistingName_ReturnsNull()
    {
        // Act
        var result = await _gmlManager.Users.GetUserByName("nonexistent");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetUserBySkinGuid_ExistingGuid_ReturnsUser()
    {
        // Arrange
        var guid = (await _gmlManager.Users.GetUserByUuid(TestUserUuid))?.TextureSkinGuid;

        // Act
        var result = await _gmlManager.Users.GetUserBySkinGuid(guid!);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetUserByCloakGuid_ExistingGuid_ReturnsUser()
    {
        // Arrange
        var guid = (await _gmlManager.Users.GetUserByUuid(TestUserUuid))?.TextureCloakGuid;

        // Act
        var result = await _gmlManager.Users.GetUserByCloakGuid(guid!);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task ValidateUser_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var user = await _gmlManager.Users.GetUserByUuid(TestUserUuid) ?? throw new Exception();

        // Act
        var result = await _gmlManager.Users.ValidateUser(user.Uuid, TestServerUuid, user.AccessToken);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public async Task ValidateUser_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        var userUuid = TestUserUuid;
        var uuid = TestUserUuid;
        var accessToken = "invalid-token";

        // Act
        var result = await _gmlManager.Users.ValidateUser(userUuid, uuid, accessToken);

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task CanJoinToServer_ValidUser_ReturnsTrue()
    {
        // Arrange
        var user = await _gmlManager.Users.GetUserByUuid(TestUserUuid);
        var serverId = TestServerUuid;

        // Act
        user!.ServerUuid = serverId;
        user.ServerExpiredDate = DateTime.MaxValue;
        await _gmlManager.Users.UpdateUser(user);
        var result = await _gmlManager.Users.CanJoinToServer(user, serverId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Act
        var result = (await _gmlManager.Users.GetUsers()).ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.IsTrue(result.Any(u => u.Name == TestUserName));
    }

    [Test]
    public async Task GetUsers_WithPagination_ReturnsCorrectUsers()
    {
        // Arrange
        var take = 2;
        var offset = 0;
        var findName = "user";

        // Act
        var result = await _gmlManager.Users.GetUsers(take, offset, findName);

        // Assert
        Assert.NotNull(result);
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetUsers_ByUuids_ReturnsMatchingUsers()
    {
        // Arrange
        var uuids = new[] { TestUserUuid };

        // Act
        var result = (await _gmlManager.Users.GetUsers(uuids)).ToList();

        // Assert
        Assert.Multiple(() =>
        {

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Uuid, Is.EqualTo(TestUserUuid));
        });
    }

    // [Test]
    // public async Task GetSkin_ValidUser_ReturnsStream()
    // {
    //     // Arrange
    //     var mockUser = new User();
    //
    //     // Act
    //     var result = await _gmlManager.Users.GetSkin(mockUser);
    //
    //     // Assert
    //     Assert.That(result, Is.Not.Null);
    //     Assert.That(result, Is.InstanceOf<Stream>());
    //
    //     // Проверка содержимого потока
    //     result.Position = 0;
    //     var buffer = new byte[5];
    //     await result.ReadExactlyAsync(buffer, 0, buffer.Length);
    //     Assert.That(buffer[0], Is.EqualTo(1));
    //     Assert.That(buffer[1], Is.EqualTo(2));
    // }

    // [Test]
    // public async Task GetCloak_ValidUser_ReturnsStream()
    // {
    //     // Arrange
    //     var mockUser = new User();
    //
    //     // Act
    //     var result = await _gmlManager.Users.GetCloak(mockUser);
    //
    //     // Assert
    //     Assert.That(result, Is.Not.Null);
    //     Assert.That(result, Is.InstanceOf<Stream>());
    // }

    // [Test]
    // public async Task GetHead_ValidUser_ReturnsStream()
    // {
    //     // Arrange
    //     var mockUser = new User();
    //
    //     // Act
    //     var result = await _gmlManager.Users.GetHead(mockUser);
    //
    //     // Assert
    //     Assert.That(result, Is.Not.Null);
    //     Assert.That(result, Is.InstanceOf<Stream>());
    // }

    [Test]
    public async Task UpdateUser_ValidUser_CompletesSuccessfully()
    {
        // Arrange
        var user = await _gmlManager.Users.GetUserByUuid(TestUserUuid) ?? throw new Exception();

        user.TextureSkinGuid = "new-skin-guid";
        user.TextureCloakGuid = "new-cloak-guid";

        // Act
        await _gmlManager.Users.UpdateUser(user);

        // Assert
        var updatedUser = await _gmlManager.Users.GetUserByUuid(TestUserUuid);
        Assert.That(updatedUser, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedUser.TextureSkinGuid, Is.EqualTo("new-skin-guid"));
            Assert.That(updatedUser.TextureCloakGuid, Is.EqualTo("new-cloak-guid"));
        });
    }

    [Test]
    public async Task RemoveUser_ValidUser_CompletesSuccessfully()
    {
        // Arrange
        var mockUser = new User { Uuid = "user-to-remove" };

        // Act
        await _gmlManager.Users.RemoveUser(mockUser);
    }

    [Test]
    public async Task EndSession_ValidUser_CompletesSuccessfully()
    {
        // Arrange
        var user = await _gmlManager.Users.GetUserByUuid(TestUserUuid) ?? throw new Exception();

        // Act
        await _gmlManager.Users.StartSession(user);
        await _gmlManager.Users.EndSession(user);
    }

    [Test]
    public async Task GetUserByAccessToken_ValidToken_ReturnsUser()
    {
        // Arrange
        var accessToken = await _gmlManager.Users.GetUserByUuid(TestUserUuid) ?? throw new Exception();

        // Act
        var result = await _gmlManager.Users.GetUserByAccessToken(accessToken.AccessToken);

        // Assert
        Assert.That(result, Is.Not.Null);
    }


    [Test]
    public void Empty_ReturnsUserWithDefaultValues()
    {
        // Act
        var emptyUser = User.Empty;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(emptyUser.Name, Is.Not.Empty);
            Assert.That(emptyUser.Uuid, Is.Not.Empty);
            Assert.That(emptyUser.AccessToken, Is.Not.Empty);
        });
    }
}
