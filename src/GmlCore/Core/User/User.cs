using System;
using GmlCore.Interfaces.User;
using Newtonsoft.Json;

namespace Gml.Core.User
{
    public class User : IUser
    {
        [JsonIgnore] public bool IsValid => ExpiredDate != DateTime.MinValue && ExpiredDate > DateTime.Now;

        public static IUser Empty { get; set; } = new User
        {
            Name = "Default123",
            Uuid = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser",
            AccessToken = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser"
        };

        public string Name { get; set; } = null!;
        public string TextureUrl { get; set; }
        public string? AccessToken { get; set; }
        public string? Uuid { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}
