using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Gml.Models.Converters;
using Gml.Models.Enums;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;
using Newtonsoft.Json;

namespace Gml.Models
{
    public class BaseProfile : IGameProfile
    {
        private bool IsDisposed;

        public BaseProfile()
        {
        }

        internal BaseProfile(string name, string gameVersion, GameLoader loader)
        {
            Loader = loader;
            GameVersion = gameVersion;
            Name = name;

            IsValidProfile = NullableBool.Undefined;
        }

        internal NullableBool IsValidProfile { get; set; }
        internal NullableBool IsLoaded { get; set; }

        [JsonIgnore] public IProfileProcedures ProfileProcedures { get; set; }

        [JsonIgnore] public IGameDownloaderProcedures GameLoader { get; set; }

        public string Name { get; set; }
        public string GameVersion { get; set; }
        public string LaunchVersion { get; set; }
        public GameLoader Loader { get; set; }
        public string ClientPath { get; set; }
        public string IconBase64 { get; set; }
        public string BackgroundImageKey { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(LocalFileInfoConverter))]
        public List<IFileInfo>? FileWhiteList { get; set; }

        public DateTimeOffset CreateDate { get; set; }

        public async Task<bool> ValidateProfile()
        {
            CheckDispose();

            IsValidProfile = await ProfileProcedures.ValidateProfileAsync(this)
                ? NullableBool.True
                : NullableBool.False;

            return IsValidProfile == NullableBool.True;
        }

        public async Task DownloadAsync(OsType osType, string osArch)
        {
            CheckDispose();

            await ProfileProcedures.DownloadProfileAsync(this, osType, osArch);
        }

        public Task<Process> CreateProcess(IStartupOptions startupOptions, IUser user)
        {
            CheckDispose();

            return GameLoader.CreateProfileProcess(this, startupOptions, user, false, Array.Empty<string>());
        }

        public Task<bool> CheckClientExists()
        {
            CheckDispose();

            return GameLoader.CheckClientExists(this);
        }

        public Task<bool> CheckOsTypeLoaded(IStartupOptions startupOptions)
        {
            CheckDispose();

            return GameLoader.CheckOsTypeLoaded(this, startupOptions);
        }

        public Task<string[]> InstallAuthLib()
        {
            CheckDispose();

            return ProfileProcedures.InstallAuthLib(this);
        }

        public Task<IGameProfileInfo?> GetCacheProfile()
        {
            CheckDispose();

            return ProfileProcedures.GetCacheProfile(this);
        }

        public async Task<bool> CheckIsFullLoaded(IStartupOptions startupOptions)
        {
            CheckDispose();

            return await GameLoader.IsFullLoaded(this, startupOptions);
        }

        public async Task Remove()
        {
            CheckDispose();

            await ProfileProcedures.RemoveProfile(this);

            Dispose();
        }

        public void Dispose()
        {
            if (IsDisposed) return;


            IsDisposed = true;
        }

        public async Task<bool> CheckIsLoaded()
        {
            CheckDispose();

            IsLoaded = await GameLoader.IsFullLoaded(this)
                ? NullableBool.True
                : NullableBool.False;

            return IsValidProfile == NullableBool.True;
        }

        private void CheckDispose()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(Name);
        }

        public void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
            }

            IsDisposed = true;
        }
    }
}
