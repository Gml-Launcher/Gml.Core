using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Gml.Models.Converters;
using Gml.Models.Enums;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Servers;
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
        [JsonIgnore] public IProfileServersProcedures ServerProcedures { get; set; }

        [JsonIgnore] public IGameDownloaderProcedures GameLoader { get; set; }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string GameVersion { get; set; }
        public string? LaunchVersion { get; set; }
        public GameLoader Loader { get; set; }
        public string ClientPath { get; set; }
        public string IconBase64 { get; set; }
        public string BackgroundImageKey { get; set; }
        public string Description { get; set; }

        public string? JvmArguments { get; set; }
        public string? GameArguments { get; set; }
        public ProfileState State { get; set; }

        [JsonConverter(typeof(LocalFileInfoConverter))]
        public List<IFileInfo>? FileWhiteList { get; set; }

        public List<IFolderInfo>? FolderWhiteList { get; set; }

        public List<IProfileServer> Servers { get; set; } = new();

        public DateTimeOffset CreateDate { get; set; }

        public async Task<bool> ValidateProfile()
        {
            CheckDispose();

            IsValidProfile = await ProfileProcedures.ValidateProfileAsync(this)
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

            return GameLoader.CreateProcess(startupOptions, user, false, [], []);
        }

        public Task<bool> CheckClientExists()
        {
            CheckDispose();

            //ToDo: Доделать
            // return GameLoader.CheckClientExists(this);
            return Task.FromResult(true);
        }

        public Task<bool> CheckOsTypeLoaded(IStartupOptions startupOptions)
        {
            CheckDispose();

            //ToDo: Доделать
            // return GameLoader.CheckOsTypeLoaded(this, startupOptions);
            return Task.FromResult(true);
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

        public Task<IProfileServer> AddMinecraftServer(string serverName, string address, int port)
        {
            return ServerProcedures.AddMinecraftServer(this, serverName, address, port);
        }

        public Task<bool> CheckIsFullLoaded(IStartupOptions startupOptions)
        {
            CheckDispose();

            //ToDo: Доделать
            // return await GameLoader.IsFullLoaded(this, startupOptions);

            return Task.FromResult(true);
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

        public Task<bool> CheckIsLoaded()
        {
            CheckDispose();

            //ToDo: Доделать
            // IsLoaded = await GameLoader.IsFullLoaded(this)
            //     ? NullableBool.True
            //     : NullableBool.False;

            return Task.FromResult(IsValidProfile == NullableBool.True);
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

        public virtual void AddServer(IProfileServer server)
        {
            Servers.Add(server);
        }

        public virtual void RemoveServer(IProfileServer server)
        {
            Servers.Remove(server);
        }

        public Task CreateModsFolder()
        {
            return ProfileProcedures.CreateModsFolder(this);
        }

        public Task<IEnumerable<IFileInfo>> GetProfileFiles(string osName, string osArchitecture)
        {
            return ProfileProcedures.GetProfileFiles(this, osName, osArchitecture);
        }

        public Task<IFileInfo[]> GetAllProfileFiles(bool needRestoreCache = false)
        {
            return ProfileProcedures.GetAllProfileFiles(this, needRestoreCache);
        }

        public Task CreateUserSessionAsync(IUser user)
        {
            return ProfileProcedures.CreateUserSessionAsync(this, user);
        }
    }
}
