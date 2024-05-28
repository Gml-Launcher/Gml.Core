using System.Collections.Generic;
using Gml.Core.System;
using GmlCore.Interfaces.Launcher;

namespace Gml.Core.Launcher
{
    public class GameProfileInfo : IGameProfileInfo
    {
        public string JavaPath { get; set; }
        public string IconBase64 { get; set; }
        public string Description { get; set; }
        public IEnumerable<LocalFileInfo> Files { get; set; }
        public IEnumerable<LocalFileInfo> WhiteListFiles { get; set; }
        public bool HasUpdate { get; set; }
        public string ProfileName { get; set; }
        public string MinecraftVersion { get; set; }
        public string ClientVersion { get; set; }
        public string Arguments { get; set; }
        public string JvmArguments { get; set; }
    }
}
