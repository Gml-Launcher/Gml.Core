namespace GmlCore.Interfaces.Servers;

public interface IProfileServer
{
    public string Name { get; set; }
    void UpdateStatus();
}
