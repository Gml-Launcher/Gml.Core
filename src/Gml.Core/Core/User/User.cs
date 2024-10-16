using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GmlCore.Interfaces;
using GmlCore.Interfaces.User;

namespace Gml.Core.User
{
    public class User : IUser
    {
        [JsonIgnore] internal bool IsValid => ExpiredDate != DateTime.MinValue && ExpiredDate > DateTime.Now;

        public static IUser Empty { get; set; } = new User
        {
            Name = "Default123",
            Uuid = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser",
            AccessToken = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser"
        };

        public string Name { get; set; } = null!;
        public string? TextureSkinUrl { get; set; }
        public string? TextureCloakUrl { get; set; }
        public string ServerUuid { get; set; }

        public string? TextureSkinGuid { get; set; }
        public string? TextureCloakGuid { get; set; }
        public bool IsBanned { get; set; }
        public DateTime ServerExpiredDate { get; set; }
        public string? AccessToken { get; set; }
        public string? Uuid { get; set; }
        public DateTime ExpiredDate { get; set; }
        public List<ISession> Sessions { get; set; } = [];
        [JsonIgnore]
        public IGmlManager Manager { get; set; }

        public async Task DownloadAndInstallSkinAsync(string skinUrl)
        {
            TextureSkinUrl = await Manager.Integrations.TextureProvider.SetSkin(this, skinUrl);

            if (!string.IsNullOrEmpty(TextureSkinUrl))
                TextureSkinGuid = Guid.NewGuid().ToString();
            else
                TextureSkinGuid = string.Empty;
        }


        public async Task DownloadAndInstallCloakAsync(string cloakUrl)
        {
            TextureCloakUrl = await Manager.Integrations.TextureProvider.SetCloak(this, cloakUrl);

            if (!string.IsNullOrEmpty(TextureCloakUrl))
                TextureCloakGuid = Guid.NewGuid().ToString();
            else
                TextureSkinGuid = string.Empty;
        }

        public Task SaveUserAsync()
        {
            return Manager.Users.UpdateUser(this);
        }
    }
}
