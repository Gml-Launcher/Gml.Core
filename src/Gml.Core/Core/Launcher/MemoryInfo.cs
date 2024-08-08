using System.Collections.Generic;
using GmlCore.Interfaces.Sentry;

namespace Gml.Core.Launcher;

public class MemoryInfo : IMemoryInfo
{
    public long AllocatedBytes { get; set; }
    public long HighMemoryLoadThresholdBytes { get; set; }
    public long TotalAvailableMemoryBytes { get; set; }
    public int FinalizationPendingCount { get; set; }
    public bool Compacted { get; set; }
    public bool Concurrent { get; set; }
    public List<double> PauseDurations { get; set; }
}
