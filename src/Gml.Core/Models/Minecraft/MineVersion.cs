using GmlCore.Interfaces.Versions;

namespace Gml.Models.Minecraft;

public class MineVersion : IVersion
{
    public string Name { get; set; }
    public bool IsRelease { get; set; }
}
