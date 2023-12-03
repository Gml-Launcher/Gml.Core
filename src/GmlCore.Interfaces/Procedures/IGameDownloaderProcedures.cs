using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CmlLib.Core.Downloader;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;

namespace GmlCore.Interfaces.Procedures
{
    public interface IGameDownloaderProcedures
    {
        public delegate void FileDownloadChanged(DownloadFileChangedEventArgs file);
        public delegate void ProgressDownloadChanged(object sender, ProgressChangedEventArgs e);

        public event FileDownloadChanged FileChanged;
        public event ProgressDownloadChanged ProgressChanged;
        
        Task<string> DownloadGame(string version, GameLoader loader);
        Task<bool> IsFullLoaded(IGameProfile baseProfile);
        Task<Process> CreateProfileProcess(IGameProfile baseProfile, IStartupOptions startupOptions);
        Task<bool> CheckClientExists(IGameProfile baseProfile);
    }
}