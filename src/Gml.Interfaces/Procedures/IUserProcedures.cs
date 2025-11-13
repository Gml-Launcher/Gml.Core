using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using GmlCore.Interfaces.User;

namespace GmlCore.Interfaces.Procedures
{
    public interface IUserProcedures
    {
        Task<IUser> GetAuthData(
            string login,
            string password,
            string device,
            string protocol,
            IPAddress? address,
            string? customUuid,
            string? hwid,
            bool isSlim);
        Task<IUser?> GetUserByUuid(string uuid);
        Task<IUser?> GetUserByName(string userName);
        Task<IUser?> GetUserBySkinGuid(string guid);
        Task<IUser?> GetUserByCloakGuid(string guid);
        Task<bool> ValidateUser(string userUuid, string uuid, string accessToken);
        Task<bool> CanJoinToServer(IUser user, string serverId);
        Task<IReadOnlyCollection<IUser>> GetUsers();
        Task<IReadOnlyCollection<IUser>> GetUsers(int take, int offset, string findName);
        Task<IReadOnlyCollection<IUser>> GetUsers(IEnumerable<string> userUuids);
        Task UpdateUser(IUser user);
        Task RemoveUser(IUser user);
        Task StartSession(IUser user);
        Task EndSession(IUser user);
        Task<Stream> GetSkin(IUser user);
        Task<Stream> GetCloak(IUser user);
        Task<Stream> GetHead(IUser user);
        Task<IUser?> GetUserByAccessToken(string accessToken);
        Task BlockHardware(IEnumerable<string?> hwids);
        Task UnblockHardware(IEnumerable<string?> hwids);

        /// <summary>
        /// Checks whether the specified hardware is blocked.
        /// </summary>
        /// <param name="hardware">Hardware identifiers to check.</param>
        /// <returns>True if blocked; otherwise, false.</returns>
        Task<bool> CheckContainsHardware(IHardware hardware);
    }
}
