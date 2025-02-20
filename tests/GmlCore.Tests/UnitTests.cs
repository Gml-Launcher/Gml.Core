using System.Diagnostics;
using System.Net.Sockets;
using Gml;
using Gml.Core.Integrations;
using Gml.Core.Launcher;
using Gml.Models.Mods;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Mods;
using Pingo;
using Pingo.Status;

namespace GmlCore.Tests;

public class Tests
{
    private const string ServerName = "Hitech #1";
    private IGameProfile _testGameProfile = null!;

    private const string CheckProfileName = "TestProfile1710";
    private const string CheckMinecraftVersion = "1.7.10";
    private const string CheckLaunchVersion = "10.13.4.1614";
    private const GameLoader CheckLoader = GameLoader.Forge;

    private GmlManager GmlManager { get; } =
        new(new GmlSettings("GamerVIILauncher", "gfweagertghuysergfbsuyerbgiuyserg", httpClient: new HttpClient())
        {
            TextureServiceEndpoint = "http://gml-web-skins:8085"
        });

    [OneTimeSetUp]
    public async Task Setup()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile(CheckProfileName)
                           ?? await GmlManager.Profiles.AddProfile(CheckProfileName, CheckProfileName, CheckMinecraftVersion,
                               CheckLaunchVersion,
                               CheckLoader,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");
    }

