using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Modrinth.Api.Core.Endpoints;
using Modrinth.Api.Core.System;
using NotImplementedException = System.NotImplementedException;
using Version = Modrinth.Api.Models.Dto.Version;

namespace Modrinth.Api.Core.Projects
{
    public class Versions
    {
        private readonly ModrinthApi _api;
        private readonly HttpClient _httpClient;

        public Versions(ModrinthApi modrinthApi, HttpClient httpClient)
        {
            _api = modrinthApi;
            _httpClient = httpClient;
        }

        public async Task<Version?> GetVersionById(string identifier, CancellationToken token)
        {
            var endPoint = ModrinthEndpoints.Version.Replace("{id}", identifier);

            var response = await _httpClient.GetAsync(endPoint, token);

            RequestHelper.UpdateApiRequestInfo(_api, response);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Version>(content) ?? null;
        }

        public async Task<IReadOnlyCollection<Version>> GetVersionsByModId(string identifier, string loader, string gameVersion, CancellationToken token)
        {
            if (_httpClient.BaseAddress == null)
            {
                throw new InvalidOperationException("BaseAddress в HttpClient не установлен.");
            }

            var endPoint = ModrinthEndpoints.ProjectVersions.Replace("{id}", identifier);

            var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress, endPoint));

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["loaders"] = $"[\"{loader}\"]";
            query["game_versions"] = $"[\"{gameVersion}\"]";
            uriBuilder.Query = query.ToString();

            var fullUri = uriBuilder.ToString();
            var response = await _httpClient.GetAsync(fullUri, token);
            RequestHelper.UpdateApiRequestInfo(_api, response);

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<Version>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Version[]>(content) ?? Array.Empty<Version>();
        }
    }
}
