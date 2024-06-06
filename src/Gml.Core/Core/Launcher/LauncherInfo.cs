using System.Collections.Generic;
using Gml.Models.Storage;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Launcher
{
    public class LauncherInfo : ILauncherInfo
    {
        private readonly IGmlSettings _settings;

        public string Name => _settings.Name;
        public string BaseDirectory => _settings.BaseDirectory;
        public string InstallationDirectory => _settings.InstallationDirectory;
        public IStorageSettings StorageSettings { get; set; } = new StorageSettings();
        public Dictionary<OsType, IVersionFile?> ActualLauncherVersion { get; set; } = new();

        public LauncherInfo(IGmlSettings settings)
        {
            _settings = settings;
        }

        public void UpdateSettings(
            StorageType storageType,
            string storageHost,
            string storageLogin,
            string storagePassword)
        {
            StorageSettings.StoragePassword = storagePassword;
            StorageSettings.StorageLogin = storageLogin;
            StorageSettings.StorageType = storageType;
            StorageSettings.StorageHost = storageHost;
        }
    }
}
