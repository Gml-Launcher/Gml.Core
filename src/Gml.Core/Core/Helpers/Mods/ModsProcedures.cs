using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gml.Core.Extensions;
using Gml.Models.Mods;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;
using GmlCore.Interfaces.Procedures;
using Modrinth.Api;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Models.Projects;

// using Modrinth.Api.Domains.Models.Dto;

namespace Gml.Core.Helpers.Mods;

public class ModsProcedures(IGmlSettings settings) : IModsProcedures
{
    private readonly ModrinthApi _modrinthApi = new(Environment.CurrentDirectory, settings.HttpClient);

    public Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile)
    {
        return profile.GetModsAsync();
    }

    public Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile, string name)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<IMod>> FindModsAsync(
        GameLoader profileLoader,
        string gameVersion,
        string modName,
        short take,
        short offset)
    {
        var searchFilter = new ProjectModFilter
        {
            Query = modName,
            Index = FaceIndexEnum.Downloads,
            Limit = take,
            Offset = offset
        };

        searchFilter.AddFacet(ProjectFilterTypes.Version, gameVersion);
        searchFilter.AddFacet(ProjectFilterTypes.Category, profileLoader.ToModrinthString());

        var mods = await _modrinthApi.Mods.FindAsync<ModProject>(searchFilter, CancellationToken.None)
            .ConfigureAwait(false);

        return mods.Hits.Select(mod => new ModrinthMod
        {
            Name = mod.Title,
            Description = mod.Description,
            FollowsCount = mod.Follows,
            DownloadCount = mod.Downloads,
            IconUrl = mod.IconUrl,
        });
    }
}
