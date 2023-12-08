using System.Collections.Generic;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.System;

namespace Gml.Core.Launcher
{
    public class GameProfileInfo : IGameProfileInfo
    {
        public string ProfileName { get; set; }
        public string MinecraftVersion { get; set; }
        public string ClientVersion { get; set; }
        public string Arguments { get; set; }
        public IEnumerable<IFileInfo> Files { get; set; }
        public IEnumerable<IFileInfo> WhiteListFiles { get; set; }
    }
}
