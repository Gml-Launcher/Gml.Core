using Gml;
using Gml.Core.Launcher;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Storage;

namespace GmlCore.Tests;

public class RestoreTest
{

    private const string LauncherName = "GamerVIILauncher";
    private const string SecurityKey = "gfweagertghuysergfbsuyerbgiuyserg";

    private IGmlManager _gmlManager;

    [SetUp]
    public async Task SetupOnce()
    {
        _gmlManager = new GmlManager(new GmlSettings(LauncherName, SecurityKey, httpClient: new HttpClient())
        {
            TextureServiceEndpoint = "http://gml-web-skins:8085"
        });
    }

    [Test]
    public async Task CheckRestore()
    {
        // Arrange
        _gmlManager.RestoreSettings<LauncherVersion>();
    }
}

public struct LauncherVersion : IVersionFile
{
    public string Version { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Guid { get; set; }
    public object Clone()
    {
        return new LauncherVersion
        {
            Version = Version,
            Title = Title,
            Description = Description,
            Guid = Guid,
        };
    }
}
