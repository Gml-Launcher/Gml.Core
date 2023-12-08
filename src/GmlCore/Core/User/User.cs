using GmlCore.Interfaces.User;

namespace Gml.Core.User
{
    public class User : IUser
    {
        public string Name { get; set; }
        public string AccessToken { get; set; }
        public string Uuid { get; set; }
    }
}
