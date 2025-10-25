namespace Gml.Dto.Auth;

public class AuthTokensDto
{
    public string AccessToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
}
