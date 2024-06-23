using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.LiteLoader;
using CmlLib.Core.VersionMetadata;
using CommunityToolkit.Diagnostics;
using Gml.Common;
using Gml.Core.Constants;
using Gml.Core.Exceptions;
using Gml.Core.Helpers.Game;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Models;
using Gml.Models.System;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace Gml.Core.Helpers.Profiles
{
    public partial class ProfileProcedures : IProfileProcedures
    {
        public delegate void ProgressPackChanged(ProgressChangedEventArgs e);

        private ISubject<double> _packChanged = new Subject<double>();
        public IObservable<double> PackChanged => _packChanged;

        private const string AuthLibUrl =
            "https://github.com/yushijinhun/authlib-injector/releases/download/v1.2.5/authlib-injector-1.2.5.jar";


        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storageService;
        private readonly GmlManager _gmlManager;
        private List<IGameProfile> _gameProfiles = new();
        private ConcurrentDictionary<string, string> _fileHashCache = new();
        private VersionMetadataCollection? _vanillaVersions;
        private ConcurrentDictionary<string, IEnumerable<ForgeVersion>>? _forgeVersions = new();
        private IReadOnlyCollection<string>? _fabricVersions;
        private IReadOnlyList<LiteLoaderVersion>? _liteLoaderVersions;

        public ProfileProcedures(
            ILauncherInfo launcherInfo,
            IStorageService storageService,
            GmlManager gmlManager)
        {
            _launcherInfo = launcherInfo;
            _storageService = storageService;
            _gmlManager = gmlManager;
        }

        public async Task AddProfile(IGameProfile? profile)
        {
            await RestoreProfiles();

            if (profile is null)
                throw new ArgumentNullException(nameof(profile));

            if (_gameProfiles.Any(c => c.Name == profile.Name))
                throw new ProfileExistException(profile);

            profile.ProfileProcedures = this;
            profile.ServerProcedures = this;
            profile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, profile);

            _gameProfiles.Add(profile);

            await SaveProfiles();
        }

        public async Task<IGameProfile?> AddProfile(string name,
            string version,
            string loaderVersion,
            GameLoader loader,
            string icon,
            string description)
        {
            if (string.IsNullOrEmpty(name))
                ThrowHelper.ThrowArgumentNullException<string>(name);

            if (string.IsNullOrEmpty(version))
                ThrowHelper.ThrowArgumentNullException<string>(version);

            var profile = new GameProfile(name, version, loader)
            {
                ProfileProcedures = this,
                ServerProcedures = this,
                IsEnabled = true,
                CreateDate = DateTimeOffset.Now,
                LaunchVersion = loaderVersion,
                Description = description,
                IconBase64 = icon
            };

            await AddProfile(profile);

            return profile;
        }

        public async Task<bool> CanAddProfile(string name, string version, string loaderVersion, GameLoader dtoGameLoader)
        {
            if (_gameProfiles.Any(c => c.Name == name))
                return false;

            var versions = await GetAllowVersions(dtoGameLoader, version);

            switch (dtoGameLoader)
            {
                case GameLoader.Undefined:
                    break;
                case GameLoader.Vanilla:
                    return versions.Any(c => c.Equals(version));
                case GameLoader.Forge:
                    return versions.Any(c => c.Equals(loaderVersion));
                case GameLoader.Fabric:
                    return versions.Any(c => c.Equals(version));
                case GameLoader.LiteLoader:
                    return versions.Any(c => c.Equals(loaderVersion));
                default:
                    throw new ArgumentOutOfRangeException(nameof(dtoGameLoader), dtoGameLoader, null);
            }




            return true;
        }

        public Task RemoveProfile(IGameProfile profile)
        {
            return RemoveProfile(profile, false);
        }

        public async Task RemoveProfile(IGameProfile profile, bool removeProfileFiles)
        {
            await RestoreProfiles();

            var localProfile = _gameProfiles.FirstOrDefault(c => c.Name == profile.Name);

            if (localProfile == null)
                return;

            if (removeProfileFiles)
            {
                if (Directory.Exists(localProfile.ClientPath))
                    Directory.Delete(localProfile.ClientPath, true);
            }

            _gameProfiles.Remove(localProfile);

            await SaveProfiles();
        }

        public async Task RestoreProfiles()
        {
            var profiles = await _storageService.GetAsync<List<GameProfile>>(StorageConstants.GameProfiles);

            if (profiles != null && !_gameProfiles.Any())
            {
                profiles = profiles.Where(c => c != null).ToList();

                Parallel.ForEach(profiles, RestoreProfile);

                _gameProfiles = [..profiles];
            }
        }

        private async void RestoreProfile(GameProfile profile)
        {
            await UpdateProfilesService(profile);
        }

        public Task RemoveProfile(int profileId)
        {
            var profile = _gameProfiles[profileId];

            return RemoveProfile(profile, false);
        }

        public async Task ClearProfiles()
        {
            _gameProfiles = new List<IGameProfile>();

            await SaveProfiles();
        }

        public async Task<bool> ValidateProfileAsync(IGameProfile baseProfile)
        {
            // ToDo: Сделать проверку верности профиля через схему

            return true;
        }

        public bool ValidateProfile()
        {
            throw new NotImplementedException();
        }

        public async Task SaveProfiles()
        {
            await _storageService.SetAsync(StorageConstants.GameProfiles, _gameProfiles);
        }

        public async Task DownloadProfileAsync(IGameProfile baseProfile)
        {
            if (baseProfile is GameProfile gameProfile && await gameProfile.ValidateProfile())
                gameProfile.LaunchVersion =
                    await gameProfile.GameLoader.DownloadGame(baseProfile.GameVersion, baseProfile.LaunchVersion, gameProfile.Loader);
        }

        public async Task<IGameProfile?> GetProfile(string profileName)
        {
            await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            return profile;
        }

        public async Task<IEnumerable<IGameProfile>> GetProfiles()
        {
            await RestoreProfiles();

            return _gameProfiles.AsEnumerable();
        }


        public async Task<IEnumerable<IFileInfo>> GetProfileFiles(IGameProfile baseProfile)
        {
            var profileDirectoryInfo = new DirectoryInfo(baseProfile.ClientPath);

            var localFiles = profileDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

            var localFilesInfo = await Task.WhenAll(localFiles.AsParallel().Select(c =>
            {
                string hash;

                if (_fileHashCache.TryGetValue(c.FullName, out var value))
                {
                    hash = value;
                }
                else
                {
                    using (var algorithm = new SHA256Managed())
                    {
                        hash = SystemHelper.CalculateFileHash(c.FullName, algorithm);
                        _fileHashCache[c.FullName] = hash;
                    }
                }

                return Task.FromResult(new LocalFileInfo
                {
                    Name = c.Name,
                    Directory = c.FullName.Replace(_launcherInfo.InstallationDirectory, string.Empty),
                    Size = c.Length,
                    Hash = hash
                });
            }));

            return localFilesInfo;
        }

        public async Task<IGameProfileInfo?> GetProfileInfo(
            string profileName,
            IStartupOptions startupOptions,
            IUser user)
        {
            if (!_gameProfiles.Any())
                await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            if (profile == null)
                return null;

            var profileDirectory = Path.Combine(profile.ClientPath, "platforms", startupOptions.OsName,
                startupOptions.OsArch);
            var relativePath = Path.Combine("clients", profileName);

            var jvmArgs = new List<string>();

            if (profile.JvmArguments is not null)
            {
                jvmArgs.Add(profile.JvmArguments);
            }

            var files =
                await profile.GetProfileFiles(startupOptions.OsName, startupOptions.OsArch);

            if (files!.Any(c => c.Name == Path.GetFileName(AuthLibUrl)))
            {
                var authLibRelativePath = Path.Combine(profile.ClientPath, "libraries", "custom", Path.GetFileName(AuthLibUrl));
                jvmArgs.Add($"-javaagent:{authLibRelativePath}={{authEndpoint}}");
            }

            Process? process = default;

            try
            {
                process = await profile.GameLoader.CreateProcess(startupOptions, user, false,
                    jvmArgs.ToArray());
            }
            catch (Exception exception)
            {
                // ToDo: Sentry
            }

            var arguments =
                process?.StartInfo.Arguments
                    .Replace(profileDirectory, Path.Combine("{localPath}", relativePath))
                    .Replace(_launcherInfo.InstallationDirectory, "{localPath}")
                ?? string.Empty;

            var javaPath = process?.StartInfo.FileName.Replace(_launcherInfo.InstallationDirectory, "{localPath}") ??
                           "java";

            if (process != null)
            {
                return new GameProfileInfo
                {
                    ProfileName = profile.Name,
                    Description = profile.Description,
                    IconBase64 = profile.IconBase64,
                    JvmArguments = profile.JvmArguments,
                    HasUpdate = profile.State != ProfileState.Loading,
                    Arguments = arguments,
                    JavaPath = javaPath,
                    ClientVersion = profile.GameVersion,
                    MinecraftVersion = profile.LaunchVersion?.Split('-').First(),
                    Files = files!.OfType<LocalFileInfo>(),
                    WhiteListFiles = profile.FileWhiteList?.OfType<LocalFileInfo>().ToList() ??
                                     new List<LocalFileInfo>()
                };
            }

            return new GameProfileInfo
            {
                ProfileName = profile.Name,
                Arguments = string.Empty,
                JavaPath = string.Empty,
                IconBase64 = profile.IconBase64,
                Description = profile.Description,
                ClientVersion = profile.GameVersion,
                HasUpdate = profile.State != ProfileState.Loading,
                MinecraftVersion = profile.LaunchVersion?.Split('-')?.First()
            };
        }

        public async Task<IGameProfileInfo?> RestoreProfileInfo(
            string profileName)
        {
            await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            if (profile == null)
                return null;

            try
            {
                await profile.DownloadAsync();
                var authLibArguments = await profile.InstallAuthLib();
                await profile.CreateModsFolder();
                var process =
                    await profile.GameLoader.CreateProcess(StartupOptions.Empty, Core.User.User.Empty, true, authLibArguments);

                var files = (await GetProfileFiles(profile)).ToList();
                var files2 = GetWhiteListFilesProfileFiles(files);

                await SaveProfiles();

                return new GameProfileInfo
                {
                    ProfileName = profile.Name,
                    Arguments = process.StartInfo.Arguments.Replace(profile.ClientPath, "{localPath}"),
                    ClientVersion = profile.GameVersion,
                    HasUpdate = profile.State != ProfileState.Loading,
                    MinecraftVersion = profile.LaunchVersion.Split('-').First(),
                    Files = files.OfType<LocalFileInfo>(),
                    WhiteListFiles = files2.OfType<LocalFileInfo>()
                };
            }
            catch (Exception exception)
            {
                throw new Exception($"Не удалось восстановить игровой профиль. {exception}");
            }
            finally
            {
                profile.State = ProfileState.Ready;
            }
        }

        public async Task PackProfile(IGameProfile profile)
        {
            var fileInfos = await profile.GetAllProfileFiles();
            var totalFiles = fileInfos.Length;
            var processed = 0;

            foreach (var file in fileInfos)
            {
                var percentage = processed * 100 / totalFiles;
                try
                {
                    var filePath = NormalizePath(_launcherInfo.InstallationDirectory, file.Directory);

                    switch (_launcherInfo.StorageSettings.StorageType)
                    {
                        case StorageType.LocalStorage:
                            file.FullPath = filePath;
                            if (await _storageService.GetAsync<LocalFileInfo>(file.Hash) is not {} localFile || !File.Exists(localFile.FullPath))
                            {
                                await _storageService.SetAsync(file.Hash, file);
                            }

                            _packChanged.OnNext(percentage);

                            break;
                        case StorageType.S3:
                            var tags = new Dictionary<string, string>
                            {
                                { "hash", file.Hash },
                                { "file-name", file.Name }
                            };
                            if (await _gmlManager.Files.CheckFileExists("profiles", file.Hash) == false)
                            {
                                await _gmlManager.Files.LoadFile(File.OpenRead(filePath), "profiles", file.Hash, tags);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
                finally
                {
                    _packChanged.OnNext(percentage);
                }

                processed++;
            }
        }

        private string NormalizePath(string directory, string fileDirectory)
        {
            directory = directory
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            // .TrimStart(Path.DirectorySeparatorChar);

            fileDirectory = fileDirectory
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(directory, fileDirectory);
        }

        public async Task AddFileToWhiteList(IGameProfile profile, IFileInfo file)
        {
            profile.FileWhiteList ??= new List<IFileInfo>();

            if (!profile.FileWhiteList.Any(c => c.Hash == file.Hash))
            {
                profile.FileWhiteList.Add(file);
                await SaveProfiles();
            }
        }

        public async Task RemoveFileFromWhiteList(IGameProfile profile, IFileInfo file)
        {
            profile.FileWhiteList ??= new List<IFileInfo>();

            if (profile.FileWhiteList.FirstOrDefault(c => c.Hash == file.Hash) is { } fileInfo)
            {
                profile.FileWhiteList.Remove(fileInfo);
                await SaveProfiles();
            }
        }

        public async Task UpdateProfile(IGameProfile profile,
            string newProfileName,
            Stream? icon,
            Stream? backgroundImage,
            string updateDtoDescription,
            bool isEnabled,
            string jvmArguments)
        {
            var directory =
                new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "clients", profile.Name));
            var newDirectory =
                new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "clients", newProfileName));

            var needRenameFolder = profile.Name != newProfileName;

            if (newDirectory.Exists && profile.Name != newProfileName)
                return;

            var iconBase64 = icon is null
                ? profile.IconBase64
                : await ConvertStreamToBase64Async(icon);

            var backgroundKey = backgroundImage is null
                ? profile.BackgroundImageKey
                : await _gmlManager.Files.LoadFile(backgroundImage, "profile-backgrounds");

            await UpdateProfile(profile, newProfileName, iconBase64, backgroundKey, updateDtoDescription,
                needRenameFolder, directory, newDirectory, isEnabled, jvmArguments);
        }

        private async Task<string> ConvertStreamToBase64Async(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                return Convert.ToBase64String(fileBytes);
            }
        }

        private async Task UpdateProfile(IGameProfile profile, string newProfileName, string newIcon,
            string backgroundImageKey,
            string newDescription, bool needRenameFolder, DirectoryInfo directory, DirectoryInfo newDirectory,
            bool isEnabled, string jvmArguments)
        {
            profile.Name = newProfileName;
            profile.IconBase64 = newIcon;
            profile.BackgroundImageKey = backgroundImageKey;
            profile.Description = newDescription;
            profile.IsEnabled = isEnabled;
            profile.JvmArguments = jvmArguments;

            profile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, profile);

            await SaveProfiles();
            await RestoreProfiles();

            if (needRenameFolder) RenameFolder(directory.FullName, newDirectory.FullName);
        }

        public async Task<string[]> InstallAuthLib(IGameProfile profile)
        {
            var directory =
                new DirectoryInfo(profile.ClientPath);

            var authLibPath = new DirectoryInfo(Path.Combine(directory.FullName, "libraries", "custom"));
            var downloadingUrl = new FileInfo(AuthLibUrl);
            var downloadingFileInfo = new FileInfo(Path.Combine(authLibPath.FullName, downloadingUrl.Name));
            var authlibFileName = Path.GetFileName(downloadingUrl.FullName);

            if (!authLibPath.Exists)
                authLibPath.Create();

            if (downloadingFileInfo.Exists && downloadingFileInfo.Length > 0)
                return [$"-javaagent:{{localPath}}\\libraries\\custom\\{authlibFileName}={{authEndpoint}}"];

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(AuthLibUrl);

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (Stream fileStream = new FileStream(downloadingFileInfo.FullName, FileMode.Create,
                           FileAccess.Write, FileShare.None, 8192, true))
                {
                    await contentStream.CopyToAsync(fileStream);
                }
            }

            downloadingFileInfo.Refresh();

            return [$"-javaagent:{{localPath}}\\libraries\\custom\\{authlibFileName}={{authEndpoint}}"];
        }

        public async Task<IGameProfileInfo?> GetCacheProfile(IGameProfile baseProfile)
        {
            return await _storageService.GetAsync<GameProfileInfo>($"CachedProfile-{baseProfile.Name}");
        }

        public Task SetCacheProfile(IGameProfileInfo profile)
        {
            return _storageService.SetAsync($"CachedProfile-{profile.ProfileName}", (GameProfileInfo)profile);
        }

        public Task CreateModsFolder(IGameProfile profile)
        {
            var modsDirectory = Path.Combine(profile.ClientPath, "mods");

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<IFileInfo>> GetProfileFiles(
            IGameProfile profile,
            string osName,
            string osArchitecture)
        {
            return profile.GameLoader.GetLauncherFiles(osName, osArchitecture);
        }

        public Task<IFileInfo[]> GetAllProfileFiles(IGameProfile baseProfile)
        {
            return baseProfile.GameLoader.GetAllFiles();
        }

        public async Task<IEnumerable<string>> GetAllowVersions(GameLoader gameLoader, string? minecraftVersion)
        {
            try
            {
                var anyLauncher = new MinecraftLauncher();

                switch (gameLoader)
                {
                    case GameLoader.Undefined:
                        break;
                    case GameLoader.Vanilla:

                        _vanillaVersions ??= await anyLauncher.GetAllVersionsAsync();
                        return _vanillaVersions.Where(c => c.Type == "release").Select(c => c.Name);

                    case GameLoader.Forge:

                        var forge = new ForgeInstaller(anyLauncher);
                        var versionMapper = new ForgeInstallerVersionMapper();

                        if (!_forgeVersions.Any(c => c.Key == minecraftVersion))
                        {
                            _forgeVersions[minecraftVersion] = await forge.GetForgeVersions(minecraftVersion);
                        }

                        return _forgeVersions[minecraftVersion]
                            .OrderByDescending(c => c.IsRecommendedVersion)
                            .ThenByDescending(c => c.Time)
                            .Select(c => versionMapper.CreateInstaller(c).ForgeVersion.ForgeVersionName);

                    case GameLoader.Fabric:

                        var fabricLoader = new FabricInstaller(new HttpClient());
                        _fabricVersions ??= await fabricLoader.GetSupportedVersionNames();

                        return _fabricVersions;

                    case GameLoader.LiteLoader:
                        var liteLoaderVersionLoader = new LiteLoaderInstaller(new HttpClient());

                        _liteLoaderVersions ??= await liteLoaderVersionLoader.GetAllLiteLoaders();

                        return _liteLoaderVersions
                            .Select(c => c)
                            .Where(c => c.BaseVersion == minecraftVersion)
                            .Select(c => c.Version)!;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(gameLoader), gameLoader, null);
                }

                return [];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw new VersionNotLoadedException("Не удалось получить список версий для данного загрузчика, причина: " + e.Message);
            }
        }

        private string ValidatePath(string path, OsType osType)
        {
            return osType == OsType.Windows
                ? path.Replace("/", "\\")
                : path.Replace("\\", "/");
        }


        private async Task UpdateProfilesService(GameProfile gameProfile)
        {
            foreach (var server in gameProfile.Servers)
            {
                server.ServerProcedures = _gmlManager.Servers;
                gameProfile.ServerAdded.OnNext(server);
            }

            gameProfile.State = ProfileState.Ready;
            gameProfile.ProfileProcedures = this;
            gameProfile.ServerProcedures = this;
            gameProfile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, gameProfile);
            // gameProfile.LaunchVersion =
            //     await gameLoader.ValidateMinecraftVersion(gameProfile.GameVersion, gameProfile.Loader);
            // gameProfile.GameVersion = gameLoader.InstallationVersion!.Id;
        }

        public IEnumerable<IFileInfo> GetWhiteListFilesProfileFiles(IEnumerable<IFileInfo> files)
        {
            return files.Where(c => c.Directory.EndsWith("options.txt"));
        }

        /// <summary>
        ///     Renames a folder name
        /// </summary>
        /// <param name="directory">The full directory of the folder</param>
        /// <param name="newFolderName">New name of the folder</param>
        /// <returns>Returns true if rename is successfull</returns>
        public static bool RenameFolder(string directory, string newFolderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory) ||
                    string.IsNullOrWhiteSpace(newFolderName))
                    return false;


                var oldDirectory = new DirectoryInfo(directory);

                if (!oldDirectory.Exists) return false;

                if (string.Equals(oldDirectory.Name, newFolderName, StringComparison.OrdinalIgnoreCase))
                    //new folder name is the same with the old one.
                    return false;

                string newDirectory;

                if (oldDirectory.Parent == null)
                    //root directory
                    newDirectory = Path.Combine(directory, newFolderName);
                else
                    newDirectory = Path.Combine(oldDirectory.Parent.FullName, newFolderName);

                if (Directory.Exists(newDirectory)) Directory.Delete(newDirectory, true);

                oldDirectory.MoveTo(newDirectory);

                return true;
            }
            catch
            {
                //ignored
                return false;
            }
        }
    }
}
