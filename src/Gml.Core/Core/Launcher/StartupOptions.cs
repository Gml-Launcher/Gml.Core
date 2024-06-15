using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Launcher;

namespace Gml.Core.Launcher
{
    public class StartupOptions : IStartupOptions
    {
        public static IStartupOptions Empty { get; set; } = new StartupOptions
        {
            FullScreen = false,
            ScreenHeight = 500,
            ScreenWidth = 500,
            MaximumRamMb = 1024,
            MinimumRamMb = 1024,
            OsName = "windows",
            OsArch = "64"
        };

        public int MinimumRamMb { get; set; }
        public int MaximumRamMb { get; set; }
        public bool FullScreen { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public string? ServerIp { get; set; }
        public int ServerPort { get; set; }
        public string OsName { get; set; }
        public string OsArch { get; set; }
    }
}
