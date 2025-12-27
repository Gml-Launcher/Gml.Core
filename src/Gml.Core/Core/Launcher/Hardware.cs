using System.Linq;
using GmlCore.Interfaces.User;

namespace Gml.Core.Launcher;

public class Hardware : IHardware
{
    public Hardware()
    {
    }

    public Hardware(string? cpuIdentifier, string? motherboardIdentifier, string? diskIdentifiers)
    {
        CpuIdentifier = cpuIdentifier;
        MotherboardIdentifier = motherboardIdentifier;
        DiskIdentifiers = diskIdentifiers;
    }

    public Hardware(string? hwid)
    {
        if (string.IsNullOrEmpty(hwid))
            return;

        var parts = hwid.Split('-');
        if (parts.Length >= 3)
        {
            CpuIdentifier = parts[0];
            MotherboardIdentifier = parts[1];
            DiskIdentifiers = string.Join("-", parts.Skip(2));
        }
    }

    public string? CpuIdentifier { get; set; }
    public string? MotherboardIdentifier { get; set; }
    public string? DiskIdentifiers { get; set; }
}
