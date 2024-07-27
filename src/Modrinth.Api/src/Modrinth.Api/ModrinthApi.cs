using System;
using System.Net.Http;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Core.System;

namespace Modrinth.Api
{
    public class ModrinthApi
    {
        private readonly string _installationDirectory;
        private HttpClientFactory HttpClientFactory { get; }
        public Projects Projects { get; }
        public Mods Mods { get; }
        public Settings Settings { get; }
        public Versions Versions { get; }
        public Other Other { get; }

        public ModrinthApi(string installationDirectory, HttpClient httpClient)
        {
            _installationDirectory = installationDirectory;
            Settings = new Settings();

            httpClient.BaseAddress = new Uri("https://api.modrinth.com");

            Projects = new Projects(this, httpClient);
            Mods = new Mods(this, httpClient, installationDirectory);
            Versions = new Versions(this, httpClient);
            Other = new Other(this, httpClient);
        }

    }

}
