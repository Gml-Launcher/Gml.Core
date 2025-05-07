# Gml.Core

Gml.Core is a core library for the Gml-Launcher project, providing essential functionality for the Minecraft launcher
ecosystem. It is designed to streamline the process of launching Minecraft, managing game versions, profiles, and
server-side operations. This library serves as a foundation for other Gml-Launcher components, such as Gml.Web.Api,
enabling seamless integration and extensibility across the launcher ecosystem.

## Features

- **Cross-Platform Support**: Compatible with Windows, macOS, and Linux.
- **Minecraft Version Management**: Easily manage and switch between different Minecraft versions, including vanilla,
  Forge, NeoForge, Fabric, LiteLoader, and Quilt.
- **Profile Management**: Handle game profiles, including custom configurations and settings.
- **Mod Support**: Seamless integration with popular mod services like CurseForge and Modrinth.
- **Server Management**: Add and manage Minecraft servers for each profile.
- **Authentication**: Support for various authentication services.
- **Extensible Architecture**: Modular design to support additional features and integrations.

## Prerequisites

Before using Gml.Core, ensure you have the following installed:

- **.NET 8.0 SDK**: Required for building and running the library. Download it
  from [Microsoft's official website](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Git**: Required for cloning the repository. Download it from the [Git website](https://git-scm.com/downloads).

## Installation

1. **Clone the Repository**:
   ```bash
   git clone --recursive https://github.com/Gml-Launcher/Gml.Core.git
   cd Gml.Core
   ```

2. **Restore Dependencies**:
   Use the .NET CLI to restore the required NuGet packages:
   ```bash
   dotnet restore
   ```

3. **Build the Project**:
   Compile the library using the .NET CLI:
   ```bash
   dotnet build
   ```

4. **(Optional) Publish the Library**:
   If you want to package the library for distribution:
   ```bash
   dotnet publish -c Release
   ```

## Usage

Gml.Core is designed to be integrated into other projects within the Gml-Launcher ecosystem. Below is a basic example of
how to use the library in a C# project.

### Example: Basic Usage
This example demonstrates basic usage of GmlManager for managing Minecraft profiles, users, servers and mods.

```csharp
// GmlManager initialization
var gmlManager = new GmlManager(new GmlSettings(
    "GmlServer",                 // Launcher name
    "yourSecurityKey",          // Security key
    httpClient: new HttpClient()) 
{
    TextureServiceEndpoint = "http://localhost:8085" // Texture service endpoint
});

// Getting the list of profiles
var profiles = await gmlManager.Profiles.GetProfiles();

// Creating a new profile
var profile = await gmlManager.Profiles.AddProfile(
    "MyProfile",                // Profile name
    "MyProfile",                // Display name
    "1.20.1",                   // Game version
    "47.2.0",                   // Loader version
    GameLoader.Forge,           // Loader type
    string.Empty,               // Icon (optional)
    string.Empty                // Notes (optional)
);

// User authentication
var authServices = await gmlManager.Integrations.GetAuthServices();
await gmlManager.Integrations.SetActiveAuthService(authServices.First());

var user = await gmlManager.Users.GetAuthData(
    "username",                 // Username
    "password",                 // Password
    "Desktop",                  // Platform
    "1.0",                      // Client version
    IPAddress.Parse("127.0.0.1"), // IP address
    null,                       // Credentials (optional)
    null                        // Token (optional)
);

// Adding a server to the profile
var server = await gmlManager.Servers.AddMinecraftServer(
    profile,                    // Profile
    "My Server",                // Server name
    "mc.example.com",           // Server address
    25565                       // Port
);

// Searching for mods
var mods = await gmlManager.Mods.FindModsAsync(
    profile.Loader,             // Loader type
    profile.GameVersion,        // Game version
    ModType.Modrinth,           // Mods source
    "jei",                      // Search query
    10,                         // Number of results
    0                           // Offset
);
```
