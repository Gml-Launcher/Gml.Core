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
using Gml.Core.Services.Storage;
using Gml.Core.User;
using Gml.Models.Converters;
using Gml.Models.Sessions;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
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
            string? hwid)
        {
            var authUser = await _storage.GetUserAsync<AuthUser>(login, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            }) ?? new AuthUser
            {
                Name = login
            };

            authUser.AuthHistory.Add(AuthUserHistory.Create(device, protocol, hwid, address?.ToString()));
            authUser.AccessToken = GenerateJwtToken(login);
            authUser.Uuid = customUuid ?? UsernameToUuid(login);
            authUser.ExpiredDate = DateTime.Now + TimeSpan.FromDays(30);

            await _storage.SetUserAsync(login, authUser.Uuid, authUser);

            return authUser;
        }

        public async Task<IUser?> GetUserByUuid(string uuid)
        {
            return await _storage.GetUserByUuidAsync<AuthUser>(uuid, new JsonSerializerOptions
            {
                Converters = { new SessionConverter() }
            });
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

            user.ServerUuid = serverUuid;
            user.ServerExpiredDate = DateTime.Now.AddMinutes(1);
            user.ServerJoinHistory.Add(new ServerJoinHistory(serverUuid, DateTime.Now));

            await UpdateUser(user);

            return user.AccessToken?.StartsWith(accessToken) ?? false;
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

        public Task UpdateUser(IUser user)
        {
            return _storage.SetUserAsync(user.Name, user.Uuid, (AuthUser)user);
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

        public Task<IUser?> GetUserByAccessToken(string accessToken)
        {
            throw new NotImplementedException();
        }

        private string GenerateJwtToken(string login)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecurityKey));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, DateTime.Now.Ticks.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, login),
                new Claim(JwtRegisteredClaimNames.Name, login)
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Name,
                audience: _settings.Name,
                expires: DateTime.Now.AddHours(1),
                claims: claims,
                signingCredentials: signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
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
