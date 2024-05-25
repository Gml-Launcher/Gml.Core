using System.Threading.Tasks;

namespace GmlCore.Interfaces.Servers;

public interface IProfileServer
{
    public string Name { get; set; }
    Task UpdateStatusAsync();
}
