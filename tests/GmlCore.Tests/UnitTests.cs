using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Utils;
using Gml;
using Gml.Core.Launcher;
using Gml.Models;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;

namespace GmlCore.Tests;

public class Tests
{
    private IGameProfile _testGameProfile = null!;

    private GmlManager GmlManager { get; } = new(new GmlSettings("GamerVIILauncher"));

    [SetUp]
    public async Task Setup()
    {
        await GmlManager.Profiles.RestoreProfiles();
    }

    [Test, Order(0)]
    public void InitializeTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GmlManager.LauncherInfo.InstallationDirectory, Is.Not.Empty);
            Assert.That(GmlManager.LauncherInfo.BaseDirectory, Is.Not.Empty);
            Assert.That(GmlManager.LauncherInfo.Name, Is.Not.Empty);
        });
    }

    [Test, Order(1)]
    public async Task CreateProfile()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        Assert.Multiple(() =>
        {
            Assert.That(_testGameProfile, Is.Not.Null);
            Assert.That(_testGameProfile.GameVersion, Is.Not.Empty);
        });
    }

    [Test, Order(2)]
    public async Task AddProfile()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        Assert.Multiple(async () =>
        {
            Assert.That(await GmlManager.Profiles.CanAddProfile("HiTech", "1.20.1"), Is.False);
        });
    }

    [Test, Order(3)]
    public async Task ValidateProfile()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        Assert.Multiple(async () => { Assert.That(await _testGameProfile!.ValidateProfile(), Is.True); });
    }

    [Test, Order(4)]
    public async Task RemoveProfile()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        await _testGameProfile.Remove();
    }

    [Test, Order(5)]
    public async Task ChangeLoaderTypeAndSaveProfiles()
    {
        await GmlManager.Profiles.SaveProfiles();
    }

    [Test, Order(6)]
    public async Task DownloadProfile()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        if (await _testGameProfile.CheckIsFullLoaded() == false)
            await _testGameProfile.DownloadAsync();
    }

    [Test, Order(7)]
    public async Task CheckIsFullLoaded()
    {
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        Assert.That(await _testGameProfile.CheckIsFullLoaded(), Is.True);
    }


    [Test, Order(8)]
    public async Task InstallForgeClient()
    {
        
        var forgeClient = await GmlManager.Profiles.GetProfile("Aztex")
            ?? await GmlManager.Profiles.AddProfile("Aztex", "1.20.1", GameLoader.Forge)
            ?? throw new Exception("Failed to create profile instance");
        
        if (await forgeClient.CheckIsFullLoaded() == false)
            await forgeClient.DownloadAsync();
        
        var process = await forgeClient.CreateProcess(new StartupOptions
        {
            MinimumRamMb = 4096,
            FullScreen = false,
            ScreenHeight = 600,
            ScreenWidth = 900,
            ServerIp = null,
            ServerPort = 25565,
            MaximumRamMb = 8192
        });

        var processUtil = new ProcessUtil(process);

        processUtil.OutputReceived += (s, message) => Console.WriteLine(message);
        processUtil.StartWithEvents();
        await processUtil.WaitForExitTaskAsync();
        
        // Assert.That(await forgeClient.CheckIsFullLoaded(), Is.True);
        
    }
    

    [Test, Order(999)]
    public async Task ClientStartup()
    {
        
        _testGameProfile = await GmlManager.Profiles.GetProfile("HiTech")
                           ?? await GmlManager.Profiles.AddProfile("HiTech", "1.20.1", GameLoader.Vanilla)
                           ?? throw new Exception("Failed to create profile instance");
        
        var process = await _testGameProfile.CreateProcess(new StartupOptions
        {
            MinimumRamMb = 4096,
            FullScreen = false,
            ScreenHeight = 600,
            ScreenWidth = 900,
            ServerIp = "",
            ServerPort = 25565,
            MaximumRamMb = 4096
        });

        var processUtil = new ProcessUtil(process);

        processUtil.OutputReceived += (s, message) => Console.WriteLine(message);
        processUtil.StartWithEvents();
        await processUtil.WaitForExitTaskAsync();
    }
}