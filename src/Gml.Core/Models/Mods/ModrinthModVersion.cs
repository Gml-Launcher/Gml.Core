using System;
using System.Collections.Generic;
using System.IO;
using GmlCore.Interfaces.Mods;
using Modrinth.Api.Models.Dto.Entities;

namespace Gml.Models.Mods;

public class ModrinthModVersion : IModVersion
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string VersionName { get; set; }
    public DateTimeOffset DatePublished { get; set; }
    public int Downloads { get; set; }
    public List<Dependency> Dependencies { get; set; } = [];
}
