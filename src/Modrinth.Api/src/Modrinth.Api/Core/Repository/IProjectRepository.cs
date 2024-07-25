using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Projects;

namespace Modrinth.Api.Core.Repository
{
    public interface IProjectRepository
    {
        Task<SearchProjectResultDto> FindAsync<T>(ProjectFilter filter, CancellationToken token);
        Task<T> FindAsync<T>(string identifier, CancellationToken token);
        Task<IEnumerable<Version>> GetVersionsAsync(string identifier, CancellationToken cancellationToken);
    }
}
