using System;
using System.Collections.Generic;
using Gml.Core.Constants;
using Gml.Core.GameDownloader;
using Gml.Core.Helpers.Files;
using Gml.Core.Helpers.Launcher;
using Gml.Core.Helpers.Profiles;
using Gml.Core.Helpers.User;
using Gml.Core.Integrations;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Models;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml
{
    public class GmlManager : IGmlManager
    {
        public GmlManager(IGmlSettings settings)
        {
            LauncherInfo = new LauncherInfo(settings);
            Storage = new SqliteStorageService(settings);
            GameLoader = new GameDownloaderProcedures(LauncherInfo, Storage, GameProfile.Empty);
            Profiles = new ProfileProcedures(LauncherInfo, Storage, this);
            Files = new FileStorageProcedures(LauncherInfo, Storage);
            Integrations = new ServicesIntegrationProcedures(Storage);
            Users = new UserProcedures(Storage);
            Launcher = new LauncherProcedures(LauncherInfo, Storage, Files);

            Servers = (IProfileServersProcedures)Profiles;
        }

        public IGameDownloaderProcedures GameLoader { get; }
        public IStorageService Storage { get; }
        public ILauncherInfo LauncherInfo { get; }
        public IProfileProcedures Profiles { get; }
        public IProfileServersProcedures Servers { get; }
        public IFileStorageProcedures Files { get; }
        public IServicesIntegrationProcedures Integrations { get; }
        public IUserProcedures Users { get; }
        public ILauncherProcedures Launcher { get; }

        public void RestoreSettings<T>() where T : IVersionFile
        {
            try
            {
                var versionReleases = Storage.GetAsync<Dictionary<OsType, T?>>(StorageConstants.ActualVersionInfo).Result;

                if (versionReleases is null) return;

                foreach (var item in versionReleases)
                {
                    LauncherInfo.ActualLauncherVersion.Add(item.Key, item.Value);
                }

                Profiles.RestoreProfiles().Wait();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
