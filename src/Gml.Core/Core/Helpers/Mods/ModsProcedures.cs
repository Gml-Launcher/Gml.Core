using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CurseForge.APIClient;
using Gml.Core.Constants;
using Gml.Core.Extensions;
using Gml.Core.Launcher;
using Gml.Models.Mods;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;
using Modrinth;

namespace Gml.Core.Helpers.Mods;

public class ModsProcedures : IModsProcedures
{
    private readonly IBugTrackerProcedures _bugTracker;
    private readonly int _curseForgeGameId = 432;
    private readonly ModrinthClient _modrinthClient;
    private readonly IStorageService _storage;
    private ApiClient? _curseForgeApi;
    private ConcurrentDictionary<string, ModInfo> _modsInfo = [];

    public ModsProcedures(ILauncherInfo launcherInfo,
        IGmlSettings settings,
        IStorageService storage,
        IBugTrackerProcedures bugTracker)
    {
        _storage = storage;
        _bugTracker = bugTracker;
        _modrinthClient = new ModrinthClient();

        UpdateCurseForgeToken(launcherInfo.AccessTokens);
        launcherInfo.SettingsUpdated.Subscribe(newSettings => { UpdateCurseForgeToken(launcherInfo.AccessTokens); });
    }

    public ICollection<IModInfo> ModsDetails => _modsInfo.Values.OfType<IModInfo>().ToArray();

    public Task<IReadOnlyCollection<IMod>> GetModsAsync(IGameProfile profile)
    {
        return profile.GetModsAsync();
    }

    // TODO: выбор откуда брать данные CurseForge или Modrinth
    public Task<IEnumerable<IMod>> GetModsAsync(IGameProfile profile, string name)
    {
        throw new NotImplementedException();
    }

    public async Task<IExternalMod?> GetInfo(string identify, ModType modType)
    {
        switch (modType)
        {
            case ModType.Modrinth:
                return await GetInfoModByModrinth(identify);
            case ModType.CurseForge:
                return await GetInfoModByCurseForge(identify);
            case ModType.Local:
            default:
                throw new ArgumentOutOfRangeException(nameof(modType), modType, null);
        }
    }


    public async Task<IReadOnlyCollection<IModVersion>> GetVersions(IExternalMod modInfo,
        ModType modType,
        GameLoader profileLoader,
        string gameVersion)
    {
        switch (modType)
        {
            case ModType.Modrinth:
                return await GetModrinthModVersions(modInfo, profileLoader, gameVersion);
            case ModType.CurseForge:
                return await GetCurseForgeModVersions(modInfo, profileLoader, gameVersion);
            case ModType.Local:
            default:
                throw new ArgumentOutOfRangeException(nameof(modType), modType, null);
        }
    }

    // TODO: нужно сделать реализацию поиска модов по выбору где искать CurseForge или Modrinth
    public Task<IReadOnlyCollection<IExternalMod>> FindModsAsync(GameLoader profileLoader,
        string gameVersion,
        ModType modLoaderType,
        string modName,
        short take,
        short offset)
    {
        switch (modLoaderType)
        {
            case ModType.Modrinth:
                return FindByModrinthMods(profileLoader, gameVersion, modName, take, offset);
            case ModType.CurseForge:
                return FindByCurseForgeMods(profileLoader, gameVersion, modName, take, offset);
            case ModType.Local:
            default:
                throw new ArgumentOutOfRangeException(nameof(modLoaderType), modLoaderType, null);
        }
    }

    // TODO: нужно версию так же выбирать откуда брать данные CurseForge или Modrinth
    public Task SetModDetails(string modName, string title, string description)
    {
        _modsInfo.AddOrUpdate(modName,
            _ => new ModInfo
            {
                Key = modName,
                Title = title,
                Description = description
            },
            (_, existing) =>
            {
                existing.Title = title;
                existing.Description = description;
                return existing;
            });

        return _storage.SetAsync(StorageConstants.ModsInfo, _modsInfo);
    }

    public async Task Retore()
    {
        try
        {
            _modsInfo = await _storage.GetAsync<ConcurrentDictionary<string, ModInfo>>(StorageConstants.ModsInfo) ?? [];
        }
        catch (Exception exception)
        {
            _bugTracker.CaptureException(exception);
            _modsInfo = [];
        }
    }

