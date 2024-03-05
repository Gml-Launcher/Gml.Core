using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.FabricMC;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Version;
using Gml.Core.Services.Storage;
using Gml.Models;
using Gml.Models.CmlLib;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.User;

namespace Gml.Core.GameDownloader
{
    public class GameDownloaderProcedures : IGameDownloaderProcedures
    {
        private readonly CMLauncher _launcher;

        private readonly ILauncherInfo _launcherInfo;
        private readonly CustomMinecraftPath _minecraftPath;
        private readonly IStorageService _storage;


        public GameDownloaderProcedures(ILauncherInfo launcherInfo, IStorageService storage, IGameProfile profile)
        {
            _launcherInfo = launcherInfo;
            _storage = storage;

            if (profile == GameProfile.Empty)
                return;

            var clientDirectory = Path.Combine(launcherInfo.InstallationDirectory, "clients", profile.Name);

            profile.ClientPath = clientDirectory;
            _minecraftPath = new CustomMinecraftPath(clientDirectory);
            _launcher = new CMLauncher(_minecraftPath);

            // ToDo: Заменить на свой класс
            _launcher.FileChanged += fileInfo => FileChanged?.Invoke(fileInfo.FileName ?? string.Empty);
            _launcher.ProgressChanged += (sender, args) => ProgressChanged?.Invoke(sender, args);

            ServicePointManager.DefaultConnectionLimit = 255;
        }

        public MVersion? InstallationVersion { get; private set; }

        public event IGameDownloaderProcedures.FileDownloadChanged? FileChanged;
        public event IGameDownloaderProcedures.ProgressDownloadChanged? ProgressChanged;

        public async Task<string> DownloadGame(
            string version, GameLoader loader,
            Web.Api.Domains.System.OsType osType,
            string osArch)
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentNullException(nameof(version));

            switch (loader)
            {
                case GameLoader.Vanilla:
                    await _launcher.CheckAndDownloadAsync(InstallationVersion);
                    return InstallationVersion.Id;
                case GameLoader.Forge:

                    var forge = new MForge(_launcher);
                    // var originalVersion = version.Split('-').First() ?? string.Empty;

                    return await forge.Install(version, true, (OsType)(int)osType, osArch).ConfigureAwait(false);


                case GameLoader.Fabric:

                    var fabricVersionLoader = new FabricVersionLoader();
                    var fabricVersions = await fabricVersionLoader.GetVersionMetadatasAsync();

                    var fabric = fabricVersions.GetVersionMetadata(version);

                    await fabric.SaveAsync(_minecraftPath);

                    await _launcher.CreateProcessAsync(version, new MLaunchOption());

                    return version;

                default:
                    throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
            }
        }


        public async Task<bool> IsFullLoaded(IGameProfile baseProfile, IStartupOptions? startupOptions = null)
        {
            if (await baseProfile.CheckClientExists() == false)
                return false;

            if (startupOptions != null && await baseProfile.CheckOsTypeLoaded(startupOptions) == false)
                return false;

            return true;
        }

        public Task<Process> CreateProfileProcess(IGameProfile baseProfile, IStartupOptions startupOptions, IUser user,
            bool forceDownload, string[]? jvmArguments)
        {
            var session = new MSession(user.Name, user.AccessToken, user.Uuid);

            return _launcher.CreateProcessAsync(baseProfile.LaunchVersion, new MLaunchOption
            {
                MinimumRamMb = startupOptions.MinimumRamMb,
                MaximumRamMb = startupOptions.MaximumRamMb,
                FullScreen = startupOptions.FullScreen,
                ScreenHeight = startupOptions.ScreenHeight,
                ScreenWidth = startupOptions.ScreenWidth,
                ServerIp = startupOptions.ServerIp,
                ServerPort = startupOptions.ServerPort,
                Session = session,
                JVMArguments = jvmArguments,
                OsType = (OsType)Enum.Parse(typeof(OsType), startupOptions.OsType.ToString())
            }, forceDownload);
        }

        public Task<bool> CheckClientExists(IGameProfile baseProfile)
        {
            var jarFilePath = Path.Combine(
                baseProfile.ClientPath,
                "client",
                baseProfile.GameVersion,
                $"{baseProfile.GameVersion}.jar");

            var fileInfo = new FileInfo(jarFilePath);

            return Task.FromResult(fileInfo.Exists);
        }

        public Task<bool> CheckOsTypeLoaded(IGameProfile baseProfile, IStartupOptions startupOptions)
        {
            var jarFilePath = Path.Combine(
                baseProfile.ClientPath,
                "client",
                baseProfile.GameVersion,
                "natives");


            return Task.FromResult(false);
        }


        internal async Task<string> ValidateMinecraftVersion(string version, GameLoader loader)
        {
            if (InstallationVersion != null)
                return InstallationVersion.Id;

            switch (loader)
            {
                case GameLoader.Vanilla:
                    // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 |
                                                           SecurityProtocolType.Tls;

                    InstallationVersion ??= await _launcher.GetVersionAsync(version);
                    break;

                case GameLoader.Forge:

                    var clientHandler = new HttpClientHandler();
                    clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

                    var versionMapper = new ForgeInstallerVersionMapper();
                    var versionLoader = new ForgeVersionLoader(new HttpClient(clientHandler));

                    var minecraftVersion = version.Split('-').First();

                    var versions = (await versionLoader.GetForgeVersions(minecraftVersion)).ToList();

                    var bestVersion =
                        versions.FirstOrDefault(v => v.IsRecommendedVersion) ??
                        versions.FirstOrDefault(v => v.IsLatestVersion) ??
                        versions.FirstOrDefault() ??
                        throw new InvalidOperationException("Cannot find any version");

                    var mappedVersion = versionMapper.CreateInstaller(bestVersion);
                    InstallationVersion = new MVersion(mappedVersion.VersionName);

                    break;


                case GameLoader.Fabric:
                    var fabricLoader = new FabricVersionLoader();

                    var fabricMinecraftVersion = version.Split('-').Last();

                    var fabricVersions = await fabricLoader.GetVersionMetadatasAsync();

                    var fabricBestVersion = fabricVersions.FirstOrDefault(v => v.Name.EndsWith($"-{fabricMinecraftVersion}"))
                                            ?? throw new InvalidOperationException("Cannot find any version");

                    InstallationVersion = new MVersion(fabricBestVersion.Name);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
            }

            return InstallationVersion.Id;
        }
    }
}
