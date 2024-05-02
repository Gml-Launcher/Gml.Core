using Gml.Core.GameDownloader;
using Gml.Core.Helpers.Files;
using Gml.Core.Helpers.Profiles;
using Gml.Core.Helpers.User;
using Gml.Core.Integrations;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Core.StateMachine;
using Gml.Models;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml
{
    public class GmlManager : IGmlManager
    {
        public GmlManager(IGmlSettings settings)
        {
            LauncherInfo = new LauncherInfo(settings);
            Storage = new SqliteStorageService(settings);
            GameLoader = new GameDownloaderProcedures(LauncherInfo, Storage, GameProfile.Empty);
            Profiles = new ProfileProcedures(LauncherInfo, Storage);
            Files = new FileStorageProcedures(LauncherInfo, Storage);
            Integrations = new ServicesIntegrationProcedures(Storage);
            Users = new UserProcedures(Storage);
        }

        public IGameDownloaderProcedures GameLoader { get; }
        public IStorageService Storage { get; }

        internal ProfileLoaderStateMachine ProfileLoaderState { get; }
        public ILauncherInfo LauncherInfo { get; }
        public IProfileProcedures Profiles { get; }
        public IFileStorageProcedures Files { get; }
        public IServicesIntegrationProcedures Integrations { get; }
        public IUserProcedures Users { get; }
    }
}
