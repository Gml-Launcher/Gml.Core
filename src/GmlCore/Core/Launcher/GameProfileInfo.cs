using System.Collections.Generic;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.System;

namespace Gml.Core.Launcher
{
    public class GameProfileInfo : IGameProfileInfo
    {
        public string JavaPath { get; set; }
        public string ProfileName { get; set; }
        public string MinecraftVersion { get; set; }
        public string ClientVersion { get; set; }
        public string IconBase64 { get; set; }
        public string Description { get; set; }
        public string Arguments { get; set; }
        public IEnumerable<Gml.Core.System.LocalFileInfo> Files { get; set; }
        public IEnumerable<Gml.Core.System.LocalFileInfo> WhiteListFiles { get; set; }
    }
}
