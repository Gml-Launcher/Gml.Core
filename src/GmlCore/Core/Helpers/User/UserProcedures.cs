using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using Gml.Core.User;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.User;
using NotImplementedException = System.NotImplementedException;

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
            authUser.Uuid ??= UsernameToUuid(login);
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
            return ConstructOfflinePlayerUuid(username).ToString();
        }

        public static Guid ConstructOfflinePlayerUuid(string username)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
                // Set the version to 3 -> Name based MD5 hash
                hash[6] = (byte)((hash[6] & 0x0F) | (3 << 4));
                // IETF variant
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
                return new Guid(hash);
            }
        }
    }
}
