using GmlCore.Interfaces.Bootstrap;

namespace Gml.Models.Bootstrap;

public class JavaBootstrapProgram(string name, string version, int majorVersion) : IBootstrapProgram
{
    public string Name { get; set; } = name;
    public string Version { get; set; } = version;
    public int MajorVersion { get; set; } = majorVersion;

    public override string ToString()
    {
        return $"Java: {Name} | {Version}";
    }
}
