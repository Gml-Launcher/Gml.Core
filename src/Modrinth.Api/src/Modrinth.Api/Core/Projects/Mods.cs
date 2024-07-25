using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Modrinth.Api.Core.Repository;
using Modrinth.Api.Core.System;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Dto.Entities;
using Modrinth.Api.Models.Projects;

namespace Modrinth.Api.Core.Projects
{
    public class Mods : Projects, IProjectRepository
    {
        private readonly ModrinthApi _api;
        private readonly FileLoader _fileLoader;
        private readonly DirectoryInfo _directoryInfo;

        public Mods(ModrinthApi api, HttpClient httpClient, string installationDirectory) : base(api, httpClient)
        {
            _api = api;
            _fileLoader = new FileLoader();
            _directoryInfo = new DirectoryInfo(installationDirectory);
        }

        public new async Task<TProject> FindAsync<TProject>(string identifier, CancellationToken token)
        {
            var project = await base.FindAsync<TProject>(identifier, token);

            if (project is Project modProject)
                modProject.Api = _api;

            return project;
        }

        public async Task DownloadAsync(Version version, CancellationToken token)
        {
            var filesUrls = version.Files.Select(c => c.Url);

            await _fileLoader.DownloadFilesAsync(filesUrls, _directoryInfo.FullName, token);
        }

        public async Task<Version?> GetLastVersionAsync(string identifier, string loaderName, CancellationToken token)
        {
            var versions = await GetProjectVersions(identifier, token);

            return versions.OrderByDescending(c => c.DatePublished).FirstOrDefault(c => c.Loaders.Contains(loaderName));
        }
    }
}
