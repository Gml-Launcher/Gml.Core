using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Modrinth.Api.Core.Endpoints;
using Modrinth.Api.Core.System;
using Modrinth.Api.Models.Dto;

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

        private async Task<Version?> GetVersionById(string identifier, CancellationToken token)
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
    }
}
