using System.IO;
using Gml.Core.Helpers.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Launcher
{
    public class GmlSettings : IGmlSettings
    {
        private readonly ISystemProcedures _systemProcedures = new SystemProcedures();

        public GmlSettings(string name, string securityKey, string? baseDirectory = null)
        {
            Name = name;
            FolderName = _systemProcedures.CleanFolderName(name);
            SecurityKey = securityKey;
            BaseDirectory = string.IsNullOrEmpty(baseDirectory) ? _systemProcedures.DefaultInstallation : baseDirectory;
            InstallationDirectory = Path.Combine(BaseDirectory, FolderName);
        }

        public string FolderName { get; }
        public string Name { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }
        public IStorageSettings StorageSettings { get; set; }
        public string SecurityKey { get; set; }
    }
}
