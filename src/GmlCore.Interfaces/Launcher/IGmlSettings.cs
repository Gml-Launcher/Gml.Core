namespace GmlCore.Interfaces.Launcher
{
    public interface IGmlSettings
    {
        public string Name { get; }
        public string BaseDirectory { get; }
        public string InstallationDirectory { get; }
    }
}