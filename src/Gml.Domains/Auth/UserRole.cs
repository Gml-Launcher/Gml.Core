namespace Gml.Domains.Auth;

public class UserRole
{
    public int UserId { get; set; }
    public User.DbUser DbUser { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
