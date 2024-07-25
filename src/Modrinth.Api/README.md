Modrinth.Api
=======

## How to use Modrinth.Api in your project

To use the `Modrinth.Api` in your project, follow these steps:

### Step 1: Import the necessary namespaces

```csharp
using Modrinth.Api;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Projects;
```

### Step 2: Create an instance of ModrinthApi

```csharp
namespace YourNamespace
{
    ModrinthApi api = new ModrinthApi();
}
```

## How to Get a List of Projects

To retrieve a list of projects from Modrinth using the `Modrinth.Api`, follow these steps:

### Step 1: Set Up Project Filter

```csharp

var projectFilter = new ProjectFilter()
{
    Query = "your_search_query",
    Limit = 50, // Set the number of projects to retrieve
    Index = FaceIndexEnum.Follows,
    Offset = 0
};

projectFilter.AddFacet(ProjectFilterTypes.Category, "forge", LogicalOperator.Or);
projectFilter.AddFacet(ProjectFilterTypes.Version, "1.16.5", LogicalOperator.Or);

```

### Step 2: Retrieve Projects List

```csharp
var projects = await _api.Projects.FindAsync<SearchProjectResultDto>(projectFilter, CancellationToken.None);
```
This code snippet demonstrates how to set up a project filter and retrieve a list of projects based on your search criteria. Adjust the filter parameters according to your specific requirements.

## How to Get a List of Mods and Download a Version with Dependencies

To interact with Modrinth and retrieve a list of mods while also downloading a specific version with all dependencies, follow these steps:

### Step 1: Set Up Mod Filter

Before fetching the list of mods or downloading a version, you can set up a filter to narrow down the results. For example, you can filter mods by name, category, version, etc.

```csharp
var modFilter = new ProjectModFilter()
{
    Query = "your_search_query",
    Limit = 50, // Set the number of mods to retrieve
    Index = FaceIndexEnum.Follows,
    Offset = 0
};

// Add facets to filter mods further (optional)
modFilter.AddFacet(ProjectFilterTypes.Category, "forge", LogicalOperator.Or);
modFilter.AddFacet(ProjectFilterTypes.Version, "1.16.5", LogicalOperator.Or);

```

### Step 2: Download a Mod Version with Dependencies

```csharp
// Retrieve the ModProject instance for the mod
var modProject = await _api.Mods.FindAsync<ModProject?>(ProjectId, CancellationToken.None);

// Retrieve all versions for the mod
var versions = await modProject?.GetVersionsAsync(CancellationToken.None)!;

// Assuming you want to download the latest version
var latestVersion = versions.MaxBy(c => c.DatePublished);

if (latestVersion != null)
{
    // Set up the folder where you want to download the mod
    var folder = Path.Combine(Environment.CurrentDirectory, "mods");

    // Download the mod with all dependencies
    await _api.Mods.DownloadAsync(folder, latestVersion, true, CancellationToken.None);
}
```

This code snippet demonstrates how to set up a mod filter, retrieve a list of mods, and download a specific version with all dependencies. Customize the search query, filter options, and version selection according to your project's requirements.
