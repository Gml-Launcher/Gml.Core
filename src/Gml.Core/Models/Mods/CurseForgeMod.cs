using System.Collections.Generic;
using System.IO;
using GmlCore.Interfaces.Mods;

namespace Gml.Models.Mods;

public class CurseForgeMod : IExternalMod
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public int DownloadCount { get; set; }
    public int FollowsCount { get; set; }
    public IReadOnlyCollection<string> Files { get; set; }
    public IReadOnlyCollection<IMod> Dependencies { get; set; }
    public ModType Type { get; }
    public Stream Icon { get; set; }
}
