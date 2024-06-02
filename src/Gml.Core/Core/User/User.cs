using System;
using System.Text.Json.Serialization;
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
        public string TextureUrl { get; set; }
        public string ServerUuid { get; set; }

        public bool IsBanned { get; set; }
        public DateTime ServerExpiredDate { get; set; }
        public string? AccessToken { get; set; }
        public string? Uuid { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}
