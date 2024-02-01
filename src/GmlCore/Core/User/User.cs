using System;
using GmlCore.Interfaces.User;

namespace Gml.Core.User
{
    public class User : IUser
    {
        public string Name { get; set; } = null!;
        public string? AccessToken { get; set; }
        public string? Uuid { get; set; }
        public DateTime ExpiredDate { get; set; }

        public bool IsValid => ExpiredDate != DateTime.MinValue && ExpiredDate > DateTime.Now;

        public static IUser Empty { get; set; } = new User
        {
            Name = "Default123",
            Uuid = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser",
            AccessToken = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser"
        };
    }
}
