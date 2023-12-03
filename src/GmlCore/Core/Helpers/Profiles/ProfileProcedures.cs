using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Exceptions;
using Gml.Core.GameDownloader;
using Gml.Core.Services.Storage;
using Gml.Models;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
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
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (version is null)
                throw new ArgumentNullException(nameof(version));

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

        public Task<IGameProfile?> GetProfile(string profileName)
        {
            var profile = _gameProfiles.FirstOrDefault(c => c.Name == profileName);

            return Task.FromResult(profile);
        }
    }
}