    [Test]
    [Order(0)]
    public void InitializeTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GmlManager.LauncherInfo.InstallationDirectory, Is.Not.Empty);
            Assert.That(GmlManager.LauncherInfo.BaseDirectory, Is.Not.Empty);
            Assert.That(GmlManager.LauncherInfo.Name, Is.Not.Empty);
        });
    }

    [Test]
    [Order(1)]
    public async Task Create_LiteLoader_Profile()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.LiteLoader)}";

        _testGameProfile = await GmlManager.Profiles.GetProfile(name)
                           ?? await GmlManager.Profiles.AddProfile(name, CheckProfileName, "1.7.10", "1.7.10_04",
                               GameLoader.LiteLoader,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        Assert.Multiple(() =>
        {
            Assert.That(_testGameProfile, Is.Not.Null);
            Assert.That(_testGameProfile.GameVersion, Is.Not.Empty);
        });
    }

    [Test]
    [Order(2)]
    public async Task Create_Vanilla_Profile()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Vanilla)}";

        _testGameProfile = await GmlManager.Profiles.GetProfile(name)
                           ?? await GmlManager.Profiles.AddProfile(name, CheckProfileName, "1.20.1", string.Empty, GameLoader.Vanilla,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        Assert.Multiple(async () =>
        {
            Assert.That(
                await GmlManager.Profiles.CanAddProfile(name, "1.20.1", string.Empty, GameLoader.Vanilla),
                Is.False);
        });
    }

    [Test]
    [Order(2)]
    public async Task Create_Forge_Profile()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Forge)}";

        _testGameProfile = await GmlManager.Profiles.GetProfile(name)
                           ?? await GmlManager.Profiles.AddProfile(name, CheckProfileName, "1.7.10", "10.13.4.1614", GameLoader.Forge,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        Assert.Multiple(async () =>
        {
            Assert.That(
                await GmlManager.Profiles.CanAddProfile(name, "1.7.10", string.Empty, GameLoader.Forge),
                Is.False);
        });
    }

    [Test]
    [Order(2)]
    public async Task Create_NeoForge_Profile()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.NeoForge)}";

        _testGameProfile = await GmlManager.Profiles.GetProfile(name)
                           ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.4", "neoforge-20.4.237", GameLoader.NeoForge,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        Assert.Multiple(async () =>
        {
            Assert.That(
                await GmlManager.Profiles.CanAddProfile(name, "1.7.10", string.Empty, GameLoader.NeoForge),
                Is.False);
        });
    }

    [Test]
    [Order(2)]
    public async Task Create_Fabric_Profile()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Fabric)}";

        _testGameProfile = await GmlManager.Profiles.GetProfile(name)
                           ?? await GmlManager.Profiles.AddProfile(name, CheckProfileName, "1.20.1", "0.16.0", GameLoader.Fabric,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        Assert.Multiple(async () =>
        {
            Assert.That(
                await GmlManager.Profiles.CanAddProfile(name, "1.7.10", string.Empty, GameLoader.Fabric),
                Is.False);
        });
    }

    [Test]
    [Order(3)]
    public Task ValidateProfile()
    {
        Assert.Multiple(async () => { Assert.That(await _testGameProfile.ValidateProfile(), Is.True); });
        return Task.CompletedTask;
    }

    [Test]
    [Order(4)]
    public async Task CreateServer()
    {

        var server = await GmlManager.Servers.AddMinecraftServer(_testGameProfile, ServerName, "127.0.0.1", 25565);

        Assert.That(server, Is.Not.Null);
    }

    [Test]
    [Order(5)]
    public async Task GetOnline()
    {

        var server = _testGameProfile.Servers.First(c => c.Name == ServerName);

        try
        {
            await server.UpdateStatusAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Assert.That(server, Is.Not.Null);
    }

    [Test]
    [Order(6)]
    public async Task RemoveServer()
    {
        await GmlManager.Servers.RemoveServer(_testGameProfile, ServerName);
    }

    [Test]
    [Order(6)]
    public async Task Get_All_Minecraft_Versions()
    {
        var versions = await GmlManager.Profiles.GetAllowVersions(GameLoader.Vanilla, string.Empty);

        Assert.That(versions.Count(), Is.Not.Zero);
    }

    [Test]
    [Order(7)]
    public async Task Get_Forge_1_7_10_Versions()
    {
        var versions = await GmlManager.Profiles.GetAllowVersions(GameLoader.Forge, "1.7.10");

        Assert.That(versions.Count(), Is.Not.Zero);
    }

    [Test]
    [Order(8)]
    public async Task Get_Fabric_1_20_1_Versions()
    {
        var versions = await GmlManager.Profiles.GetAllowVersions(GameLoader.Fabric, "1.20.1");

        Assert.That(versions.Count(), Is.Not.Zero);
    }

    [Test]
    [Order(7)]
    public async Task Get_LiteLoader_1_7_10_Versions()
    {
        var versions = await GmlManager.Profiles.GetAllowVersions(GameLoader.LiteLoader, "1.7.10");

        Assert.That(versions.Count(), Is.Not.Zero);
    }

    [Test]
    [Order(900)]
    public async Task Remove_Profile()
    {
        var name = _testGameProfile.Name;

        await _testGameProfile.Remove();

        var checkProfile = await GmlManager.Profiles.GetProfile(name);

        Assert.That(checkProfile, Is.Null);
    }

    [Test]
    [Order(41)]
    public async Task ServerPing1_20_6()
    {
        try
        {
            // 1.20.6
            var options = new MinecraftPingOptions
            {
                Address = "45.153.68.20",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    [Order(42)]
    public async Task ServerPing1_7_10()
    {
        try
        {
            // 1.7.10
            var options = new MinecraftPingOptions
            {
                Address = "45.153.68.20",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    [Order(43)]
    public async Task ServerPing1_5_2()
    {
        try
        {
            // 1.5.2
            var options = new MinecraftPingOptions
            {
                Address = "45.153.68.20",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    [Order(44)]
    public async Task ServerPing1_12_2()
    {
        try
        {
            // 1.12.2
            var options = new MinecraftPingOptions
            {
                Address = "45.153.68.20",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    [Order(45)]
    public async Task ServerPing1_16_5()
    {
        try
        {
            // 1.16.5
            var options = new MinecraftPingOptions
            {
                Address = "45.153.68.20",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    [Order(46)]
    public async Task ServerPing1_20_1()
    {
        try
        {
            // 1.20.1
            var options = new MinecraftPingOptions
            {
                Address = "45.153.68.20",
                Port = 25565,
                TimeOut = TimeSpan.FromMilliseconds(300)
            };

            var status = await Minecraft.PingAsync(options) as JavaStatus;

            Console.WriteLine($"{status?.OnlinePlayers} / {status?.MaximumPlayers}");

            Assert.That(status, Is.Not.Null);
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    [Order(50)]
    public async Task ChangeLoaderTypeAndSaveProfiles()
    {
        await GmlManager.Profiles.SaveProfiles();
    }

    [Test]
    [Order(60)]
    public async Task DownloadProfile()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "HiTech", "1.20.1", string.Empty, GameLoader.Vanilla,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        // if (await _testGameProfile.CheckIsFullLoaded(_options) == false)
        //     await _testGameProfile.DownloadAsync(_options.OsType, _options.OsArch);
    }

    [Test]
    [Order(70)]
    public async Task CheckIsFullLoaded()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "HiTech", "1.20.1", string.Empty, GameLoader.Vanilla,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        // Assert.That(await _testGameProfile.CheckIsFullLoaded(_options), Is.True);
    }

    [Test]
    [Order(75)]
    public async Task CheckInstallDotnet()
    {
        var isInstalled = await GmlManager.System.InstallDotnet();

        Assert.That(isInstalled, Is.True);
    }

    [Test]
    [Order(76)]
    public async Task Build_launcher()
    {
        var launcherVersions = await GmlManager.Launcher.GetVersions();

        var version = launcherVersions.First();
        bool isBuild = false;

        var logsDisposable = GmlManager.System.DownloadLogs.Subscribe(logs =>
        {
            Console.WriteLine(logs);
            Debug.WriteLine(logs);
        });

        var eventDisposable = GmlManager.Launcher.BuildLogs.Subscribe(logs =>
        {
            Console.WriteLine(logs);
            Debug.WriteLine(logs);
        });

        if (await GmlManager.System.InstallDotnet())
        {
            if (!GmlManager.Launcher.CanCompile(version, out var _))
            {
                await GmlManager.Launcher.Download(version, "http://localhost:5000", "GmlLauncher");
            }

            if (GmlManager.Launcher.CanCompile(version, out var message))
            {
                Console.WriteLine(message);
                Debug.WriteLine(message);
                isBuild = await GmlManager.Launcher.Build(version, ["win-x64"]);

            }
        }

        logsDisposable.Dispose();
        eventDisposable.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(GmlManager.Launcher.CanCompile(version, out _), Is.True);
            Assert.That(isBuild, Is.True);
        });
    }

    [Test]
    [Order(80)]
    public Task InstallForgeClient()
    {
        return Task.CompletedTask;
        // var forgeClient = await GmlManager.Profiles.GetProfile("Aztex")
        //                   ?? await GmlManager.Profiles.AddProfile("Aztex", "1.20.1",
        //                       string.Empty,
        //                       GameLoader.Forge,
        //                       string.Empty,
        //                       string.Empty)
        //                   ?? throw new Exception("Failed to create profile instance");

        // if (await forgeClient.CheckIsFullLoaded(_options) == false)
        //     await forgeClient.DownloadAsync(_options.OsType, _options.OsArch);
        //
        // var process = await forgeClient.CreateProcess(_options, User.Empty);

        // var processUtil = new ProcessUtil(process);
        //
        // processUtil.OutputReceived += (s, message) => Console.WriteLine(message);
        // processUtil.StartWithEvents();
        // await processUtil.WaitForExitTaskAsync();

        // Assert.That(await forgeClient.CheckIsFullLoaded(), Is.True);
    }

    [Test]
    [Order(90)]
    public Task ClientStartup()
    {
        return Task.CompletedTask;
        // _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
        //                    ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla, string.Empty,
        //                        string.Empty)
        //                    ?? throw new Exception("Failed to create profile instance");
        //
        // var process = await _testGameProfile.CreateProcess(_options, User.Empty);
        //
        // var processUtil = new ProcessUtil(process);
        //
        // processUtil.OutputReceived += (s, message) => Console.WriteLine(message);
        // processUtil.StartWithEvents();
        // await processUtil.WaitForExitTaskAsync();
    }

    [Test]
    [Order(91)]
    public async Task GetJavaVersions()
    {
        var versions = await GmlManager.System.GetJavaVersions();

        Assert.That(versions, Is.Not.Empty);
    }

    [Test]
    [Order(92)]
    public Task ChangeProfileVersion()
    {
        return Task.CompletedTask;
        // var versions = await GmlManager.System.GetJavaVersions();
        //
        // var version = versions.First(c => c.Version == "21.0.3");
        //
        // _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
        //                    ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", string.Empty, GameLoader.Vanilla,
        //                        string.Empty,
        //                        string.Empty)
        //                    ?? throw new Exception("Failed to create profile instance");
        //
        // await GmlManager.Profiles.ChangeBootstrapProgram(_testGameProfile, version);
    }

    [Test]
    [Order(92)]
    public async Task GetMods_By_Modrinth()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Forge)}-mods";

        var profile = await GmlManager.Profiles.GetProfile(name)
                      ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.1", string.Empty, GameLoader.Forge,
                          string.Empty,
                          string.Empty)
                      ?? throw new Exception("Failed to create profile instance");

        var mods = await GmlManager.Mods.FindModsAsync(
            profile.Loader,
            profile.GameVersion,
            ModType.Modrinth,
            string.Empty,
            10,
            0);

        await profile.Remove();

        Assert.That(mods, Is.Not.Empty);
        Assert.That(mods.OfType<ModrinthMod>(), Is.Not.Empty);
    }

    [Test]
    [Order(92)]
    public async Task GetMods_By_CurseForge()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Forge)}-mods";

        var profile = await GmlManager.Profiles.GetProfile(name)
                           ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.1", string.Empty, GameLoader.Forge,
                               string.Empty,
                               string.Empty)
                           ?? throw new Exception("Failed to create profile instance");

        var mods = await GmlManager.Mods.FindModsAsync(
            profile.Loader,
            profile.GameVersion,
            ModType.CurseForge,
            string.Empty,
            10,
            0);

        await profile.Remove();

        Assert.That(mods, Is.Not.Empty);
        Assert.That(mods.OfType<CurseForgeMod>(), Is.Not.Empty);
    }

    [Test]
    [Order(92)]
    public async Task GetModsInfo_By_Modrinth()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Forge)}-mods";

        var profile = await GmlManager.Profiles.GetProfile(name)
                      ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.1", string.Empty, GameLoader.Forge,
                          string.Empty,
                          string.Empty)
                      ?? throw new Exception("Failed to create profile instance");

        var mod = (await GmlManager.Mods.FindModsAsync(
            profile.Loader,
            profile.GameVersion,
            ModType.Modrinth,
            string.Empty,
            1,
            0)).First();

        var mods = await GmlManager.Mods.GetInfo(mod.Id, mod.Type);

        await profile.Remove();

        Assert.That(mods, Is.Not.Null);
    }

    [Test]
    [Order(92)]
    public async Task GetModsInfo_By_CurseForge()
    {
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Forge)}-mods";

        var profile = await GmlManager.Profiles.GetProfile(name)
                      ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.1", string.Empty, GameLoader.Forge,
                          string.Empty,
                          string.Empty)
                      ?? throw new Exception("Failed to create profile instance");

        var mod = (await GmlManager.Mods.FindModsAsync(
            profile.Loader,
            profile.GameVersion,
            ModType.CurseForge,
            string.Empty,
            1,
            0)).First();

        var mods = await GmlManager.Mods.GetInfo(mod.Id, mod.Type);

        await profile.Remove();

        Assert.That(mods, Is.Not.Null);
    }

    [Test]
    [Order(92)]
    public async Task AddModToProfile_From_Modrinth()
    {
        var modType = ModType.Modrinth;
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Fabric)}-mods";

        var profile = await GmlManager.Profiles.GetProfile(name)
                      ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.1", string.Empty, GameLoader.Fabric,
                          string.Empty,
                          string.Empty)
                      ?? throw new Exception("Failed to create profile instance");

        var mod = (await GmlManager.Mods.FindModsAsync(
            profile.Loader,
            profile.GameVersion,
            modType,
            "FabricApi",
            1,
            0)).First();

        var modInfo = await GmlManager.Mods.GetInfo(mod.Id, mod.Type);

        if (modInfo is null)
        {
            throw new Exception("Failed to get mod info");
        }

        var versions = await GmlManager.Mods.GetVersions(modInfo, modType, profile.Loader, profile.GameVersion);

        var version = versions.First(c => c.Files.Any()).Files.First();

        await profile.AddMod(Path.GetFileName(version), await GmlManager.LauncherInfo.Settings.HttpClient.GetStreamAsync(version));

        var mods = (await profile.GetModsAsync()).Any(c => c.Name == Path.GetFileNameWithoutExtension(version));

        await profile.Remove();

        Assert.That(mods, Is.True);

    }

    [Test]
    [Order(92)]
    public async Task AddModToProfile_From_Forge()
    {
        var modType = ModType.CurseForge;
        const string name = $"{CheckMinecraftVersion}{nameof(GameLoader.Fabric)}-mods";

        var profile = await GmlManager.Profiles.GetProfile(name)
                      ?? await GmlManager.Profiles.AddProfile(name, name, "1.20.1", string.Empty, GameLoader.Fabric,
                          string.Empty,
                          string.Empty)
                      ?? throw new Exception("Failed to create profile instance");

        var mod = (await GmlManager.Mods.FindModsAsync(
            profile.Loader,
            profile.GameVersion,
            modType,
            "Skins",
            1,
            0)).First();

        var modInfo = await GmlManager.Mods.GetInfo(mod.Id, mod.Type);

        if (modInfo is null)
        {
            throw new Exception("Failed to get mod info");
        }

        var versions = await GmlManager.Mods.GetVersions(modInfo, modType, profile.Loader, profile.GameVersion);

        var version = versions.First(c => c.Files.Any()).Files.First();

        await profile.AddMod(Path.GetFileName(version), await GmlManager.LauncherInfo.Settings.HttpClient.GetStreamAsync(version));

        var mods = (await profile.GetModsAsync()).Any(c => c.Name == Path.GetFileNameWithoutExtension(version));

        await profile.Remove();

        Assert.That(mods, Is.True);
    }

    [Test]
    [Order(93)]
    public async Task GetNewsForVk()
    {
        var vkProvider = new VkNewsProvider("", "");

        await GmlManager.Integrations.NewsProvider.AddListener(vkProvider);

        GmlManager.Integrations.NewsProvider.RefreshAsync();

        await Task.Delay(5000);

        var news = await GmlManager.Integrations.NewsProvider.GetNews();

        Assert.Multiple(async () =>
        {
            Assert.That(news, Is.Not.Null);
            Assert.That(news.Count, !Is.EqualTo(0));
        });
    }

    [Test]
    [Order(94)]
    public async Task GetNewsForUnicore()
    {
        var unicoreProvider = new UnicoreNewsProvider("https://api.unicorecms2.ru/news");

        await GmlManager.Integrations.NewsProvider.AddListener(unicoreProvider);

        GmlManager.Integrations.NewsProvider.RefreshAsync();

        await Task.Delay(5000);

        var news = await GmlManager.Integrations.NewsProvider.GetNews();

        Assert.Multiple(async () =>
        {
            Assert.That(news, Is.Not.Null);
            Assert.That(news.Count, !Is.EqualTo(0));
        });
    }

    [Test]
    [Order(95)]
    public async Task GetNewsForAzuriom()
    {
        var azuriomProvider = new AzuriomNewsProvider("https://magcent.ru/api/posts");

        await GmlManager.Integrations.NewsProvider.AddListener(azuriomProvider);

        GmlManager.Integrations.NewsProvider.RefreshAsync();

        await Task.Delay(5000);

        var news = await GmlManager.Integrations.NewsProvider.GetNews();

        Assert.Multiple(async () =>
        {
            Assert.That(news, Is.Not.Null);
            Assert.That(news.Count, !Is.EqualTo(0));
        });
    }

    [Test]
    [Order(96)]
    public async Task GetNewsForCustom()
    {
        var customProvider = new CustomNewsProvider("http://localhost:5292/api/v1/news");

        await GmlManager.Integrations.NewsProvider.AddListener(customProvider);

        GmlManager.Integrations.NewsProvider.RefreshAsync();

        await Task.Delay(5000);

        var news = await GmlManager.Integrations.NewsProvider.GetNews();

        Assert.Multiple(async () =>
        {
            Assert.That(news, Is.Not.Null);
            Assert.That(news.Count, !Is.EqualTo(0));
        });
    }

    [Test]
    [Order(900)]
    public Task CheckInstallationFromOriginalCmlLib()
    {
        return Task.CompletedTask;
        // var path = new MinecraftPath();
        // var launcher = new CMLauncher(path);
        // var forge = new MForge(launcher);
        // forge.InstallerOutput += (s, e) => Console.WriteLine(e);
        //
        // var versionName = await forge.Install("1.7.10");
        //
        // var launchOption = new MLaunchOption
        // {
        //     MaximumRamMb = 1024,
        //     Session = MSession.GetOfflineSession("TaiogStudio")
        // };
        //
        // var process = await launcher.CreateProcessAsync(versionName, launchOption);
        //
        // process.Start();
        // _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
        //                    ?? await GmlManager.Profiles.AddProfile("HiTech", "1.7.10", GameLoader.Forge, string.Empty,
        //                        string.Empty)
        //                    ?? throw new Exception("Failed to create profile instance");
        //
        // await GmlManager.Profiles.DownloadProfileAsync(_testGameProfile, _options.OsType, _options.OsArch);
        //
        // var myProcess = await _testGameProfile.CreateProcess(_options, User.Empty);
        //
        // Console.WriteLine();


        // process.Start();

        // Console.WriteLine();

        // _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
        //                    ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla, string.Empty,
        //                        string.Empty)
        //                    ?? throw new Exception("Failed to create profile instance");
        //
        // var process = await _testGameProfile.CreateProcess(new StartupOptions
        // {
        //     MinimumRamMb = 4096,
        //     FullScreen = false,
        //     ScreenHeight = 600,
        //     ScreenWidth = 900,
        //     ServerIp = "",
        //     ServerPort = 25565,
        //     MaximumRamMb = 4096
        // }, new User
        // {
        //     Name = "GamerVII"
        // });
        //
        // var processUtil = new ProcessUtil(process);
        //
        // processUtil.OutputReceived += (s, message) => Console.WriteLine(message);
        // processUtil.StartWithEvents();
        // await processUtil.WaitForExitTaskAsync();
    }
}
