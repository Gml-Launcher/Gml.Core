using System.Net;
using Gml.Core.User;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.User;

namespace GmlCore.Tests;

public class AuthUserTests
{
    private static AuthUser CreateUserWithManager(FakeUserProcedures fakeUsers)
    {
        var user = new AuthUser
        {
            Name = "TestUser",
            Uuid = "UUID-1",
            Manager = new FakeGmlManager(fakeUsers)
        };
        return user;
    }

    [Test]
    public async Task Block_Permanent_BlocksHardwareAndUpdatesUser()
    {
        var fakeUsers = new FakeUserProcedures();
        var user = CreateUserWithManager(fakeUsers);
        user.AuthHistory.AddRange(new[]
        {
            new AuthUserHistory { Hwid = "HW1" },
            new AuthUserHistory { Hwid = "HW2" },
            new AuthUserHistory { Hwid = "HW1" }, // duplicate
            new AuthUserHistory { Hwid = null } // null allowed
        });

        await user.Block(true);

        // Assert state
        Assert.That(user.IsBanned, Is.True);
        Assert.That(user.IsBannedPermanent, Is.True);

        // Assert UpdateUser called
        Assert.That(fakeUsers.UpdateUserCalls.Count, Is.EqualTo(1));
        Assert.That(ReferenceEquals(fakeUsers.UpdateUserCalls[0], user), Is.True);

        // Assert hardware blocked with distinct values (including single null)
        Assert.That(fakeUsers.BlockHardwareCalls.Count, Is.EqualTo(1));
        var hwids = fakeUsers.BlockHardwareCalls[0].ToArray();
        // Must contain HW1 and HW2
        Assert.That(hwids, Does.Contain("HW1"));
        Assert.That(hwids, Does.Contain("HW2"));
        // Distinct: only one null occurrence if present
        Assert.That(hwids.Count(h => h is null) <= 1, Is.True);
    }

    [Test]
    public async Task Block_Temporary_UpdatesUserOnly()
    {
        var fakeUsers = new FakeUserProcedures();
        var user = CreateUserWithManager(fakeUsers);
        user.AuthHistory.Add(new AuthUserHistory { Hwid = "HW1" });

        await user.Block(false);

        Assert.That(user.IsBanned, Is.True);
        Assert.That(user.IsBannedPermanent, Is.False);
        Assert.That(fakeUsers.UpdateUserCalls.Count, Is.EqualTo(1));
        Assert.That(fakeUsers.BlockHardwareCalls.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task Unblock_Permanent_UnblocksHardwareAndClearsPermanentFlag()
    {
        var fakeUsers = new FakeUserProcedures();
        var user = CreateUserWithManager(fakeUsers);
        user.IsBanned = true;
        user.IsBannedPermanent = true;
        user.AuthHistory.AddRange(new[]
        {
            new AuthUserHistory { Hwid = "HW1" },
            new AuthUserHistory { Hwid = "HW2" },
            new AuthUserHistory { Hwid = "HW2" }
        });

        await user.Unblock(true);

        Assert.That(user.IsBanned, Is.False);
        Assert.That(user.IsBannedPermanent, Is.False);
        Assert.That(fakeUsers.UpdateUserCalls.Count, Is.EqualTo(1));
        Assert.That(fakeUsers.UnblockHardwareCalls.Count, Is.EqualTo(1));
        var hwids = fakeUsers.UnblockHardwareCalls[0].ToArray();
        Assert.That(hwids, Is.EquivalentTo(new[] { "HW1", "HW2" }));
    }

    [Test]
    public async Task Unblock_Temporary_DoesNotTouchHardwareAndKeepsPermanentFlag()
    {
        var fakeUsers = new FakeUserProcedures();
        var user = CreateUserWithManager(fakeUsers);
        user.IsBanned = true;
        user.IsBannedPermanent = true; // should stay as is on temporary unblock
        user.AuthHistory.Add(new AuthUserHistory { Hwid = "HW1" });

        await user.Unblock(false);

        Assert.That(user.IsBanned, Is.False);
        Assert.That(user.IsBannedPermanent, Is.True);
        Assert.That(fakeUsers.UpdateUserCalls.Count, Is.EqualTo(1));
        Assert.That(fakeUsers.UnblockHardwareCalls.Count, Is.EqualTo(0));
    }

    private class FakeUserProcedures : IUserProcedures
    {
        public List<IEnumerable<string?>> BlockHardwareCalls { get; } = new();
        public List<IEnumerable<string?>> UnblockHardwareCalls { get; } = new();
        public List<IUser> UpdateUserCalls { get; } = new();

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
            UpdateUserCalls.Add(user);
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
            BlockHardwareCalls.Add(hwids.ToArray());
            return Task.CompletedTask;
        }

        public Task UnblockHardware(IEnumerable<string?> hwids)
        {
            UnblockHardwareCalls.Add(hwids.ToArray());
            return Task.CompletedTask;
        }

        public Task<bool> CheckContainsHardware(IHardware hardware)
        {
            throw new NotImplementedException();
        }
    }

    private class FakeGmlManager : IGmlManager
    {
        public FakeGmlManager(IUserProcedures users)
        {
            Users = users;
        }

        public ILauncherInfo LauncherInfo => throw new NotImplementedException();
        public IBugTrackerProcedures BugTracker => throw new NotImplementedException();
        public IProfileProcedures Profiles => throw new NotImplementedException();
        public IFileStorageProcedures Files => throw new NotImplementedException();
        public IServicesIntegrationProcedures Integrations => throw new NotImplementedException();
        public IUserProcedures Users { get; }
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
}