    private void UpdateCurseForgeToken(IDictionary<string, string> tokens)
    {
        if (tokens.TryGetValue(AccessTokenTokens.CurseForgeKey, out var token) && !string.IsNullOrEmpty(token))
            _curseForgeApi = new ApiClient(token);
    }

    private async Task<IExternalMod?> GetInfoModByModrinth(string identify)
    {
        var mod = await _modrinthClient.Project
            .GetAsync(identify, CancellationToken.None)
            .ConfigureAwait(false);

        return mod is null
            ? null
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

    private async Task<IExternalMod?> GetInfoModByCurseForge(string identify)
    {
        if (_curseForgeApi is null) return null;

        var mod = await _curseForgeApi
            .GetModAsync(int.Parse(identify))
            .ConfigureAwait(false);

        return mod is null
            ? null
            : new CurseForgeMod
            {
                Id = mod.Data.Id.ToString(),
                Name = mod.Data.Name,
                Description = mod.Data.Summary,
                FollowsCount = Convert.ToInt32(mod.Data.Rating),
                DownloadCount = Convert.ToInt32(mod.Data.DownloadCount),
                IconUrl = mod.Data.Logo.Url
            };
    }

    private async Task<IReadOnlyCollection<IModVersion>> GetModrinthModVersions(IExternalMod modInfo,
        GameLoader profileLoader, string gameVersion)
    {
        var versions = await _modrinthClient.Version.GetProjectVersionListAsync(
                modInfo.Id, [profileLoader.ToModrinthString()], [gameVersion]
            )
            .ConfigureAwait(false);

        return versions.Select(version => new ModrinthModVersion
        {
            Id = version.Id,
            Name = version.Name,
            VersionName = version.VersionNumber,
            DatePublished = version.DatePublished,
            Downloads = version.Downloads,
            Dependencies = version.Dependencies,
            Files = version.Files.Where(c => !c.FileName.StartsWith("sources_")).Select(c => c.Url).ToList()
        }).ToArray();
    }

    private async Task<IReadOnlyCollection<IModVersion>> GetCurseForgeModVersions(IExternalMod modInfo,
        GameLoader profileLoader, string gameVersion)
    {
        if (_curseForgeApi is null) return [];

        var versions = await _curseForgeApi.GetModFilesAsync(
                int.Parse(modInfo.Id),
                gameVersion,
                profileLoader.ToCurseForge()
            )
            .ConfigureAwait(false);
        return versions.Data.Select(version => new CurseForgeModVersion
        {
            Id = version.Id.ToString(),
            Name = version.FileName,
            VersionName = version.FileName,
            DatePublished = version.FileDate,
            Files = [version.DownloadUrl]
        }).ToArray();
    }

    // Работа с Modrinth
    private async Task<IReadOnlyCollection<IExternalMod>> FindByModrinthMods(GameLoader profileLoader,
        string gameVersion, string modName, short take,
        short offset)
    {
        var facets = new FacetCollection();
        facets.Add(Facet.Category(profileLoader.ToModrinthString()));
        facets.Add(Facet.Version(gameVersion));

        var search = await _modrinthClient.Project.SearchAsync(modName, offset: offset, limit: take, facets: facets);

        return search.Hits.Select(mod => new ModrinthMod
        {
            Id = mod.ProjectId,
            Name = mod.Title,
            Description = mod.Description,
            FollowsCount = mod.Followers,
            DownloadCount = mod.Downloads,
            IconUrl = mod.IconUrl
        }).ToArray();
    }

    // Работа с CurseForge
    private async Task<IReadOnlyCollection<IExternalMod>> FindByCurseForgeMods(GameLoader profileLoader,
        string gameVersion, string modName, short take,
        short offset)
    {
        if (_curseForgeApi is null) return [];

        var mods = await _curseForgeApi.SearchModsAsync(
                _curseForgeGameId,
                gameVersion: gameVersion,
                modLoaderType: profileLoader.ToCurseForge(),
                searchFilter: modName,
                pageSize: take,
                index: offset
            )
            .ConfigureAwait(false);

        if (mods?.Data is null || mods.Data.Count == 0) return [];

        return mods.Data.Select(mod => new CurseForgeMod
        {
            Id = mod.Id.ToString(),
            Name = mod.Name,
            Description = mod.Summary,
            FollowsCount = Convert.ToInt32(mod.Rating),
            DownloadCount = Convert.ToInt32(mod.DownloadCount),
            IconUrl = mod.Logo.Url
        }).ToArray();
    }
}
