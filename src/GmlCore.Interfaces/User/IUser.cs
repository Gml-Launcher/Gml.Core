namespace GmlCore.Interfaces.User
{
    public interface IUser
    {
        string Name { get; set; }
        string AccessToken { get; set; }
        string Uuid { get; set; }
    }
}
