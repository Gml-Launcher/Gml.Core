using System;
using System.Collections.Generic;
using CurseForge.APIClient.Models.Files;
using GmlCore.Interfaces.Mods;
using Modrinth.Api.Models.Dto.Entities;

namespace Gml.Models.Mods;

public class CurseForgeModVersion : IModVersion
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset DatePublished { get; set; }
    public int Downloads { get; set; }
    public string VersionName { get; set; }
    public List<FileDependency> Dependencies { get; set; } = [];
    public List<string> Files { get; set; } = [];
}
