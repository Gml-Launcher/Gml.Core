using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.System
{
    public class SystemProcedures : ISystemProcedures
    {
        private string _installationDirectory;

        public string DefaultInstallation
        {
            get
            {
                if (string.IsNullOrEmpty(_installationDirectory))
                    _installationDirectory = GetDefaultInstallationPath();

                return _installationDirectory;
            }
        }

        public string CleanFolderName(string name)
        {
            var cleanedName =
                new string(Array.FindAll(name.ToCharArray(),
                    c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));

            cleanedName = Path.GetInvalidFileNameChars()
                .Aggregate(cleanedName,
                    (current, c) => current.Replace(c.ToString(), "_"));

            return cleanedName;
        }

        public string GetDefaultInstallationPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) 
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) 
                : Environment.CurrentDirectory;
        }
    }
}