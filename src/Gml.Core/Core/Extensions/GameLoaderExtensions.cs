using System;
using GmlCore.Interfaces.Enums;

namespace Gml.Core.Extensions;

public static class GameLoaderExtensions
{
    public static string ToModrinthString(this GameLoader loader)
    {
        switch (loader)
        {
            case GameLoader.Forge:
                return "forge";
            case GameLoader.Fabric:
                return "fabric";
            case GameLoader.LiteLoader:
                return "liteloader";
            case GameLoader.NeoForge:
                return "neoforge";
            case GameLoader.Quilt:
                return "quilt";
        }

        throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
    }
}
