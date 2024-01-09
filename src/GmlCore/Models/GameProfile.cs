using System.Collections.Generic;
using System.Linq;
using Gml.Core.System;
using Gml.Models.Converters;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.System;
using Newtonsoft.Json;

namespace Gml.Models
{
    public class GameProfile : BaseProfile
    {


        [JsonConverter(typeof(LocalFileInfoConverter))]
        public List<LocalFileInfo>? FileWhiteList
        {
            get => base.FileWhiteList?.Cast<LocalFileInfo>().ToList();
            set => base.FileWhiteList = value?.Cast<IFileInfo>().ToList();
        }

        public GameProfile()
        {
        }

        internal GameProfile(string name, string gameVersion, GameLoader gameLoader)
            : base(name, gameVersion, gameLoader)
        {
        }

        public static IGameProfile Empty { get; set; } =
            new GameProfile("Empty", "0.0.0", GmlCore.Interfaces.Enums.GameLoader.Undefined);
    }
}
