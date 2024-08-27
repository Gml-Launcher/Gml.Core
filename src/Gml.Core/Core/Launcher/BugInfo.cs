using System;
using System.Collections.Generic;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Sentry;
using Newtonsoft.Json;

namespace Gml.Core.Launcher;

public class BugInfo : IBugInfo
{
    public string Id { get; set; } = null!;
    public string? PcName { get; set; }
    public string? Username { get; set; }
    public IMemoryInfo MemoryInfo { get; set; }
    public IEnumerable<IExceptionReport> Exceptions { get; set; }
    public DateTime SendAt { get; set; }
    public string? IpAddress { get; set; }
    public string? OsVeriosn { get; set; }
    public string? OsIdentifier { get; set; }
}
