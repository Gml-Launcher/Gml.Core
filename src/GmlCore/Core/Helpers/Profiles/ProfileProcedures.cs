using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using NotImplementedException = System.NotImplementedException;

namespace Gml.Core.Helpers.Profiles
{
    public class ProfileProcedures : IProfileProcedures
    {
        private readonly IGameDownloaderProcedures _gameDownloader;
        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storageService;
        private List<IGameProfile> _gameProfiles = new List<IGameProfile>();

        public ProfileProcedures(IGameDownloaderProcedures gameDownloader, ILauncherInfo launcherInfo,
            IStorageService storageService)
        {
            _gameDownloader = gameDownloader;
            _launcherInfo = launcherInfo;
            _storageService = storageService;
        }

        public async Task AddProfile(IGameProfile? profile)
        {
            if (!_gameProfiles.Any())
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

        public async Task<IGameProfile?> AddProfile(string name, string version, GameLoader loader)
        {
            if (string.IsNullOrEmpty(name))
                ThrowHelper.ThrowArgumentNullException<string>(name);

            if (string.IsNullOrEmpty(version))
                ThrowHelper.ThrowArgumentNullException<string>(version);

            var profile = new GameProfile(name, version, loader)
            {
                ProfileProcedures = this,
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


        public async Task RemoveProfile(IGameProfile profile)
        {
            if (!_gameProfiles.Any())
                await RestoreProfiles();

            var localProfile = _gameProfiles.FirstOrDefault(c => c.Name == profile.Name);

            _gameProfiles.Remove(profile);

            await _storageService.SetAsync(StorageConstants.GameProfiles, _gameProfiles);
        }

        public async Task RestoreProfiles()
        {
            var profiles = await _storageService.GetAsync<List<GameProfile>>(StorageConstants.GameProfiles);

            if (profiles != null)
            {
                profiles.ForEach(UpdateProfilesService);

                _gameProfiles.AddRange(profiles);
            }
        }

        private async void UpdateProfilesService(GameProfile c)
        {
            var gameLoader = new GameDownloaderProcedures(_launcherInfo, _storageService, c);

            c.ProfileProcedures = this;
            c.GameLoader = gameLoader;

            c.LaunchVersion = await gameLoader.ValidateMinecraftVersion(c.GameVersion, c.Loader);
            c.GameVersion = gameLoader.InstallationVersion!.Id;
        }

        public Task RemoveProfile(int profileId)
        {
            throw new NotImplementedException();
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
            {
                gameProfile.LaunchVersion =
                    await gameProfile.GameLoader.DownloadGame(gameProfile.GameVersion, gameProfile.Loader);
            }
        }

        public async Task<IGameProfile?> GetProfile(string profileName)
        {
            if (!_gameProfiles.Any())
                await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            return profile;
        }

        public async Task<IEnumerable<IGameProfile>> GetProfiles()
        {
            if (!_gameProfiles.Any())
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

            IEnumerable<IFileInfo> localFilesInfo = localFiles.Select(c => new LocalFileInfo
            {
                Name = c.Name,
                Directory = c.FullName.Replace(_launcherInfo.InstallationDirectory, string.Empty),
                Size = c.Length,
                Hash = SystemHelper.CalculateFileHash(c.FullName, new SHA256Managed())
            });

            return Task.FromResult(localFilesInfo);
        }

        public async Task<IGameProfileInfo?> GetProfileInfo(string profileName, IStartupOptions startupOptions, IUser user)
        {
            if (!_gameProfiles.Any())
                await RestoreProfiles();

            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            if (profile == null)
                return null;

            Process process;

            if (await profile.CheckIsFullLoaded() == false)
            {
                await profile.DownloadAsync();
                process = await profile.GameLoader.CreateProfileProcess(profile, startupOptions, user, true);
            }
            else
            {
                process = await profile.GameLoader.CreateProfileProcess(profile, startupOptions, user, false);
            }

            var files = await GetProfileFiles(profile);
            var files2 = GetWhiteListFilesProfileFiles(files);

            return new GameProfileInfo
            {
                ProfileName = profile.Name,
                Arguments = process.StartInfo.Arguments.Replace(profile.ClientPath, "{localPath}"),
                ClientVersion = profile.GameVersion,
                MinecraftVersion = profile.LaunchVersion.Split('-').First(),
                Files = files,
                WhiteListFiles = files2
            };

        }

        public async Task PackProfile(IGameProfile profile)
        {

            await profile.GameLoader.CreateProfileProcess(profile, StartupOptions.Empty, User.User.Empty, true);

            var files = await GetProfileFiles(profile);

            foreach (var file in files)
                await _storageService.SetAsync(file.Hash, file);

        }
    }
}
