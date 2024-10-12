using System.IO;
using System.Net.Http;
using Gml.Core.Helpers.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Launcher
{
    public class GmlSettings : IGmlSettings
    {
        private readonly ISystemProcedures _systemProcedures;
        public ISystemProcedures SystemProcedures => _systemProcedures;

        public GmlSettings(string name, string securityKey, string? baseDirectory = null, HttpClient? httpClient = null)
        {
            HttpClient = httpClient ?? new HttpClient();
            _systemProcedures = new SystemProcedures(this);

            Name = name;
            SecurityKey = securityKey;
            FolderName = _systemProcedures.CleanFolderName(name);
            BaseDirectory = string.IsNullOrEmpty(baseDirectory) ? _systemProcedures.DefaultInstallation : baseDirectory;
            InstallationDirectory = Path.Combine(BaseDirectory, FolderName);

        }

        public string FolderName { get; }
        public string Name { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }
        public HttpClient HttpClient { get; }

        public IStorageSettings StorageSettings { get; set; }
        public string SecurityKey { get; set; }
        public string TextureServiceEndpoint { get; set; }
    }
}
