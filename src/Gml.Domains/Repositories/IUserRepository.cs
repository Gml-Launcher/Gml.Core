using System.Threading.Tasks;

namespace Gml.Domains.Repositories;

public interface IUserRepository
{
    Task<User.DbUser?> CheckExistUser(string login, string email);
    Task<User.DbUser?> GetUser(string loginOrEmail, string password);
    Task<User.DbUser> CreateUser(string email, string login, string password);
    Task<User.DbUser?> GetUser(string loginOrEmail);
}
