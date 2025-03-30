using System;
using CurseForge.APIClient.Models.Mods;
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


    public static ModLoaderType ToCurseForge(this GameLoader loader)
    {
        switch (loader)
        {
            case GameLoader.Forge:
                return ModLoaderType.Forge;
            case GameLoader.Fabric:
                return ModLoaderType.Fabric;
            case GameLoader.LiteLoader:
                return ModLoaderType.LiteLoader;
            case GameLoader.NeoForge:
                return ModLoaderType.NeoForge;
            case GameLoader.Quilt:
                return ModLoaderType.Quilt;
        }

        throw new ArgumentOutOfRangeException(nameof(loader), loader, null);
    }
}
