using System.IO;
using CmlLib.Core;

namespace Gml.Models.CmlLib;

public class CustomMinecraftPath : MinecraftPath
{
    public CustomMinecraftPath(string rootDirectory, string profilePath, string platform, string architecture)
    {
        BasePath = profilePath;

        Library = NormalizePath(Path.Combine(BasePath, "libraries", platform, architecture));

        Resource = NormalizePath(Path.Combine(BasePath, "resources"));
        Versions = NormalizePath(Path.Combine(BasePath, "client"));
        Runtime = NormalizePath(Path.Combine(rootDirectory, "runtime"));
        Assets = NormalizePath(Path.Combine(rootDirectory, "assets"));

        CreateDirs();
    }
}
