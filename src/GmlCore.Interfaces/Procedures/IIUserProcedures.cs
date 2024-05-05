using System.Threading.Tasks;
using GmlCore.Interfaces.User;

namespace GmlCore.Interfaces.Procedures
{
    public interface IUserProcedures
    {
        Task<IUser> GetAuthData(string login, string password, string device);
        Task<IUser?> GetUserByUuid(string uuid);
    }
}
