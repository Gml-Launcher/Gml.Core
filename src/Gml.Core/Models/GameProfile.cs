using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gml.Models.Converters;
using Gml.Models.Servers;
using Gml.Models.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Servers;
using GmlCore.Interfaces.System;
using Newtonsoft.Json;

namespace Gml.Models;

public class GameProfile : BaseProfile
{
    private readonly ConcurrentDictionary<IProfileServer, IDisposable> _serverTimers = new();

    public GameProfile()
    {
        ServerAdded.Subscribe(CreateServerListener);
    }

    internal GameProfile(string name, string displayName, string gameVersion, GameLoader gameLoader)
        : base(name, displayName, gameVersion, gameLoader)
    {
        ServerAdded.Subscribe(CreateServerListener);

        ServerRemoved.Subscribe(server =>
        {
            if (_serverTimers.TryGetValue(server, out var timer))
            {
                timer.Dispose();
                _serverTimers.TryRemove(server, out _);
            }
        });
    }

    internal Subject<IProfileServer> ServerAdded { get; } = new();

    internal Subject<IProfileServer> ServerRemoved { get; } = new();

    [JsonConverter(typeof(ServerConverter))]
    public List<MinecraftServer> Servers
    {
        get => base.Servers.Cast<MinecraftServer>().ToList();
        set => base.Servers = value.Cast<IProfileServer>().ToList();
    }

    public static IGameProfile Empty { get; set; } =
        new GameProfile("Empty", "Empty", "0.0.0", GmlCore.Interfaces.Enums.GameLoader.Undefined);


    [JsonConverter(typeof(LocalFileInfoConverter))]
    public List<LocalFileInfo>? FileWhiteList
    {
        get => base.FileWhiteList?.Cast<LocalFileInfo>().ToList();
        set => base.FileWhiteList = value?.Cast<IFileInfo>().ToList();
    }

    [JsonConverter(typeof(LocalFolderInfoConverter))]
    public List<LocalFolderInfo>? FolderWhiteList
    {
        get => base.FolderWhiteList?.Cast<LocalFolderInfo>().ToList();
        set => base.FolderWhiteList = value?.Cast<IFolderInfo>().ToList();
    }

    private async void CreateServerListener(IProfileServer server)
    {
        await server.UpdateStatusAsync();

        var timer = Observable.Interval(TimeSpan.FromMinutes(2))
            .Subscribe(_ => server.UpdateStatusAsync());

        _serverTimers.TryAdd(server, timer);
    }

    public override void AddServer(IProfileServer server)
    {
        if (Servers.Any(c => c.Name == server.Name))
            throw new Exception($"Сервер с наименованием {server.Name} уже существует!");

        base.Servers.Add(server);
        ServerAdded.OnNext(server);
    }

    public override void RemoveServer(IProfileServer server)
    {
        base.Servers.Remove(server);
        ServerRemoved.OnNext(server);
    }
}
