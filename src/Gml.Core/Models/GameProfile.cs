using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gml.Core.System;
using Gml.Models.Converters;
using Gml.Models.Servers;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Servers;
using GmlCore.Interfaces.System;
using Newtonsoft.Json;

namespace Gml.Models
{
    public class GameProfile : BaseProfile
    {

        private readonly Dictionary<IProfileServer, IDisposable> _serverTimers = new();
        private readonly Subject<IProfileServer> _serverAdded = new();
        private readonly Subject<IProfileServer> _serverRemoved = new();

        public GameProfile()
        {
        }

        internal GameProfile(string name, string gameVersion, GameLoader gameLoader)
            : base(name, gameVersion, gameLoader)
        {
            if (Servers.Any())
            {

            }

            _serverAdded.Subscribe(server =>
            {
                var timer = Observable
                    .Interval(TimeSpan.FromMinutes(1))
                    .Subscribe(_ => server.UpdateStatus());

                _serverTimers[server] = timer;
            });

            _serverRemoved.Subscribe(server =>
            {
                if (_serverTimers.TryGetValue(server, out var timer))
                {
                    timer.Dispose();
                    _serverTimers.Remove(server);
                }
            });
        }


        [JsonConverter(typeof(LocalFileInfoConverter))]
        public List<LocalFileInfo>? FileWhiteList
        {
            get => base.FileWhiteList?.Cast<LocalFileInfo>().ToList();
            set => base.FileWhiteList = value?.Cast<IFileInfo>().ToList();
        }

        [JsonConverter(typeof(ServerConverter))]
        public List<MinecraftServer> Servers
        {
            get => base.Servers.Cast<MinecraftServer>().ToList();
            set => base.Servers = value.Cast<IProfileServer>().ToList();
        }

        public static IGameProfile Empty { get; set; } =
            new GameProfile("Empty", "0.0.0", GmlCore.Interfaces.Enums.GameLoader.Undefined);

        public override void AddServer(IProfileServer server)
        {
            base.Servers.Add(server);
            _serverAdded.OnNext(server);
        }

        public override void RemoveServer(IProfileServer server)
        {
            base.Servers.Remove(server);
            _serverRemoved.OnNext(server);
        }
    }
}
