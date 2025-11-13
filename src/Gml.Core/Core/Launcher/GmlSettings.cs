using System.IO;
using System.Net.Http;
using Gml.Core.Helpers.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Launcher;

public class GmlSettings : IGmlSettings
{
    public GmlSettings(string name, string securityKey, string? baseDirectory = null, HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();
        SystemProcedures = new SystemProcedures(this);

        Name = name;
        SecurityKey = securityKey;
        FolderName = SystemProcedures.CleanFolderName(name);
        BaseDirectory = string.IsNullOrEmpty(baseDirectory) ? SystemProcedures.DefaultInstallation : baseDirectory;
        InstallationDirectory = Path.Combine(BaseDirectory, FolderName);
    }

    public string FolderName { get; }
    public ISystemProcedures SystemProcedures { get; }

    public string Name { get; }
    public string BaseDirectory { get; }
    public string InstallationDirectory { get; }
    public HttpClient HttpClient { get; }

    public IStorageSettings? StorageSettings { get; set; }
    public string SecurityKey { get; set; }
    public string TextureServiceEndpoint { get; set; }
}
