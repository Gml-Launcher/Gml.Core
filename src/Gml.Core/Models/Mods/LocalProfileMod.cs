using System.Collections.Generic;
using System.IO;
using GmlCore.Interfaces.Mods;

namespace Gml.Models.Mods;

public class LocalProfileMod : IMod
{
    public IEnumerable<string> Files { get; set; }
    public IEnumerable<IMod> Dependencies { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public Stream Icon { get; set; }
    public ModType Type => ModType.Local;
}
