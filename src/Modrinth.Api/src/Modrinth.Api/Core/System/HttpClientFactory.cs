using System;
using System.Net.Http;

namespace Modrinth.Api.Core.System
{
    public class HttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public HttpClient HttpClient => _httpClient;

        internal HttpClientFactory(int timeout)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout),
                BaseAddress = new Uri("https://api.modrinth.com/v2/search")
            };
        }
    }
}
