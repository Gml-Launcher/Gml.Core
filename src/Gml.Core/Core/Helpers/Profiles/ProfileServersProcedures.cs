using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gml.Models.Servers;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Servers;
using Pingo;
using Pingo.Status;

namespace Gml.Core.Helpers.Profiles;

public partial class ProfileProcedures : IProfileServersProcedures
{
    public async Task<IProfileServer> AddMinecraftServer(IGameProfile profile, string serverName, string address, int port)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile), "Profile cannot be null.");
        }

        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException("Address cannot be null or empty.", nameof(address));
        }

        if (port is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        if (profile.Servers.Any(c => c.Name.Equals(serverName)))
        {
            throw new Exception("Сервер с таким наименованием уже присутствует в данном профиле");
        }

        var server = new MinecraftServer
        {
            Address = address,
            Name = serverName,
            Port = port,
            ServerProcedures = this
        };

        profile.AddServer(server);

        await SaveProfiles();

        return server;
    }

    public async Task UpdateServerState(IProfileServer server)
    {
        try
        {
            if (server is MinecraftServer minecraftServer)
            {
                var options = new MinecraftPingOptions
                {
                    Address = minecraftServer.Address,
                    Port = (ushort)minecraftServer.Port
                };

                var status = await Minecraft.PingAsync(options) as JavaStatus;

                minecraftServer.Online = status?.OnlinePlayers;
                minecraftServer.MaxOnline = status?.MaximumPlayers;
                minecraftServer.Version = status?.Name ?? string.Empty;
                minecraftServer.IsOnline = status?.MaximumPlayers is not null;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    public async Task RemoveServer(IGameProfile profile, string serverName)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile), "Profile cannot be null.");
        }

        if (string.IsNullOrEmpty(serverName))
        {
            throw new ArgumentException("Server name cannot be null or empty.", nameof(serverName));
        }

        var server = profile.Servers.FirstOrDefault(c => c.Name == serverName);

        if (server is not null)
        {
            profile.Servers.Remove(server);
        }

        await SaveProfiles();
    }
}
