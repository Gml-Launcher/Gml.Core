using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace GmlCore.Interfaces
{
    public interface IGmlManager
    {
        public ILauncherInfo LauncherInfo { get; }
        public IProfileProcedures Profiles { get; }
    }
}