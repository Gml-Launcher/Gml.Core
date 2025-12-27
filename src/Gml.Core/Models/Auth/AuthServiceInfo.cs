using GmlCore.Interfaces.Auth;
using GmlCore.Interfaces.Enums;

namespace Gml.Models.Auth;

public class AuthServiceInfo : IAuthServiceInfo
{
    public AuthServiceInfo(string name, AuthType authType)
    {
        Name = name;
        AuthType = authType;
    }

    public string Name { get; set; }
    public AuthType AuthType { get; set; }
    public string Endpoint { get; set; }
}
