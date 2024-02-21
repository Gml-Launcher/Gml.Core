using System.Collections.Generic;

namespace Gml.Core.User
{
    public class AuthUser : User
    {
        public List<AuthUserHistory> AuthHistory { get; set; } = new();
    }
}
