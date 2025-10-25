using System.Collections.Generic;

namespace GmlCore.Interfaces.Auth;

public interface IAccessTokenService
{
    string GenerateAccessToken(int userId, string? role = null);
    string GenerateAccessToken(string subject, int? tokenDuration, string? role = null);

    string GenerateAccessToken(int userId, string userLogin, string userEmail, IEnumerable<string> roles,
        IEnumerable<string> permissions, int? tokenDuration = null);

    string GenerateAccessToken(string subject, IEnumerable<string> roles, IEnumerable<string> permissions,
        int? tokenDuration = null);
    string GenerateAccessToken(string subject, string userLogin, string userEmail, IEnumerable<string> roles,
        IEnumerable<string> permissions, int? tokenDuration = null);
    bool ValidateToken(string token);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
