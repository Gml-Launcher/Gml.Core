using System.Diagnostics;
using System.Runtime.InteropServices;
using CmlLib.Core;
using CmlLib.Core.Java;
using CmlLib.Core.Rules;
using Gml;
using Gml.Core.Helpers.Game;
using Gml.Core.Launcher;
using Gml.Models.User;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;

namespace GmlCore.Tests;

public class JavaTests
{
    private const string LauncherName = "GmlServer";
    private const string SecurityKey = "gfweagertghuysergfbsuyerbgiuyserg";
    private AzulJavaFileExtractor _fileExtractor;

    private IGmlManager _gmlManager;
    private AzulMinecraftJavaManifestResolver _manifest;

    [OneTimeSetUp]
    public async Task SetupOnce()
    {
        var client = new HttpClient();
        _gmlManager = new GmlManager(new GmlSettings(LauncherName, SecurityKey, httpClient: client)
        {
            TextureServiceEndpoint = "http://gml-web-skins:8085"
        });


        _fileExtractor = new AzulJavaFileExtractor(client, new MinecraftJavaPathResolver(new MinecraftPath()));
        _manifest = new AzulMinecraftJavaManifestResolver(client);
    }

    [Test]
    public async Task CheckEndpointUpdate()
    {
        var minecraftVersion = "1.20.1";

        foreach (var profile in await _gmlManager.Profiles.GetProfiles())
        {
            await profile.Remove();
        }

        var service = await _gmlManager.Profiles.GetAllowVersions(GameLoader.Forge, minecraftVersion);
        var needVersion = service.First();

        var newProfile = await _gmlManager.Profiles.AddProfile(
            "Test",
            "Test",
            minecraftVersion,
            needVersion,
            GameLoader.Forge,
            string.Empty,
            string.Empty) ?? throw new Exception("Failed to create profile instance");

        await _gmlManager.Profiles.UpdateProfile(
            newProfile,
            newProfile.Name,
            newProfile.DisplayName,
            null, null,
            newProfile.Description,
            newProfile.IsEnabled,
            newProfile.JvmArguments,
            newProfile.GameArguments,
            newProfile.Priority,
            newProfile.RecommendedRam,
            false,
            ProfileJavaVendor.Azul,
            "11"
        );

        await newProfile.GameLoader.DownloadGame(newProfile.GameVersion, newProfile.LaunchVersion,
            newProfile.Loader, null);

        await _gmlManager.Profiles.PackProfile(newProfile);

        if (await _gmlManager.Profiles.GetProfileInfo(newProfile.Name, new StartupOptions
            {
                OsName = "osx",
                OsArch = nameof(Architecture.Arm64).ToLower()
            }, new AuthUser
            {
                AccessToken = "111",
                Name = "GamerVII"
            }) is not GameProfileInfo data)
        {
            throw new Exception("Failed to create profile instance");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = data.Arguments,
                FileName = data.JavaPath
            }
        };

        Debug.WriteLine($"\"{process.StartInfo.FileName}\" {process.StartInfo.Arguments}");

        Assert.That(newProfile, Is.Not.Null);
    }

    [Test]
    public async Task CheckExistsUpdate()
    {
        var minecraftVersion = "1.20.1";

        _gmlManager.RestoreSettings<LauncherVersion>();

        var newProfile = await _gmlManager.Profiles.GetProfile("Test") ??
                         throw new Exception("Failed to create profile instance");

        // await _gmlManager.Profiles.PackProfile(newProfile);

        if (await _gmlManager.Profiles.GetProfileInfo(newProfile.Name, new StartupOptions
            {
                OsName = "osx",
                OsArch = nameof(Architecture.Arm64).ToLower(),
                MinimumRamMb = 1024,
                MaximumRamMb = 8096
            }, new AuthUser
            {
                AccessToken = "111",
                Name = "GamerVII",
                Uuid = Guid.NewGuid().ToString().ToLower()
            }) is not GameProfileInfo data)
        {
            throw new Exception("Failed to create profile instance");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = data.Arguments
                    .Replace("{localPath}", _gmlManager.LauncherInfo.InstallationDirectory)
                    .Replace("{authEndpoint}", "gmlf.recloud.tech/api/v1/integrations/authlib/minecraft"),
                FileName = data.JavaPath.Replace("{localPath}", _gmlManager.LauncherInfo.InstallationDirectory)
            }
        };

        Debug.WriteLine($"\"{process.StartInfo.FileName}\" {process.StartInfo.Arguments}");

        Assert.That(newProfile, Is.Not.Null);
    }

    [Test]
    public async Task CheckJavaForCurrentOs()
    {
        var os = AzulMinecraftJavaManifestResolver.GetOSNameForJava(LauncherOSRule.Current);
        var osArch = AzulMinecraftJavaManifestResolver.GetOSArchForJava(LauncherOSRule.Current);
        var versions = await _manifest.GetManifestsForOS(os, osArch);

        Assert.That(versions, Is.Not.Empty);
    }

    [Test]
    public async Task CheckJavaForAllOs()
    {
        var osList = new[]
        {
            new LauncherOSRule(LauncherOSRule.Windows, LauncherOSRule.X86, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.Windows, LauncherOSRule.X64, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.Windows, LauncherOSRule.Arm, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.Windows, LauncherOSRule.Arm64, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.Linux, LauncherOSRule.X86, "5.0.0"),
            new LauncherOSRule(LauncherOSRule.Linux, LauncherOSRule.X64, "5.0.0"),
            new LauncherOSRule(LauncherOSRule.Linux, LauncherOSRule.Arm, "5.0.0"),
            new LauncherOSRule(LauncherOSRule.Linux, LauncherOSRule.Arm64, "5.0.0"),
            new LauncherOSRule(LauncherOSRule.OSX, LauncherOSRule.X86, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.OSX, LauncherOSRule.X64, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.OSX, LauncherOSRule.Arm, "10.0.0"),
            new LauncherOSRule(LauncherOSRule.OSX, LauncherOSRule.Arm64, "10.0.0")
        };

        foreach (var osRule in osList)
        {
            var os = AzulMinecraftJavaManifestResolver.GetOSNameForJava(osRule);
            var osArch = AzulMinecraftJavaManifestResolver.GetOSArchForJava(osRule);
            var versions = await _manifest.GetManifestsForOS(os, osArch);
            Assert.That(versions, Is.Not.Empty);
        }
    }
}
