using GmlCore.Interfaces.Launcher;

namespace GmlCore.Interfaces.Procedures;

public interface IBugTrackerProcedures
{
    void CaptureException(IBugInfo bugInfo);
}
