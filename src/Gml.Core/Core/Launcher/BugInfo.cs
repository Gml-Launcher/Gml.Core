using System.Collections.Generic;
using GmlCore.Interfaces.Launcher;

namespace Gml.Core.Launcher;

public class BugInfo : IBugInfo
{
    public IEnumerable<IBug> Bugs { get; set; }
}

public class Bug : IBug
{
    public string? PcName { get; set; }
    public string? PcUsername { get; set; }
    public string? Username { get; set; }
    public string? Exception { get; set; }
    public string? IpAddress { get; set; }
    public string? OsType { get; set; }
    public string? OsIdentifier { get; set; }
}
