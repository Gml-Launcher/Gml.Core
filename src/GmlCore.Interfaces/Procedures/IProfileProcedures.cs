using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.System;
using GmlCore.Interfaces.User;

namespace GmlCore.Interfaces.Procedures
{
    public interface IProfileProcedures
    {
        Task AddProfile(IGameProfile? profile);
        Task<IGameProfile?> AddProfile(string name, string version, GameLoader loader);

        Task<bool> CanAddProfile(string name, string version);

        Task RemoveProfile(IGameProfile profile);
        Task RemoveProfile(IGameProfile profile, bool removeProfileFiles);
        Task RestoreProfiles();
        Task RemoveProfile(int profileId);
        Task ClearProfiles();
        Task<bool> ValidateProfileAsync(IGameProfile baseProfile);
        bool ValidateProfile();
        Task SaveProfiles();
        Task DownloadProfileAsync(IGameProfile baseProfile);
        Task<IEnumerable<IFileInfo>> GetProfileFiles(IGameProfile baseProfile);
        Task<IGameProfile?> GetProfile(string profileName);
        Task<IEnumerable<IGameProfile>> GetProfiles();
        Task<IGameProfileInfo?> GetProfileInfo(string profileName, IStartupOptions startupOptions, IUser user);
        Task<IGameProfileInfo?> RestoreProfileInfo(string profileName, IStartupOptions startupOptions, IUser user);
        Task PackProfile(IGameProfile baseProfile);
    }
}
