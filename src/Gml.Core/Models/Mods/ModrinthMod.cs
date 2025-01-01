using System.Collections.Generic;
using System.IO;
using GmlCore.Interfaces.Mods;

namespace Gml.Models.Mods;

public class ModrinthMod : IMod
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public int FollowsCount { get; set; }
    public int DownloadCount { get; set; }
    public Stream Icon { get; set; }
    public IEnumerable<string> Files { get; set; }
    public IEnumerable<IMod> Dependencies { get; set; }
    public ModType Type => ModType.Modrinth;
}
