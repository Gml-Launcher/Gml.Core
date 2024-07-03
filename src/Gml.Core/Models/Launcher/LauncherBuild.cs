using System;
using GmlCore.Interfaces.Launcher;

namespace Gml.Models.Launcher;

public class LauncherBuild : ILauncherBuild
{
    public string Name { get; set; }
    public string Path { get; set; } = null!;
    public string ExecutableFilePath { get; set; } = null!;
    public DateTime DateTime { get; set; }
}
