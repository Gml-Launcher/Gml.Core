namespace Gml.Domains.User;

public class DbUser : BaseUser
{
    public string Password { get; set; }
    public string AccessToken { get; set; }
    public string Email { get; set; }
}
