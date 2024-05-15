using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gml.Models.Servers;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Servers;

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
            Port = port
        };

        profile.Servers.Add(server);

        await SaveProfiles();

        return server;
    }
}
