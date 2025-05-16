![Frame 39267](https://github.com/user-attachments/assets/dea7368e-c80c-4399-a4cf-94791b9e067a)

# Modrinth.Api

A robust and easy-to-use C# library for interacting with the Modrinth API, enabling developers to search, filter, and download mods and projects effortlessly.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
    - [Initializing the API](#initializing-the-api)
    - [Searching for Projects](#searching-for-projects)
    - [Retrieving and Downloading Mods](#retrieving-and-downloading-mods)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Features
- **Search and Filter**: Query Modrinth projects and mods with customizable filters (e.g., categories, versions, or search terms).
- **Download Mods**: Download specific mod versions along with their dependencies.
- **Asynchronous Operations**: Fully async/await compatible for efficient API calls.
- **Type-Safe Models**: Strongly-typed DTOs for seamless integration with Modrinth's API responses.

## Installation
1. Add the `Modrinth.Api` NuGet package to your project:
   ```bash
   dotnet add package Modrinth.Api
   ```
2. Ensure your project targets .NET Framework or .NET Core compatible with the library.

## Usage

### Initializing the API
To start using the `Modrinth.Api`, import the required namespaces and create an instance of `ModrinthApi`.

```csharp
using Modrinth.Api;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Projects;

namespace YourNamespace
{
    public class Program
    {
        private readonly ModrinthApi _api = new ModrinthApi();
    }
}
```

### Searching for Projects
Retrieve a list of projects by applying filters such as search queries, categories, or Minecraft versions.

```csharp
var projectFilter = new ProjectFilter
{
    Query = "fabric",
    Limit = 50,
    Index = FaceIndexEnum.Follows,
    Offset = 0
};

// Add facets for finer control
projectFilter.AddFacet(ProjectFilterTypes.Category, "forge", LogicalOperator.Or);
projectFilter.AddFacet(ProjectFilterTypes.Version, "1.20.1", LogicalOperator.Or);

// Fetch projects
var projects = await _api.Projects.FindAsync<SearchProjectResultDto>(projectFilter, CancellationToken.None);
foreach (var project in projects.Hits)
{
    Console.WriteLine($"Project: {project.Title} (ID: {project.ProjectId})");
}
```

### Retrieving and Downloading Mods
Search for mods and download specific versions, including their dependencies, to a designated folder.

```csharp
var modFilter = new ProjectModFilter
{
    Query = "sodium",
    Limit = 50,
    Index = FaceIndexEnum.Follows,
    Offset = 0
};

// Add facets to narrow down results
modFilter.AddFacet(ProjectFilterTypes.Category, "optimization", LogicalOperator.Or);
modFilter.AddFacet(ProjectFilterTypes.Version, "1.20.1", LogicalOperator.Or);

// Retrieve mod project
var modProject = await _api.Mods.FindAsync<ModProject>("sodium", CancellationToken.None);
if (modProject == null) return;

// Get all versions
var versions = await modProject.GetVersionsAsync(CancellationToken.None);
var latestVersion = versions.MaxBy(v => v.DatePublished);

if (latestVersion != null)
{
    var downloadFolder = Path.Combine(Environment.CurrentDirectory, "mods");
    await _api.Mods.DownloadAsync(downloadFolder, latestVersion, true, CancellationToken.None);
    Console.WriteLine($"Downloaded {latestVersion.Name} to {downloadFolder}");
}
```

## Examples
Here’s a complete example combining project search and mod download:

```csharp
using Modrinth.Api;
using Modrinth.Api.Core.Filter;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Models.Dto;

namespace ModrinthExample
{
    public class Program
    {
        public static async Task Main()
        {
            var api = new ModrinthApi();
            
            // Search for projects
            var filter = new ProjectFilter
            {
                Query = "adventure",
                Limit = 10
            };
            var projects = await api.Projects.FindAsync<SearchProjectResultDto>(filter, CancellationToken.None);
            Console.WriteLine($"Found {projects.Hits.Count} projects.");

            // Download a mod
            var mod = await api.Mods.FindAsync<ModProject>("fabric-api", CancellationToken.None);
            if (mod != null)
            {
                var versions = await mod.GetVersionsAsync(CancellationToken.None);
                var latest = versions.MaxBy(v => v.DatePublished);
                if (latest != null)
                {
                    await api.Mods.DownloadAsync("mods", latest, true, CancellationToken.None);
                    Console.WriteLine("Downloaded Fabric API.");
                }
}
        }
    }
}
```

## Contributing
Contributions are welcome! To contribute:
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add YourFeature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a Pull Request.

Please ensure your code follows the project's coding standards and includes appropriate tests.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
