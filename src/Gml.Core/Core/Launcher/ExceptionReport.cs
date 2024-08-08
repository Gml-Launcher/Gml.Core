using System.Collections.Generic;
using GmlCore.Interfaces.Sentry;

namespace Gml.Core.Launcher;

public class ExceptionReport : IExceptionReport
{
    public string Type { get; set; }
    public string ValueData { get; set; }
    public string Module { get; set; }
    public int ThreadId { get; set; }
    public int Id { get; set; }
    public bool Crashed { get; set; }
    public bool Current { get; set; }
    public IEnumerable<IStackTrace> StackTrace { get; set; }
}
