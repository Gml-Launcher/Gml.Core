using System.Collections.Generic;
using System.Linq;
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
        public GameProfile()
        {
        }

        internal GameProfile(string name, string gameVersion, GameLoader gameLoader)
            : base(name, gameVersion, gameLoader)
        {
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
    }
}
