using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Procedures;

namespace GmlCore.Interfaces.Launcher
{
    public interface IGameProfile : IDisposable
    {
        
        [JsonIgnore] IProfileProcedures ProfileProcedures { get; set; }
        [JsonIgnore] IGameDownloaderProcedures GameLoader { get; set; }
        
        string Name { get; set; }
        string GameVersion { get; set; }
        string LaunchVersion { get; set; }
        GameLoader Loader { get; }
        string ClientPath { get; set; }

        Task<bool> ValidateProfile();
        Task<bool> CheckIsFullLoaded();
        Task Remove();
        Task DownloadAsync();
        Task<Process> CreateProcess(IStartupOptions startupOptions);
        Task<bool> CheckClientExists();
    }
}