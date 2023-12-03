using GmlCore.Interfaces.Launcher;

namespace Gml.Core.Launcher
{
    public class LauncherInfo : ILauncherInfo
    {
        public string Name => _settings.Name;
        public string BaseDirectory => _settings.BaseDirectory;
        public string InstallationDirectory => _settings.InstallationDirectory;

        private readonly IGmlSettings _settings;

        public LauncherInfo(IGmlSettings settings)
        {
            _settings = settings;
        }
    }
}