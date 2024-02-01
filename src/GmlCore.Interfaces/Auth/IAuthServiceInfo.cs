using Gml.WebApi.Models.Enums.Auth;

namespace GmlCore.Interfaces.Auth
{
    public interface IAuthServiceInfo
    {
        public string Name { get; set; }
        public AuthType AuthType { get; set; }
        string Endpoint { get; set; }
    }
}
