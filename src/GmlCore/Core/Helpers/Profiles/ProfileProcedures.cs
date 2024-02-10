using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Gml.Common;
using Gml.Core.Constants;
using Gml.Core.Exceptions;
using Gml.Core.GameDownloader;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Core.System;
using Gml.Models;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gml.Core.Helpers.Profiles
{
    public class ProfileProcedures : IProfileProcedures
    {
        public delegate void ProgressPackChanged(ProgressChangedEventArgs e);

        public event IProfileProcedures.ProgressPackChanged PackChanged;

        private readonly IGameDownloaderProcedures _gameDownloader;


        private readonly ILauncherInfo _launcherInfo;


        private readonly IStorageService _storageService;


        private List<IGameProfile> _gameProfiles = new();

        private const string authLibUrl =
            "https://github.com/yushijinhun/authlib-injector/releases/download/v1.2.4/authlib-injector-1.2.4.jar";


        public ProfileProcedures(IGameDownloaderProcedures gameDownloader, ILauncherInfo launcherInfo,
            IStorageService storageService)
        {
            _gameDownloader = gameDownloader;
            _launcherInfo = launcherInfo;
            _storageService = storageService;
        }

        public async Task AddProfile(IGameProfile? profile)
        {
            await RestoreProfiles();

            if (profile is null)
                throw new ArgumentNullException(nameof(profile));

            if (_gameProfiles.Any(c => c.Name == profile.Name))
                throw new ProfileExistException(profile);

            profile.ProfileProcedures = this;
            profile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, profile);

            if (profile.GameLoader is GameDownloaderProcedures gameLoader)
            {
                profile.LaunchVersion = await gameLoader.ValidateMinecraftVersion(profile.GameVersion, profile.Loader);
                profile.GameVersion = gameLoader.InstallationVersion!.Id;
            }

            _gameProfiles.Add(profile);

            await _storageService.SetAsync(StorageConstants.GameProfiles, _gameProfiles);
        }


        public async Task<IGameProfile?> AddProfile(string name,
            string version,
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
                CreateDate = DateTimeOffset.Now,
                Description = description,
                IconBase64 = icon
            };

            await AddProfile(profile);

            return profile;
        }

        public Task<bool> CanAddProfile(string name, string version)
        {
            if (_gameProfiles.Any(c => c.Name == name))
                return Task.FromResult(false);

            return Task.FromResult(true);
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
                var info = await GetProfileInfo(localProfile.Name, StartupOptions.Empty, Gml.Core.User.User.Empty);

                if (info is GameProfileInfo profileInfo)
                {
                    var clientPath = _launcherInfo.InstallationDirectory + $"\\clients\\{profileInfo.ProfileName}";

                    if (Directory.Exists(clientPath)) Directory.Delete(clientPath, true);
                }
            }

            _gameProfiles.Remove(localProfile);

            await _storageService.SetAsync(StorageConstants.GameProfiles, _gameProfiles);
        }

        public async Task RestoreProfiles()
        {
            var profiles = await _storageService.GetAsync<List<GameProfile>>(StorageConstants.GameProfiles);

            if (profiles != null)
            {
                profiles = profiles.Where(c => c != null).ToList();

                profiles.ForEach(UpdateProfilesService);

                _gameProfiles = new List<IGameProfile>(profiles);
            }
        }


        private async void UpdateProfilesService(GameProfile gameProfile)
        {
            var gameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, gameProfile);

            gameProfile.ProfileProcedures = this;
            gameProfile.GameLoader = gameLoader;

            gameProfile.LaunchVersion =
                await gameLoader.ValidateMinecraftVersion(gameProfile.GameVersion, gameProfile.Loader);
            gameProfile.GameVersion = gameLoader.InstallationVersion!.Id;
        }


        public Task RemoveProfile(int profileId)
        {
            var profile = _gameProfiles[profileId];

            return RemoveProfile(profile, false);
        }

        public async Task ClearProfiles()
        {
            _gameProfiles = new List<IGameProfile>();

            await _storageService.SetAsync(StorageConstants.GameProfiles, _gameProfiles);
        }

        public async Task<bool> ValidateProfileAsync(IGameProfile baseProfile)
        {
            // ToDo: Сделать проверку верности профиля через схему
            await Task.Delay(1000);

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
                    await gameProfile.GameLoader.DownloadGame(baseProfile.GameVersion, gameProfile.Loader);
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

        public IEnumerable<IFileInfo> GetWhiteListFilesProfileFiles(IEnumerable<IFileInfo> files)
        {
            return files.Where(c => c.Directory.EndsWith("options.txt"));
        }

        public Task<IEnumerable<IFileInfo>> GetProfileFiles(IGameProfile baseProfile)
        {
            var profileDirectoryInfo = new DirectoryInfo(baseProfile.ClientPath);

            var localFiles = profileDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

            var algorithm = new SHA256Managed();

            IEnumerable<IFileInfo> localFilesInfo = localFiles.Select(c => new LocalFileInfo
            {
                Name = c.Name,
                Directory = c.FullName.Replace(_launcherInfo.InstallationDirectory, string.Empty),
                Size = c.Length,
                Hash = SystemHelper.CalculateFileHash(c.FullName, algorithm)
            });

            return Task.FromResult(localFilesInfo);
        }

        public async Task<IGameProfileInfo?> GetProfileInfo(string profileName, IStartupOptions startupOptions,
            IUser user)
        {
            if (!_gameProfiles.Any())
                await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            if (profile == null)
                return null;

            try
            {
                var jvmArgs = new List<string>();
                var files = (await GetProfileFiles(profile)).ToList();

                if (files.Any(c => c.Name == Path.GetFileName(authLibUrl)))
                {
                    var authLibRelativePath =
                        Path.Combine(profile.ClientPath, "libraries", Path.GetFileName(authLibUrl));
                    jvmArgs.Add($"-javaagent:{authLibRelativePath}={{authEndpoint}}");
                }

                Process? process = null;
                try
                {
                    process = await profile.GameLoader.CreateProfileProcess(profile, startupOptions, user, false,
                        jvmArgs.ToArray());
                }
                catch (Exception e)
                {
                }

                return new GameProfileInfo
                {
                    ProfileName = profile.Name,
                    Description = profile.Description,
                    IconBase64 = profile.IconBase64,
                    Arguments = process?.StartInfo.Arguments.Replace(profile.ClientPath, "{localPath}") ?? string.Empty,
                    JavaPath = process?.StartInfo.FileName.Replace(profile.ClientPath, "{localPath}") ?? string.Empty,
                    ClientVersion = profile.GameVersion,
                    MinecraftVersion = profile.LaunchVersion.Split('-').First(),
                    Files = files.OfType<LocalFileInfo>(),
                    WhiteListFiles = profile.FileWhiteList?.OfType<LocalFileInfo>() ?? new List<LocalFileInfo>()
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new GameProfileInfo
            {
                ProfileName = profile.Name,
                Arguments = string.Empty,
                JavaPath = string.Empty,
                IconBase64 = profile.IconBase64,
                Description = profile.Description,
                ClientVersion = profile.GameVersion,
                MinecraftVersion = profile.LaunchVersion.Split('-').First()
            };
        }

        public async Task<IGameProfileInfo?> RestoreProfileInfo(string profileName, IStartupOptions startupOptions,
            IUser user)
        {
            await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            if (profile == null)
                return null;

            await profile.DownloadAsync();
            var authLibArguments = await profile.InstallAuthLib();
            var process =
                await profile.GameLoader.CreateProfileProcess(profile, startupOptions, user, true, authLibArguments);

            var files = (await GetProfileFiles(profile)).ToList();
            var files2 = GetWhiteListFilesProfileFiles(files);

            return new GameProfileInfo
            {
                ProfileName = profile.Name,
                Arguments = process.StartInfo.Arguments.Replace(profile.ClientPath, "{localPath}"),
                ClientVersion = profile.GameVersion,
                MinecraftVersion = profile.LaunchVersion.Split('-').First(),
                Files = files.OfType<LocalFileInfo>(),
                WhiteListFiles = files2.OfType<LocalFileInfo>()
            };
        }


        public async Task PackProfile(IGameProfile profile)
        {
            var files = await GetProfileFiles(profile);

            var fileInfos = files as IFileInfo[] ?? files.ToArray();
            var totalFiles = fileInfos.Count();
            var processed = 0;

            foreach (var file in fileInfos)
            {
                await _storageService.SetAsync(file.Hash, file);

                processed++;

                var percentage = (processed * 100) / totalFiles;

                PackChanged?.Invoke(new ProgressChangedEventArgs(percentage, null));
            }
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

        public async Task UpdateProfile(IGameProfile profile, string newProfileName, string newIcon,
            string newDescription)
        {
            var directory =
                new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "clients", profile.Name));
            var newDirectory =
                new DirectoryInfo(Path.Combine(_launcherInfo.InstallationDirectory, "clients", newProfileName));

            bool needRenameFolder = profile.Name != newProfileName;

            if (newDirectory.Exists && profile.Name != newProfileName)
                return;

            profile.Name = newProfileName;
            profile.IconBase64 = newIcon;
            profile.Description = newDescription;

            profile.GameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, profile);

            await SaveProfiles();
            await RestoreProfiles();

            if (needRenameFolder)
            {
                RenameFolder(directory.FullName, newDirectory.FullName);
            }
        }

        public async Task<string[]> InstallAuthLib(IGameProfile profile)
        {
            var directory =
                new DirectoryInfo(profile.ClientPath);
            var authLibPath = new DirectoryInfo(Path.Combine(directory.FullName, "libraries"));
            var downloadedFileInfo = new FileInfo(authLibUrl);
            var downloadingFileInfo = new FileInfo(Path.Combine(authLibPath.FullName, downloadedFileInfo.Name));
            var authLibRelativePath = downloadingFileInfo.FullName.Replace($"{authLibPath.FullName}\\", string.Empty);

            if (!authLibPath.Exists)
                authLibPath.Create();

            if (downloadingFileInfo.Exists)
                return new string[] { $"-javaagent:{{localPath}}\\libraries\\{authLibRelativePath}={{authEndpoint}}" };

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(authLibUrl);

                await using Stream contentStream = await response.Content.ReadAsStreamAsync(),
                    fileStream = new FileStream(downloadingFileInfo.FullName, FileMode.Create, FileAccess.Write,
                        FileShare.None, 8192, true);
            }

            downloadingFileInfo.Refresh();

            var gameVersion = profile.LaunchVersion.Split("-").First();

            var manifestFilePath =
                new FileInfo(Path.Combine(profile.ClientPath, "client", gameVersion, $"{gameVersion}.json"));

            var content = await File.ReadAllTextAsync(manifestFilePath.FullName) ?? string.Empty;

            var jObject = JObject.Parse(content);

            var newLibrary = new JObject(
                new JProperty("downloads",
                    new JObject(
                        new JProperty("artifact",
                            new JObject(
                                new JProperty("path",
                                    downloadingFileInfo.FullName.Replace($"{authLibPath.FullName}\\", string.Empty)),
                                new JProperty("sha1",
                                    SystemHelper.CalculateFileHash(downloadingFileInfo.FullName, new SHA256Managed())),
                                new JProperty("size", downloadingFileInfo.Length),
                                new JProperty("url", authLibUrl)
                            )
                        )
                    )
                )
            );

            var libraries = (JArray)jObject["libraries"]!;
            libraries.Add(newLibrary);

            await File.WriteAllTextAsync(manifestFilePath.FullName, jObject.ToString());

            return new[] { $"-javaagent:{{localPath}}\\{authLibRelativePath}={{authEndpoint}}" };
        }

        public async Task<IGameProfileInfo?> GetCacheProfile(IGameProfile baseProfile)
        {
            return await _storageService.GetAsync<GameProfileInfo>($"CachedProfile-{baseProfile.Name}");
        }

        public Task SetCacheProfile(IGameProfileInfo profile)
        {
            return _storageService.SetAsync($"CachedProfile-{profile.ProfileName}", (GameProfileInfo)profile);
        }

        /// <summary>
        /// Renames a folder name
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
                {
                    return false;
                }


                var oldDirectory = new DirectoryInfo(directory);

                if (!oldDirectory.Exists)
                {
                    return false;
                }

                if (string.Equals(oldDirectory.Name, newFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    //new folder name is the same with the old one.
                    return false;
                }

                string newDirectory;

                if (oldDirectory.Parent == null)
                {
                    //root directory
                    newDirectory = Path.Combine(directory, newFolderName);
                }
                else
                {
                    newDirectory = Path.Combine(oldDirectory.Parent.FullName, newFolderName);
                }

                if (Directory.Exists(newDirectory))
                {
                    Directory.Delete(newDirectory, true);
                }

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
