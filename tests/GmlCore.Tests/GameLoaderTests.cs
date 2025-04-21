using NUnit.Framework;
using Gml.Core.Extensions;
using GmlCore.Interfaces.Enums;
using CurseForge.APIClient.Models.Mods;

namespace GmlCore.Tests;

[TestFixture]
public class GameLoaderTests
{
    [Test]
    public void ToModrinthString_ForgeLoader_ReturnsForge()
    {
        Assert.That(GameLoader.Forge.ToModrinthString(), Is.EqualTo("forge"));
    }

    [Test]
    public void ToModrinthString_FabricLoader_ReturnsFabric()
    {
        Assert.That(GameLoader.Fabric.ToModrinthString(), Is.EqualTo("fabric"));
    }

    [Test]
    public void ToModrinthString_LiteLoaderLoader_ReturnsLiteLoader()
    {
        Assert.That(GameLoader.LiteLoader.ToModrinthString(), Is.EqualTo("liteloader"));
    }

    [Test]
    public void ToModrinthString_NeoForgeLoader_ReturnsNeoForge()
    {
        Assert.That(GameLoader.NeoForge.ToModrinthString(), Is.EqualTo("neoforge"));
    }

    [Test]
    public void ToModrinthString_QuiltLoader_ReturnsQuilt()
    {
        Assert.That(GameLoader.Quilt.ToModrinthString(), Is.EqualTo("quilt"));
    }

    [Test]
    public void ToModrinthString_InvalidLoader_ThrowsArgumentOutOfRangeException()
    {
        var invalidLoader = (GameLoader)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidLoader.ToModrinthString());
    }

    [Test]
    public void ToCurseForge_ForgeLoader_ReturnsForge()
    {
        Assert.That(GameLoader.Forge.ToCurseForge(), Is.EqualTo(ModLoaderType.Forge));
    }

    [Test]
    public void ToCurseForge_FabricLoader_ReturnsFabric()
    {
        Assert.That(GameLoader.Fabric.ToCurseForge(), Is.EqualTo(ModLoaderType.Fabric));
    }

    [Test]
    public void ToCurseForge_LiteLoaderLoader_ReturnsLiteLoader()
    {
        Assert.That(GameLoader.LiteLoader.ToCurseForge(), Is.EqualTo(ModLoaderType.LiteLoader));
    }

    [Test]
    public void ToCurseForge_NeoForgeLoader_ReturnsNeoForge()
    {
        Assert.That(GameLoader.NeoForge.ToCurseForge(), Is.EqualTo(ModLoaderType.NeoForge));
    }

    [Test]
    public void ToCurseForge_QuiltLoader_ReturnsQuilt()
    {
        Assert.That(GameLoader.Quilt.ToCurseForge(), Is.EqualTo(ModLoaderType.Quilt));
    }

    [Test]
    public void ToCurseForge_InvalidLoader_ThrowsArgumentOutOfRangeException()
    {
        var invalidLoader = (GameLoader)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidLoader.ToCurseForge());
    }
}
