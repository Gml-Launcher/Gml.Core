using System.Collections.Generic;
using System.IO;

namespace GmlCore.Interfaces.Mods;

public interface IMod
{
    string Name { get; set; }
    string Url { get; set; }
    ModType Type { get; }
    Stream Icon { get; set; }
}

public interface IExternalMod : IMod
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public int DownloadCount { get; set; }
    public int FollowsCount { get; set; }
    IReadOnlyCollection<string> Files { get; set; }
    IReadOnlyCollection<IMod> Dependencies { get; set; }
}

public enum ModType
{
    Local,
    Modrinth
}
