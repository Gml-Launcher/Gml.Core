using GmlCore.Interfaces.Mods;

namespace Gml.Models.Mods;

public class ModInfo : IModInfo
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}
