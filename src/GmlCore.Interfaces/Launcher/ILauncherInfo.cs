namespace GmlCore.Interfaces.Launcher
{
    public interface ILauncherInfo
    {
        public string Name { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }
    }
}