using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;
using GmlCore.Interfaces.Procedures;
// using Modrinth.Api.Domains.Models.Dto;

namespace Gml.Core.Helpers.Mods;

public class ModsProcedures : IModsProcedures
{
    // private readonly ModrinthApi _modrinthApi = new();

    public async Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile)
    {
        // Task<SearchProjectResultDto>? mods = _modrinthApi.ModsProject.FindAsync();


        throw new NotImplementedException();
    }

    public Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile, string name)
    {
        throw new NotImplementedException();
    }
}
