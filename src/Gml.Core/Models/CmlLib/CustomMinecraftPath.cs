using System.IO;
using CmlLib.Core;

namespace Gml.Models.CmlLib
{
    public class CustomMinecraftPath : MinecraftPath
    {
        public CustomMinecraftPath(string basePath)
        {
            BasePath = NormalizePath(basePath);

            Library = NormalizePath(Path.Combine(BasePath, "libraries"));
            Versions = NormalizePath(Path.Combine(BasePath, "client"));
            Resource = NormalizePath(Path.Combine(BasePath, "resources"));

            Runtime = NormalizePath(Path.Combine(BasePath, "runtime"));
            Assets = NormalizePath(Path.Combine(BasePath, "assets"));

            CreateDirs();
        }
    }
}
