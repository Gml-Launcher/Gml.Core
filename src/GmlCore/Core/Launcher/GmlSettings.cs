using System.IO;
using Gml.Core.Helpers.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Launcher
{
    public class GmlSettings : IGmlSettings
    {
        public string Name { get; }
        public string FolderName { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }

        private readonly ISystemProcedures _systemProcedures = new SystemProcedures();

        public GmlSettings(string name, string? baseDirectory = null)
        {
            Name = name;
            FolderName = _systemProcedures.CleanFolderName(name);
            BaseDirectory = baseDirectory ?? _systemProcedures.DefaultInstallation;
            InstallationDirectory = Path.Combine(BaseDirectory, FolderName);
        }
        
        
    }
}