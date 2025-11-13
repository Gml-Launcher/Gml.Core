using System.Threading.Tasks;
using Gml.Domains.User;

namespace Gml.Domains.Repositories;

public interface IUserRepository
{
    Task<DbUser?> CheckExistUser(string login, string email);
    Task<DbUser?> GetUser(string loginOrEmail, string password);
    Task<DbUser> CreateUser(string email, string login, string password);
    Task<DbUser?> GetUser(string loginOrEmail);
}
