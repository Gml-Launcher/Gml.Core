using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Core.User;
using Gml.Models.Converters;
using Gml.Models.Sessions;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.User;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Gml.Core.Helpers.User
{
    public class UserProcedures : IUserProcedures
    {
        private readonly IGmlSettings _settings;
        private readonly IStorageService _storage;
        private readonly GmlManager _gmlManager;

        public UserProcedures(IGmlSettings settings, IStorageService storage, GmlManager gmlManager)
        {
            _settings = settings;
            _storage = storage;
            _gmlManager = gmlManager;
        }

        public async Task<IUser> GetAuthData(string login,
            string password,
            string device,
            string protocol,
            IPAddress? address,
            string? customUuid,
            string? hwid,
            bool isSlim)
        {
            var authUser = await _storage.GetUserAsync<AuthUser>(login, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            }) ?? new AuthUser
            {
                Name = login
            };

            authUser.AuthHistory.Add(AuthUserHistory.Create(device, protocol, hwid, address?.ToString()));
            authUser.Uuid = customUuid ?? UsernameToUuid(login);
            authUser.ExpiredDate = DateTime.Now + TimeSpan.FromDays(30);
            authUser.Manager = _gmlManager;
            authUser.IsSlim = isSlim;

            await _storage.SetUserAsync(login, authUser.Uuid, authUser);

            return authUser;
        }

        public async Task<IUser?> GetUserByUuid(string uuid)
        {
            var user = await _storage.GetUserByUuidAsync<AuthUser>(uuid, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });

            if (user is not null)
            {
                user.Manager = _gmlManager;
            }

            return user;
        }

        public async Task<IUser?> GetUserByName(string userName)
        {
            return await _storage.GetUserByNameAsync<AuthUser>(userName, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });
        }

        public async Task<IUser?> GetUserBySkinGuid(string guid)
        {
            return await _storage.GetUserBySkinAsync<AuthUser>(guid, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });
        }

        public async Task<IUser?> GetUserByCloakGuid(string guid)
        {
            return await _storage.GetUserByCloakAsync<AuthUser>(guid, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });
        }

        public async Task<bool> ValidateUser(string userUuid, string serverUuid, string accessToken)
        {
            if (await GetUserByUuid(Guid.Parse(userUuid).ToString().ToUpper()) is not AuthUser user)
            {
                return false;
            }

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(user.AccessToken))
                return false;

            if (user.IsBanned)
                return false;

            var jwtToken = handler.ReadJwtToken(user.AccessToken);

            var claims = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

            if (claims?.Value != user.Name)
                return false;

            user.ServerUuid = serverUuid;
            user.ServerExpiredDate = DateTime.Now.AddMinutes(1);
            user.ServerJoinHistory.Add(new ServerJoinHistory(serverUuid, DateTime.Now));

            await UpdateUser(user);

            return user.AccessToken?.Equals(accessToken) ?? false;
        }

        public async Task<bool> CanJoinToServer(IUser user, string serverId)
        {
            var isSuccess = user.ServerUuid == serverId && DateTime.Now <= user.ServerExpiredDate;

            if (isSuccess)
            {
                user.ServerExpiredDate = DateTime.MinValue;
                user.ServerUuid = string.Empty;
                await _storage.SetUserAsync(user.Name, user.Uuid, user);
            }

            return isSuccess;
        }

        public async Task<IEnumerable<IUser>> GetUsers()
        {
            return await _storage.GetUsersAsync<AuthUser>(new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });
        }

        public async Task<IReadOnlyCollection<IUser>> GetUsers(int take, int offset, string findName)
        {
            var authUsers = await _storage.GetUsersAsync<AuthUser>(new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            }, take, offset, findName).ConfigureAwait(false);

            return authUsers.ToArray();
        }

        public async Task<IReadOnlyCollection<IUser>> GetUsers(IEnumerable<string> userUuids)
        {
            var users = await _storage.GetUsersAsync<AuthUser>(new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            }, userUuids).ConfigureAwait(false);

            return users.ToArray();
        }

        public Task UpdateUser(IUser user)
        {
            return _storage.SetUserAsync(user.Name, user.Uuid, (AuthUser)user);
        }

        public Task RemoveUser(IUser user)
        {
            return _storage.RemoveUserByUuidAsync(user.Uuid);
        }

        public Task StartSession(IUser user)
        {
            user.Sessions.Add(new GameSession());

            return UpdateUser(user);
        }

        public Task EndSession(IUser user)
        {
            user.Sessions.Last().EndDate = DateTimeOffset.Now;

            return UpdateUser(user);
        }

        public Task<Stream> GetSkin(IUser user)
        {
            return _gmlManager.Integrations.TextureProvider.GetSkinStream(user.TextureSkinUrl);
        }

        public Task<Stream> GetCloak(IUser user)
        {
            return _gmlManager.Integrations.TextureProvider.GetCloakStream(user.TextureCloakUrl);
        }

        public Task<Stream> GetHead(IUser user)
        {
            return _gmlManager.Integrations.TextureProvider.GetHeadByNameStream(user.Name);
        }

        public async Task<IUser?> GetUserByAccessToken(string accessToken)
        {
            var user = await _storage.GetUserByAccessToken<AuthUser>(accessToken, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });

            if (user is not null)
            {
                user.Manager = _gmlManager;

            }

            return user;
        }

        public async Task BlockHardware(IEnumerable<string?> hwids)
        {
            foreach (var hwid in hwids)
            {
                await _storage.AddLockedHwid(new Hardware(hwid));
            }
        }

        public async Task UnblockHardware(IEnumerable<string?> hwids)
        {
            foreach (var hwid in hwids)
            {
                await _storage.RemoveLockedHwid(new Hardware(hwid));
            }
        }

        public Task<bool> CheckContainsHardware(IHardware hardware)
        {
            return _storage.ContainsLockedHwid(hardware);
        }

        private string UsernameToUuid(string username)
        {
            return GetOfflinePlayerUuid(username);
        }

        private string GetOfflinePlayerUuid(string username)
        {
            //new GameProfile(UUID.nameUUIDFromBytes(("OfflinePlayer:" + name).getBytes(Charsets.UTF_8)), name));
            var rawresult = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
            //set the version to 3 -> Name based md5 hash
            rawresult[6] = (byte)((rawresult[6] & 0x0f) | 0x30);
            //IETF variant
            rawresult[8] = (byte)((rawresult[8] & 0x3f) | 0x80);
            //convert to string and remove any - if any
            var finalresult = BitConverter.ToString(rawresult).Replace("-", "");
            //formatting
            finalresult = finalresult.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
            return finalresult;
        }
    }
}
