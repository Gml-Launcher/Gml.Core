using CmlLib.Core;
using CmlLib.Core.Rules;
using CmlLib.Core.Version;
using Gml.Core.Helpers.Game;

namespace GmlCore.Tests;

[TestFixture]
public class CmlLibTests
{
    [Test]
    public void ToModrinthString_ForgeLoader_ReturnsForge()
    {
        var minecraftPath = new MinecraftPath();
        var version = new MinecraftVersion("1.7.10");
        var path = minecraftPath.GetNativePath(version.Id);
        var directory = new DirectoryInfo(path);

        if (!directory.Exists)
        {
            directory.Create();

            directory.CreateSubdirectory("test");
            File.WriteAllText(Path.Combine(directory.FullName, "test", "test.txt"), "test");
        }

        var extractor = new CustomNativeLibraryExtractor(new RulesEvaluator());

        extractor.Clean(minecraftPath, version);
        Assert.Multiple(() =>
        {
            Assert.That(directory.Exists, Is.EqualTo(true));
            Assert.That(directory.GetFiles("*.*", SearchOption.AllDirectories).Any(), Is.EqualTo(true));
        });
        directory.Delete(true);
    }
}
