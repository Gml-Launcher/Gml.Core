namespace GmlCore.Interfaces.User;

public interface IHardware
{
    public string? CpuIdentifier { get; set; }
    public string? MotherboardIdentifier { get; set; }
    public string? DiskIdentifiers { get; set; }
}
