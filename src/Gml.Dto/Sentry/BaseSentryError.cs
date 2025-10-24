using System.Collections.Generic;
using Gml.Domains.Sentry;

namespace Gml.Dto.Sentry;

public class BaseSentryError
{
    public IEnumerable<SentryBugs> Bugs { get; set; }
    public long CountUsers { get; set; }
    public long Count { get; set; }
}
