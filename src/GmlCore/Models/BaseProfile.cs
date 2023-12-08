using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Models.Enums;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.User;

namespace Gml.Models
{
    public class BaseProfile : IGameProfile
    {
        public IProfileProcedures ProfileProcedures { get; set; }
        public IGameDownloaderProcedures GameLoader { get; set; }

        public string Name { get; set; }
        public string GameVersion { get; set; }
        public string LaunchVersion { get; set; }
        public GameLoader Loader { get; set; }
        public string ClientPath { get; set; }

        internal NullableBool IsValidProfile { get; set; }
        internal NullableBool IsLoaded { get; set; }

        private bool IsDisposed = false;

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

        public async Task<bool> ValidateProfile()
        {
            CheckDispose();

            IsValidProfile = await ProfileProcedures.ValidateProfileAsync(this)
                ? NullableBool.True
                : NullableBool.False;

            return IsValidProfile == NullableBool.True;
        }

        public async Task<bool> CheckIsLoaded()
        {
            CheckDispose();

            IsLoaded = await GameLoader.IsFullLoaded(this)
                ? NullableBool.True
                : NullableBool.False;

            return IsValidProfile == NullableBool.True;
        }

        public async Task DownloadAsync()
        {
            CheckDispose();

            await ProfileProcedures.DownloadProfileAsync(this);
        }

        public Task<Process> CreateProcess(IStartupOptions startupOptions, IUser user)
        {
            CheckDispose();

            return GameLoader.CreateProfileProcess(this, startupOptions, user, false);
        }

        public Task<bool> CheckClientExists()
        {
            CheckDispose();

            return GameLoader.CheckClientExists(this);
        }

        public async Task<bool> CheckIsFullLoaded()
        {
            CheckDispose();

            return await GameLoader.IsFullLoaded(this);
        }

        public async Task Remove()
        {
            CheckDispose();

            await ProfileProcedures.RemoveProfile(this);

            Dispose();
        }

        private void CheckDispose()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(Name);
        }

        public void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing) { }

            IsDisposed = true;
        }

        public void Dispose()
        {
            if (IsDisposed) return;


            IsDisposed = true;
        }
    }
}
