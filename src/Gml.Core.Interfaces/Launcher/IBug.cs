namespace GmlCore.Interfaces.Launcher;

public interface IBug
{
    public string? PcName { get; set; }
    public string? PcUsername { get; set; }
    public string? Username { get; set; }
    public string? Exception { get; set; }
    public string? IpAddress { get; set; }
    public string? OsType { get; set; }
    public string? OsIdentifier { get; set; }
}
