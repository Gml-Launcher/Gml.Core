using System;
using System.Collections.Generic;
using GmlCore.Interfaces.Mods;
using Modrinth.Models;

namespace Gml.Models.Mods;

public class ModrinthModVersion : IModVersion
{
    public Dependency[]? Dependencies { get; set; } = [];
    public string Id { get; set; }
    public string Name { get; set; }
    public string VersionName { get; set; }
    public DateTimeOffset DatePublished { get; set; }
    public int Downloads { get; set; }
    public List<string> Files { get; set; } = [];
}
