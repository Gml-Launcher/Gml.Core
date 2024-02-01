using System.Collections.Generic;
using System.Threading.Tasks;
using Gml.WebApi.Models.Enums.Auth;
using GmlCore.Interfaces.Auth;

namespace GmlCore.Interfaces.Integrations
{
    public interface IServicesIntegrationProcedures
    {
        Task<AuthType> GetAuthType();
        Task<IEnumerable<IAuthServiceInfo>> GetAuthServices();
        Task<IAuthServiceInfo?> GetActiveAuthService();
        Task<IAuthServiceInfo?> GetAuthService(AuthType authType);
        Task SetActiveAuthService(IAuthServiceInfo service);
    }
}
