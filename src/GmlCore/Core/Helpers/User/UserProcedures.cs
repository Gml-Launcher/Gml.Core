using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using Gml.Core.User;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.User;

namespace Gml.Core.Helpers.User
{
    public class UserProcedures : IUserProcedures
    {
        private readonly IStorageService _storage;

        public UserProcedures(IStorageService storage)
        {
            _storage = storage;
        }

        public async Task<IUser> GetAuthData(string login, string password, string device)
        {
            var authUser = await _storage.GetUserAsync<AuthUser>(login) ?? new AuthUser
            {
                Name = login
            };

            authUser.AuthHistory.Add(AuthUserHistory.Create(device));
            authUser.AuthHistory = authUser.AuthHistory.TakeLast(20).ToList();
            authUser.AccessToken = GenerateAccessToken();
            authUser.Uuid = UsernameToUuid(login);
            authUser.ExpiredDate = DateTime.Now + TimeSpan.FromDays(30);

            await _storage.SetUserAsync(login, authUser);

            return authUser;
        }

        private string GenerateAccessToken()
        {
            var timestamp = DateTime.Now.Ticks.ToString();
            var guidPart1 = Guid.NewGuid().ToString();
            var guidPart2 = Guid.NewGuid().ToString();
            var secretKey = "YourSecretKey"; // ToDo: Export to constant .env

            var textBytes = Encoding.UTF8.GetBytes(timestamp + secretKey + guidPart1 + guidPart2);
            return Convert.ToBase64String(textBytes);
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
