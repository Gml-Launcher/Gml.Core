using System;
using GmlCore.Interfaces.Sentry;

namespace Gml.Dto.Sentry;

public class SentryFilterDto
{
    public ProjectType ProjectType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
