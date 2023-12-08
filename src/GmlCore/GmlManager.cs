using Gml.Core.GameDownloader;
using Gml.Core.Helpers.Files;
using Gml.Core.Helpers.Profiles;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Models;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml
{
    public class GmlManager : IGmlManager
    {
        public ILauncherInfo LauncherInfo { get; }
        public IProfileProcedures Profiles { get; }
        public IFileStorageProcedures Files { get; }
        public IGameDownloaderProcedures GameLoader { get; }
        public IStorageService Storage { get; }

        public GmlManager(IGmlSettings settings)
        {
            LauncherInfo = new LauncherInfo(settings);
            Storage = new SqliteStorageService(settings);
            GameLoader = new GameDownloaderProcedures(LauncherInfo, Storage, GameProfile.Empty);
            Profiles = new ProfileProcedures(GameLoader, LauncherInfo, Storage);
            Files = new FileStorageProcedures(LauncherInfo, Storage);
        }

    }
}
