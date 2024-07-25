using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Modrinth.Api.Core.Endpoints;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Core.Repository;
using Modrinth.Api.Core.System;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Projects;
using Version = Modrinth.Api.Models.Dto.Version;

namespace Modrinth.Api.Core.Projects
{
    public class Projects : IProjectRepository
    {
        private readonly ModrinthApi _api;
        private readonly HttpClient _httpClient;

        public Projects(ModrinthApi api, HttpClient httpClient)
        {
            _api = api;
            _httpClient = httpClient;
        }

        public async Task<SearchProjectResultDto> FindAsync<Project>(ProjectFilter filter, CancellationToken token)
        {
            return await FindProjectByFilter(filter, token) ;
        }

        public Task<T> FindAsync<T>(string identifier, CancellationToken token)
        {
            return GetProjectById<T>(identifier, token);
        }

        public Task<IEnumerable<Version>> GetVersionsAsync(string identifier, CancellationToken token)
        {
            return GetProjectVersions(identifier, token);
        }

        internal async Task<SearchProjectResultDto> FindProjectByFilter(ProjectFilter filter, CancellationToken token)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            var endPoint = string.Concat(ModrinthEndpoints.SearchProjects, filter.ToQueryString());

            var response = await _httpClient.GetAsync(endPoint, token);

            RequestHelper.UpdateApiRequestInfo(_api, response);

            if (!response.IsSuccessStatusCode)
            {
                return SearchProjectResultDto.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();

            var searchProjectResult =
                JsonSerializer.Deserialize<SearchProjectResultDto>(content) ?? SearchProjectResultDto.Empty;

            searchProjectResult.Api = _api;

            return searchProjectResult;
        }


        internal async Task<T> GetProjectById<T>(string identifier, CancellationToken cancellationToken)
        {
            var endPoint = string.Concat(ModrinthEndpoints.Project, identifier);

            var response = await _httpClient.GetAsync(endPoint, cancellationToken);

            RequestHelper.UpdateApiRequestInfo(_api, response);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(content) ?? default;
        }


        internal async Task<IEnumerable<Version>> GetProjectVersions(string identifier,
            CancellationToken cancellationToken)
        {
            var endPoint = ModrinthEndpoints.ProjectVersions.Replace("{id}", identifier);

            var response = await _httpClient.GetAsync(endPoint, cancellationToken);

            RequestHelper.UpdateApiRequestInfo(_api, response);

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<Version>();
            }

            var content = await response.Content.ReadAsStringAsync();

            var versions = JsonSerializer.Deserialize<List<Version>>(content) ??
                           Enumerable.Empty<Version>().ToList();

            foreach (var version in versions)
                version.Api = _api;

            return versions;
        }
    }
}
