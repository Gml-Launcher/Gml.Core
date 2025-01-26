using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Extensions;
using Gml.Core.Services.Storage;
using Gml.Models.Mods;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;
using GmlCore.Interfaces.Procedures;
using Modrinth.Api;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Models.Projects;

namespace Gml.Core.Helpers.Mods;

public class ModsProcedures(IGmlSettings settings, IStorageService storage, IBugTrackerProcedures bugTracker) : IModsProcedures
{
    private readonly ModrinthApi _modrinthApi = new(Environment.CurrentDirectory, settings.HttpClient);
    private ConcurrentDictionary<string, ModInfo> _modsInfo = [];
    public ICollection<IModInfo> ModsDetails => _modsInfo.Values.OfType<IModInfo>().ToArray();

    public Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile)
    {
        return profile.GetModsAsync();
    }

    public Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile, string name)
    {
        throw new NotImplementedException();
    }

    public async Task<IExternalMod?> GetInfo(string identify)
    {
        var mod = await _modrinthApi.Mods.FindAsync<ModProject>(identify, CancellationToken.None)
            .ConfigureAwait(false);

        return mod is null
            ?  null
            : new ModrinthMod
        {
            Id = mod.Id,
            Name = mod.Title,
            Description = mod.Description,
            FollowsCount = mod.Followers,
            DownloadCount = mod.Downloads,
            IconUrl = mod.IconUrl
        };
    }

    public async Task<IReadOnlyCollection<IModVersion>> GetVersions(IExternalMod modInfo, GameLoader profileLoader,
        string gameVersion)
    {
        var versions = await _modrinthApi.Versions
            .GetVersionsByModId(modInfo.Id, profileLoader.ToModrinthString(), gameVersion, CancellationToken.None)
            .ConfigureAwait(false);

        return versions.Select(version =>  new ModrinthModVersion
        {
            Id = version.Id,
            Name = version.Name,
            VersionName = version.VersionNumber,
            DatePublished = version.DatePublished,
            Downloads = version.Downloads,
            Dependencies = version.Dependencies,
            Files = version.Files.Select(c => c.Url).ToList()
        }).ToArray();
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
            Index = FaceIndexEnum.Relevance,
            Limit = take,
            Offset = offset
        };

        searchFilter.AddFacet(ProjectFilterTypes.Version, gameVersion);
        searchFilter.AddFacet(ProjectFilterTypes.Category, profileLoader.ToModrinthString());

        var mods = await _modrinthApi.Mods.FindAsync<ModProject>(searchFilter, CancellationToken.None)
            .ConfigureAwait(false);

        return mods.Hits.Select(mod => new ModrinthMod
        {
            Id = mod.ProjectId,
            Name = mod.Title,
            Description = mod.Description,
            FollowsCount = mod.Follows,
            DownloadCount = mod.Downloads,
            IconUrl = mod.IconUrl,
        });
    }

    public Task SetModDetails(string modName, string title, string description)
    {
        _modsInfo.AddOrUpdate(modName,
        _ => new ModInfo
        {
            Key = modName,
            Title = title,
            Description = description,
        },
        (_, existing) =>
        {
            existing.Title = title;
            existing.Description = description;
            return existing;
        });

        return storage.SetAsync(StorageConstants.ModsInfo, _modsInfo);
    }

    public async Task Retore()
    {
        try
        {

            _modsInfo = await storage.GetAsync<ConcurrentDictionary<string, ModInfo>>(StorageConstants.ModsInfo) ?? [];

        }
        catch (Exception exception)
        {
            bugTracker.CaptureException(exception);
            _modsInfo = [];
        }
    }
}
