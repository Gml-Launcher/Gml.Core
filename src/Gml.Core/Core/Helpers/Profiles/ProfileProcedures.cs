using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.VersionMetadata;
using Gml.Common;
using Gml.Core.Constants;
using Gml.Core.Exceptions;
using Gml.Core.Helpers.Game;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Models;
using Gml.Models.Mods;
using Gml.Models.System;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Bootstrap;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace Gml.Core.Helpers.Profiles
{
    public partial class ProfileProcedures : IProfileProcedures
    {
        public delegate void ProgressPackChanged(ProgressChangedEventArgs e);

        private ISubject<double> _packChanged = new Subject<double>();
        private ISubject<int> _profilesChanged = new Subject<int>();
        public IObservable<double> PackChanged => _packChanged;
        public IObservable<int> ProfilesChanged => _profilesChanged;

        private const string AuthLibUrl =
            "https://github.com/Gml-Launcher/Gml.Authlib.Injector/releases/download/authlib-injector-1.2.5-alpha-1/authlib-injector-1.2.5-alpha-1.jar";

        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storageService;
        private readonly GmlManager _gmlManager;
        private readonly INotificationProcedures _notifications;
        private readonly IBugTrackerProcedures _bugTracker;
        private List<IGameProfile> _gameProfiles = new();
        private ConcurrentDictionary<string, string> _fileHashCache = new();
        private VersionMetadataCollection? _vanillaVersions;
        private ConcurrentDictionary<string, IEnumerable<string>> _fabricVersions = new();
        private ConcurrentDictionary<string, IEnumerable<string>> _quiltVersions = new();
        private ConcurrentDictionary<string, IEnumerable<ForgeVersion>>? _forgeVersions = new();
        private ConcurrentDictionary<string, IEnumerable<NeoForgeVersion>>? _neoForgeVersions = new();
        private IReadOnlyList<LiteLoaderVersion>? _liteLoaderVersions;

        public ProfileProcedures(ILauncherInfo launcherInfo,
            IStorageService storageService,
            INotificationProcedures notifications,
            IBugTrackerProcedures bugTracker,
            GmlManager gmlManager)
        {
            _launcherInfo = launcherInfo;
            _storageService = storageService;
            _gmlManager = gmlManager;
            _notifications = notifications;
            _bugTracker = bugTracker;
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
            profile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, profile, _notifications, _gmlManager.BugTracker);

            _gameProfiles.Add(profile);

            await SaveProfiles();
        }

        public async Task<IGameProfile?> AddProfile(string name,
            string displayName,
            string version,
            string loaderVersion,
            GameLoader loader,
            string icon,
            string description)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrEmpty(version))
                throw new ArgumentNullException(nameof(name));

            var profile = new GameProfile(name, displayName, version, loader)
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

            await AddFileToWhiteList(profile, [
                new LocalFileInfo(Path.Combine("clients", profile.Name, "options.txt")),
            ]);

            await AddFolderToWhiteList(profile, [
                new LocalFolderInfo("saves"),
                new LocalFolderInfo("logs"),
                new LocalFolderInfo("resourcepacks"),
                new LocalFolderInfo("crash-reports"),
                new LocalFolderInfo("config")
            ]);

            return profile;
        }

        public async Task<bool> CanAddProfile(string name, string version, string loaderVersion, GameLoader dtoGameLoader)
        {
            if (_gameProfiles.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
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
                    return versions.Any(c => c.Equals(loaderVersion));
                case GameLoader.LiteLoader:
                    return versions.Any(c => c.Equals(loaderVersion));
                case GameLoader.NeoForge:
                    return versions.Any(c => c.Equals(loaderVersion));
                case GameLoader.Quilt:
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
                _gameProfiles = [..profiles];

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

            _profilesChanged.OnNext(0);
        }

        public async Task DownloadProfileAsync(IGameProfile baseProfile, IBootstrapProgram? version = default)
        {
            if (baseProfile is GameProfile gameProfile && await gameProfile.ValidateProfile())
                gameProfile.LaunchVersion =
                    await gameProfile.GameLoader.DownloadGame(baseProfile.GameVersion, baseProfile.LaunchVersion, gameProfile.Loader, version);
        }



        public async Task<IGameProfile?> GetProfile(string profileName)
        {
            await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

            return profile;
        }

        public async Task<IEnumerable<IGameProfile>> GetProfiles()
        {
            await RestoreProfiles();

            return _gameProfiles.AsEnumerable();
        }

        public Task<IFileInfo?> GetProfileFiles(IGameProfile baseProfile, string directory)
        {
            var profileDirectoryInfo = new DirectoryInfo(baseProfile.ClientPath);

            var absolutePath = Path.Combine(profileDirectoryInfo.FullName.Replace(Path.Combine("clients", baseProfile.Name), string.Empty), directory.TrimStart('\\').TrimStart('/'));

            var fileInfo = new FileInfo(absolutePath);

            if (!fileInfo.Exists)
                return Task.FromResult<IFileInfo?>(null);

            using var algorithm = SHA1.Create();
            var hash = SystemHelper.CalculateFileHash(fileInfo.FullName, algorithm);

            return Task.FromResult<IFileInfo?>(new LocalFileInfo
            {
                Name = Path.GetFileName(absolutePath),
                Directory = directory,
                FullPath = fileInfo.FullName,
                Size = fileInfo.Length,
                Hash = hash
            });

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
                    using var algorithm = SHA1.Create();
                    hash = SystemHelper.CalculateFileHash(c.FullName, algorithm);
                    _fileHashCache[c.FullName] = hash;
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

            _ = profile.CreateUserSessionAsync(user);

            var profileDirectory = Path.Combine(profile.ClientPath, "platforms", startupOptions.OsName,
                startupOptions.OsArch);
            var relativePath = Path.Combine("clients", profileName);
            var jvmArgs = new List<string>();
            var gameArguments = new List<string>();

            if (!string.IsNullOrEmpty(profile.JvmArguments))
                jvmArgs.Add(profile.JvmArguments);

            var files =
                await profile.GetProfileFiles(startupOptions.OsName, startupOptions.OsArch);

            if (files.Any(c => c.Name == Path.GetFileName(AuthLibUrl)))
            {
                var authLibRelativePath = Path.Combine(profile.ClientPath, "libraries", "custom", Path.GetFileName(AuthLibUrl));
                jvmArgs.Add($"-javaagent:{authLibRelativePath}={{authEndpoint}}");
            }

            if (profile.GameArguments is not null)
                gameArguments.AddRange(profile.GameArguments.Split(' '));

            Process? process = default;

            try
            {
                process = await profile.GameLoader.CreateProcess(startupOptions, user, false,
                    jvmArgs.ToArray(), gameArguments.ToArray());
            }
            catch (Exception exception)
            {
                _bugTracker.CaptureException(exception);
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
                    DisplayName = profile.DisplayName,
                    Description = profile.Description,
                    IconBase64 = profile.IconBase64,
                    JvmArguments = profile.JvmArguments ?? string.Empty,
                    GameArguments = profile.GameArguments ?? string.Empty,
                    HasUpdate = profile.State != ProfileState.Loading,
                    Arguments = arguments,
                    JavaPath = javaPath,
                    State = profile.State,
                    ClientVersion = profile.GameVersion,
                    MinecraftVersion = profile.GameVersion,
                    LaunchVersion = profile.LaunchVersion ?? string.Empty,
                    Files = files.OfType<LocalFileInfo>(),
                    WhiteListFolders = profile.FolderWhiteList?.OfType<LocalFolderInfo>().ToList() ?? [],
                    WhiteListFiles = profile.FileWhiteList?.OfType<LocalFileInfo>().ToList() ?? []
                };
            }

            return new GameProfileInfo
            {
                ProfileName = profile.Name,
                DisplayName = profile.DisplayName,
                Arguments = string.Empty,
                JavaPath = string.Empty,
                State = profile.State,
                Files = files.OfType<LocalFileInfo>(),
                IconBase64 = profile.IconBase64,
                Description = profile.Description,
                ClientVersion = profile.GameVersion,
                JvmArguments = profile.JvmArguments ?? string.Empty,
                GameArguments = profile.GameArguments ?? string.Empty,
                LaunchVersion = profile.LaunchVersion ?? string.Empty,
                WhiteListFolders = profile.FolderWhiteList?.OfType<LocalFolderInfo>().ToList() ?? [],
                WhiteListFiles = profile.FileWhiteList?.OfType<LocalFileInfo>().ToList() ?? [],
                HasUpdate = profile.State != ProfileState.Loading,
                MinecraftVersion = profile.GameVersion
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
                    await profile.GameLoader.CreateProcess(StartupOptions.Empty, Core.User.User.Empty, true, authLibArguments, []);

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
                _bugTracker.CaptureException(exception);
                throw new Exception($"Не удалось восстановить игровой профиль. {exception}");
            }
            finally
            {
                profile.State = ProfileState.NeedCompile;
            }
        }

        public async Task PackProfile(IGameProfile profile)
        {
            await profile.SetState(ProfileState.Packing);
            var fileInfos = await profile.GetAllProfileFiles(true);

            var batchSize = 50;
            var batches = fileInfos.Select((file, index) => new { file, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.file)).ToList();

            var totalFiles = fileInfos.Length;
            var processed = 0;

            foreach (var batch in batches)
            {
                await Task.WhenAll(batch.Select(async file =>
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
                        _bugTracker.CaptureException(exception);
                        Console.WriteLine(exception);
                        throw;
                    }
                    finally
                    {
                        _packChanged.OnNext(percentage);
                        Debug.WriteLine($"Compile percentage: {percentage} [{processed} / {totalFiles}]");
                    }

                    processed++;
                }));
            }


            // Parallel.ForEach(fileInfos, async file =>
            // {var percentage = processed * 100 / totalFiles;
            //     try
            //     {
            //         var filePath = NormalizePath(_launcherInfo.InstallationDirectory, file.Directory);
            //
            //         switch (_launcherInfo.StorageSettings.StorageType)
            //         {
            //             case StorageType.LocalStorage:
            //                 file.FullPath = filePath;
            //                 if (await _storageService.GetAsync<LocalFileInfo>(file.Hash) is not {} localFile || !File.Exists(localFile.FullPath))
            //                 {
            //                     await _storageService.SetAsync(file.Hash, file);
            //                 }
            //
            //                 break;
            //             case StorageType.S3:
            //                 var tags = new Dictionary<string, string>
            //                 {
            //                     { "hash", file.Hash },
            //                     { "file-name", file.Name }
            //                 };
            //
            //                 if (await _gmlManager.Files.CheckFileExists("profiles", file.Hash) == false)
            //                 {
            //                     await _gmlManager.Files.LoadFile(File.OpenRead(filePath), "profiles", file.Hash, tags);
            //                 }
            //
            //                 break;
            //             default:
            //                 throw new ArgumentOutOfRangeException();
            //         }
            //     }
            //     catch (Exception exception)
            //     {
            //         _bugTracker.CaptureException(exception);
            //         Console.WriteLine(exception);
            //         throw;
            //     }
            //     finally
            //     {
            //         _packChanged.OnNext(percentage);
            //         Debug.WriteLine($"Compile percentage: {percentage}");
            //     }
            //
            //     processed++;
            //
            // });

            // foreach (var file in fileInfos)
            // {
            //     var percentage = processed * 100 / totalFiles;
            //     try
            //     {
            //         var filePath = NormalizePath(_launcherInfo.InstallationDirectory, file.Directory);
            //
            //         switch (_launcherInfo.StorageSettings.StorageType)
            //         {
            //             case StorageType.LocalStorage:
            //                 file.FullPath = filePath;
            //                 if (await _storageService.GetAsync<LocalFileInfo>(file.Hash) is not {} localFile || !File.Exists(localFile.FullPath))
            //                 {
            //                     await _storageService.SetAsync(file.Hash, file);
            //                 }
            //
            //                 break;
            //             case StorageType.S3:
            //                 var tags = new Dictionary<string, string>
            //                 {
            //                     { "hash", file.Hash },
            //                     { "file-name", file.Name }
            //                 };
            //
            //                 if (await _gmlManager.Files.CheckFileExists("profiles", file.Hash) == false)
            //                 {
            //                     await _gmlManager.Files.LoadFile(File.OpenRead(filePath), "profiles", file.Hash, tags);
            //                 }
            //
            //                 break;
            //             default:
            //                 throw new ArgumentOutOfRangeException();
            //         }
            //     }
            //     catch (Exception exception)
            //     {
            //         _bugTracker.CaptureException(exception);
            //         Console.WriteLine(exception);
            //         throw;
            //     }
            //     finally
            //     {
            //         _packChanged.OnNext(percentage);
            //         Debug.WriteLine($"Compile percentage: {percentage}");
            //     }
            //
            //     processed++;
            // }

            await profile.SetState(ProfileState.Ready);
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

        public Task AddFileToWhiteList(IGameProfile profile, IFileInfo file)
        {
            AddWhiteListFileIfNotExists(profile, file);

            return SaveProfiles();
        }

        public Task AddFileToWhiteList(IGameProfile profile, IEnumerable<IFileInfo> files)
        {
            foreach (var file in files)
            {
                AddWhiteListFileIfNotExists(profile, file);
            }

            return SaveProfiles();
        }

        private static void AddWhiteListFileIfNotExists(IGameProfile profile, IFileInfo file)
        {
            profile.FileWhiteList ??= [];

            if (!profile.FileWhiteList.Contains(file))
            {
                profile.FileWhiteList.Add(file);
            }
        }

        public Task RemoveFileFromWhiteList(IGameProfile profile, IFileInfo file)
        {
            profile.FileWhiteList ??= [];

            if (!profile.FileWhiteList.Contains(file))
                return Task.CompletedTask;

            profile.FileWhiteList.Remove(file);

            return SaveProfiles();

        }

        public async Task UpdateProfile(IGameProfile profile,
            string newProfileName,
            string displayName,
            Stream? icon,
            Stream? backgroundImage,
            string updateDtoDescription,
            bool isEnabled,
            string jvmArguments,
            string gameArguments,
            int priority,
            int recommendedRam,
            bool needUpdateImages)
        {
            var directory =
                new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "clients", profile.Name));
            var newDirectory =
                new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "clients", newProfileName));

            var needRenameFolder = profile.Name != newProfileName;

            if (newDirectory.Exists && profile.Name != newProfileName)
                return;

            var iconBase64 = icon is null || !needUpdateImages
                ? null
                : await ConvertStreamToBase64Async(icon);

            var backgroundKey = backgroundImage is null || !needUpdateImages
                ? null
                : await _gmlManager.Files.LoadFile(backgroundImage, "profile-backgrounds");

            await UpdateProfile(
                profile,
                newProfileName,
                displayName,
                needUpdateImages ? iconBase64 : profile.IconBase64,
                needUpdateImages ? backgroundKey : profile.BackgroundImageKey,
                updateDtoDescription,
                needRenameFolder,
                directory,
                newDirectory,
                isEnabled,
                jvmArguments,
                gameArguments,
                priority,
                recommendedRam);
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

        private async Task UpdateProfile(IGameProfile profile, string newProfileName, string displayName,
            string? newIcon,
            string? backgroundImageKey,
            string newDescription, bool needRenameFolder, DirectoryInfo directory, DirectoryInfo newDirectory,
            bool isEnabled,
            string jvmArguments,
            string gameArguments,
            int priority,
            int recommendedRam)
        {
            profile.Name = newProfileName;
            profile.DisplayName = displayName;
            profile.IconBase64 = newIcon;
            profile.BackgroundImageKey = backgroundImageKey;
            profile.Description = newDescription;
            profile.IsEnabled = isEnabled;
            profile.JvmArguments = jvmArguments;
            profile.GameArguments = gameArguments;
            profile.Priority = priority;
            profile.RecommendedRam = recommendedRam;

            profile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, profile, _notifications, _gmlManager.BugTracker);
            profile.State = profile.State == ProfileState.Created ? profile.State : ProfileState.NeedCompile;
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

        public Task<ICollection<IFileInfo>> GetProfileFiles(
            IGameProfile profile,
            string osName,
            string osArchitecture)
        {
            return profile.GameLoader.GetLauncherFiles(osName, osArchitecture);
        }

        public Task<IFileInfo[]> GetAllProfileFiles(IGameProfile baseProfile, bool needRestoreCache = false)
        {
            return baseProfile.GameLoader.GetAllFiles(needRestoreCache);
        }

        public Task<IFileInfo[]> GetModsAsync(IGameProfile baseProfile)
        {
            return baseProfile.GameLoader.GetMods();
        }

        public Task<IFileInfo[]> GetOptionalsModsAsync(IGameProfile baseProfile)
        {
            return baseProfile.GameLoader.GetOptionalsMods();
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
                        using (var client = new HttpClient())
                        {
                            var fabricLoader = new FabricInstaller(client);

                            var loaders = await fabricLoader.GetLoaders(minecraftVersion);

                            var versions = loaders
                                .Where(c => !string.IsNullOrEmpty(c.Version))
                                .OrderBy(c => c.Stable)
                                .Select(c => c.Version!)
                                .ToList()
                                .AsReadOnly();

                            if (!_quiltVersions.Any(c => c.Key == minecraftVersion))
                            {
                                _quiltVersions[minecraftVersion] = versions;
                            }

                            if (_quiltVersions[minecraftVersion] is null || !_quiltVersions[minecraftVersion].Any())
                            {
                                throw new ArgumentOutOfRangeException(nameof(gameLoader), gameLoader, null);
                            }

                            return _quiltVersions[minecraftVersion];
                        }


                    case GameLoader.LiteLoader:
                        var liteLoaderVersionLoader = new LiteLoaderInstaller(new HttpClient());

                        _liteLoaderVersions ??= await liteLoaderVersionLoader.GetAllLiteLoaders();

                        return _liteLoaderVersions
                            .Select(c => c)
                            .Where(c => c.BaseVersion == minecraftVersion)
                            .Select(c => c.Version)!;
                    case GameLoader.NeoForge:
                        var neoForge = new NeoForgeInstaller(anyLauncher);
                        var neoForgeVersionMapper = new NeoForgeInstallerVersionMapper();

                        if (!_neoForgeVersions.Any(c => c.Key == minecraftVersion))
                        {
                            _neoForgeVersions[minecraftVersion] = await neoForge.GetForgeVersions(minecraftVersion);
                        }

                        return _neoForgeVersions[minecraftVersion]
                            .Select(c => neoForgeVersionMapper.CreateInstaller(c).VersionName)
                            .Reverse();
                    case GameLoader.Quilt:
                        using (var client = new HttpClient())
                        {
                            var quiltLoader = new QuiltInstaller(client);

                            var loaders = await quiltLoader.GetLoaders(minecraftVersion);

                            var versions = loaders
                                .Where(c => !string.IsNullOrEmpty(c.Version))
                                .OrderBy(c => c.Stable)
                                .Select(c => c.Version!)
                                .ToList()
                                .AsReadOnly();

                            if (!_fabricVersions.Any(c => c.Key == minecraftVersion))
                            {
                                _fabricVersions[minecraftVersion] = versions;
                            }

                            if (_fabricVersions[minecraftVersion] is null || !_fabricVersions[minecraftVersion].Any())
                            {
                                throw new ArgumentOutOfRangeException(nameof(gameLoader), gameLoader, null);
                            }

                            return _fabricVersions[minecraftVersion];
                        }
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

        public Task ChangeBootstrapProgram(IGameProfile testGameProfile, IBootstrapProgram version)
        {
            return DownloadProfileAsync(testGameProfile, version);
        }

        public Task AddFolderToWhiteList(IGameProfile profile, IFolderInfo folder)
        {
            AddWhiteListFolderIfNotExists(profile, folder);

            return SaveProfiles();
        }

        private void AddWhiteListFolderIfNotExists(IGameProfile profile, IFolderInfo folder)
        {
            profile.FolderWhiteList ??= [];

            if (!profile.FolderWhiteList.Any(c => c == folder))
            {
                profile.FolderWhiteList.Add(folder);
            }
        }

        public Task RemoveFolderFromWhiteList(IGameProfile profile, IFolderInfo folder)
        {
            profile.FolderWhiteList ??= [];

            if (profile.FolderWhiteList.Any(c => c.Path == folder.Path))
                return Task.CompletedTask;

            profile.FolderWhiteList.Remove(folder);

            return SaveProfiles();
        }

        public Task RemoveFolderFromWhiteList(IGameProfile profile, IEnumerable<IFolderInfo> folders)
        {
            foreach (var folder in folders)
            {
                RemoveWhiteListFolderIfNotExists(profile, folder);
            }

            return SaveProfiles();
        }

        public Task AddFolderToWhiteList(IGameProfile profile, IEnumerable<IFolderInfo> folders)
        {
            foreach (var folder in folders)
            {
                AddWhiteListFolderIfNotExists(profile, folder);
            }

            return SaveProfiles();
        }

        public async Task CreateUserSessionAsync(IGameProfile profile, IUser user, string? hostValue = null)
        {
            try
            {
                var skinsService = await _storageService.GetAsync<string>(StorageConstants.SkinUrl) ?? string.Empty;
                var cloakService = await _storageService.GetAsync<string>(StorageConstants.CloakUrl) ?? string.Empty;

                var skinUrl = skinsService.Replace("{userName}", user.Name).Replace("{userUuid}", user.Uuid);
                var cloakUrl = cloakService.Replace("{userName}", user.Name).Replace("{userUuid}", user.Uuid);

                if (user is Core.User.User player)
                {
                    Task[] tasks =
                    [
                        player.DownloadAndInstallCloakAsync(cloakUrl, hostValue),
                        player.DownloadAndInstallSkinAsync(skinUrl, hostValue),
                    ];

                    Task.WaitAll(tasks);

                    Debug.WriteLine($"Skin URL: {player.TextureSkinGuid} | Cloak URL: {player.TextureCloakGuid}");

                    await player.SaveUserAsync();
                }
            }
            catch (Exception exception)
            {
                _bugTracker.CaptureException(exception);
            }

        }

        public async Task<IMod> AddMod(IGameProfile profile, string fileName, Stream streamData)
        {
            var file = await profile.GameLoader.AddMod(fileName, streamData).ConfigureAwait(false);

            return new LocalProfileMod
            {
                Name = Path.GetFileNameWithoutExtension(file.Name),
            };
        }

        public async Task<IMod> AddOptionalMod(IGameProfile profile, string fileName, Stream streamData)
        {
            var extension = Path.GetExtension(fileName);

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (!fileNameWithoutExtension.EndsWith("-optional-mod"))
            {
                fileName = $"{fileNameWithoutExtension}-optional-mod{extension}";
            }
            var file = await profile.GameLoader.AddMod(fileName, streamData).ConfigureAwait(false);

            return new LocalProfileMod
            {
                Name = Path.GetFileNameWithoutExtension(file.Name),
            };
        }

        public Task<bool> RemoveMod(IGameProfile profile, string modName)
        {
            return profile.GameLoader.RemoveMod(modName);
        }

        private void RemoveWhiteListFolderIfNotExists(IGameProfile profile, IFolderInfo folder)
        {
            profile.FolderWhiteList ??= [];

            if (profile.FolderWhiteList.Contains(folder))
            {
                profile.FolderWhiteList.Remove(folder);
            }
        }

        private async Task UpdateProfilesService(GameProfile gameProfile)
        {
            foreach (var server in gameProfile.Servers)
            {
                server.ServerProcedures = _gmlManager.Servers;
                gameProfile.ServerAdded.OnNext(server);
            }

            gameProfile.State = ProfileState.Restoring;
            gameProfile.ProfileProcedures = this;
            gameProfile.ServerProcedures = this;
            gameProfile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, gameProfile, _notifications, _gmlManager.BugTracker);

            if (await gameProfile.GameLoader.ValidateProfile(gameProfile))
                gameProfile.State = ProfileState.Ready;
            else
                gameProfile.State = ProfileState.Error;
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
