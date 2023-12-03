using System.Threading.Tasks;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;

namespace GmlCore.Interfaces.Procedures
{
    public interface IProfileProcedures
    {
        Task AddProfile(IGameProfile? profile);
        Task<IGameProfile?> AddProfile(string name, string version, GameLoader loader);
        
        Task<bool> CanAddProfile(string name, string version);
        
        Task RemoveProfile(IGameProfile profile);
        Task RestoreProfiles();
        Task RemoveProfile(int profileId);
        Task ClearProfiles();
        Task<bool> ValidateProfileAsync(IGameProfile baseProfile);
        bool ValidateProfile();
        Task SaveProfiles();
        Task DownloadProfileAsync(IGameProfile baseProfile);
        Task<IGameProfile?> GetProfile(string profileName);
    }
}