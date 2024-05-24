using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Servers;
using Newtonsoft.Json;

namespace Gml.Models.Servers;


public class MinecraftServer : IProfileServer
{

    [JsonIgnore] public IProfileServersProcedures ServerProcedures { get; set; }

    public string Name { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public string Version { get; set; }
    public bool IsOnline { get; set; }
    public int Online { get; set; }
    public int MaxOnline { get; set; }
    public void UpdateStatus()
    {
        ServerProcedures.UpdateServerState(this);
    }
}