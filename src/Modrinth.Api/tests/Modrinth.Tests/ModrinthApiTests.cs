using Modrinth.Api;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Projects;

namespace Modrinth.Tests;

public class Tests
{
    private ModrinthApi _api;
    private SearchProjectResultDto _projects;
    private SearchProjectResultDto _mods;

    [SetUp]
    public void Setup()
    {
        var modsPath = Path.Combine(Environment.CurrentDirectory, "mods");

        _api = new ModrinthApi(modsPath, new HttpClient());
    }

    [Test, Order(1)]
    public void InitApiTest()
    {
        Assert.That(_api, Is.Not.Null);
    }

    [Test, Order(2)]
    public async Task InitProjectsListTest()
    {
        var projectFilter = new ProjectModFilter()
        {
            Query = "app",
            Limit = 50,
            Index = FaceIndexEnum.Follows,
            Offset = 0
        };

        projectFilter.AddFacet(ProjectFilterTypes.Category, "forge", LogicalOperator.Or);
        projectFilter.AddFacet(ProjectFilterTypes.Category, "neoforge", LogicalOperator.Or);

        projectFilter.AddFacet(ProjectFilterTypes.Version, "1.20.1", LogicalOperator.Or);
        projectFilter.AddFacet(ProjectFilterTypes.Version, "1.12.2", LogicalOperator.Or);
        projectFilter.AddFacet(ProjectFilterTypes.Version, "1.7.10", LogicalOperator.Or);

        _projects = await _api.Projects.FindAsync<SearchProjectResultDto>(projectFilter, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_projects, Is.Not.Null);
            Assert.That(_projects!.TotalHits, Is.Not.EqualTo(0));
            Assert.That(_projects!.Hits, Is.Not.Empty);
        });
    }

    [Test, Order(3)]
    public async Task FindProjectByIdTest()
    {
        var searchedProject = _projects!.Hits.First();

        var project = await _api.Projects.FindAsync<Project>(searchedProject.ProjectId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(project, Is.Not.Null);
            Assert.That(project!.ProjectTypeEnum, Is.EqualTo(ProjectType.Mod));
            Assert.That(project.GetType(), Is.EqualTo(typeof(Project)));
            Assert.That(searchedProject.ProjectId, Is.EqualTo(project!.Id));
        });
    }

    [Test, Order(4)]
    public async Task FindProjectBySlugTest()
    {
        var searchedProject = _projects!.Hits.First();

        var project = await _api.Projects.FindAsync<Project>(searchedProject.Slug, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(project, Is.Not.Null);
            Assert.That(project!.ProjectTypeEnum, Is.EqualTo(ProjectType.Mod));
            Assert.That(project.GetType(), Is.EqualTo(typeof(Project)));
            Assert.That(searchedProject.ProjectId, Is.EqualTo(project!.Id));
        });
    }

    [Test, Order(5)]
    public async Task GetModsListTest()
    {

        var projectFilter = new ProjectModFilter()
        {
            Query = "mod",
            Index = FaceIndexEnum.Follows,
            Limit = 50,
            Offset = 0
        };

        projectFilter.AddFacet(ProjectFilterTypes.Category, "forge", LogicalOperator.Or);
        projectFilter.AddFacet(ProjectFilterTypes.Category, "neoforge", LogicalOperator.Or);

        projectFilter.AddFacet(ProjectFilterTypes.Version, "1.20.1", LogicalOperator.Or);
        projectFilter.AddFacet(ProjectFilterTypes.Version, "1.12.2", LogicalOperator.Or);
        projectFilter.AddFacet(ProjectFilterTypes.Version, "1.7.10", LogicalOperator.Or);

        _mods = await _api.Mods.FindAsync<SearchProjectResultDto>(projectFilter, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_projects, Is.Not.Null);
            Assert.That(_projects!.TotalHits, Is.Not.EqualTo(0));
            Assert.That(_projects!.Hits.Count, Is.EqualTo(_projects.Hits.Count(c => c.ProjectTypeEnum == ProjectType.Mod)));
        });
    }

    [Test, Order(6)]
    public async Task FindModByIdTest()
    {

        var projectFilter = new ProjectModFilter()
        {
            Query = "mod",
            Index = FaceIndexEnum.Follows,
            Limit = 50,
            Offset = 0
        };

        _mods = await _api.Mods.FindAsync<SearchProjectResultDto>(projectFilter, CancellationToken.None);

        var searchedMod = _mods!.Hits.First();

        var mod = await _api.Mods.FindAsync<ModProject>(searchedMod.ProjectId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(mod, Is.Not.Null);
            Assert.That(mod!.ProjectTypeEnum, Is.EqualTo(ProjectType.Mod));
            Assert.That(mod.Id, Is.EqualTo(searchedMod.ProjectId));
            Assert.That(mod.GetType(), Is.EqualTo(typeof(ModProject)));
        });

    }

    [Test, Order(7)]
    public async Task GetModVersionsTest()
    {
        _mods = await _api.Mods.FindAsync<SearchProjectResultDto>(new ProjectModFilter(), CancellationToken.None);

        var searchedMod = _mods!.Hits.First();

        var mod = await _api.Mods.FindAsync<ModProject>(searchedMod.ProjectId, CancellationToken.None);

        var versions = await mod?.GetVersionsAsync(CancellationToken.None)!;

        Assert.Multiple(() =>
        {
            Assert.That(mod, Is.Not.Null);
            Assert.That(mod.GetType(), Is.EqualTo(typeof(ModProject)));
            Assert.That(versions, Is.Not.Null);
            Assert.That(versions!.Count(), Is.Not.EqualTo(0));
        });
    }

    [Test, Order(8)]
    public async Task DownloadModTest()
    {
        var modFilter = new ProjectModFilter
        {
            Query = "BuildCraft Compat",
            Limit = 1,
            Offset = 0
        };

        modFilter.AddFacet(ProjectFilterTypes.Version, "1.7.10");

        _mods = await _api.Mods.FindAsync<SearchProjectResultDto>(modFilter, CancellationToken.None);

        var searchedMod = _mods.Hits.First();

        var modProject = await _api.Mods.FindAsync<ModProject?>(searchedMod.ProjectId, CancellationToken.None);

        var enumerable = await modProject?.GetVersionsAsync(CancellationToken.None)!;

        var actual = enumerable.ToList();

        var lastVersion = actual.MaxBy(c => c.DatePublished);

        if (lastVersion != null)
        {
            await _api.Mods.DownloadAsync(lastVersion, CancellationToken.None);
        }

        Assert.Multiple(() =>
        {
            Assert.That(lastVersion, Is.Not.Null);
        });
    }

    [Test, Order(99)]
    public void End()
    {
        Console.WriteLine($"{_api.Settings.RateLimit.Remaining} / {_api.Settings.RateLimit.Limit}, updete: {_api.Settings.RateLimit.Reset} sec.");
    }
}
