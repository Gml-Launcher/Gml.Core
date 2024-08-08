using System.Collections.Generic;

namespace GmlCore.Interfaces.Launcher;

public interface IBugInfo
{
    public IEnumerable<IBug> Bugs { get; set; }
}
