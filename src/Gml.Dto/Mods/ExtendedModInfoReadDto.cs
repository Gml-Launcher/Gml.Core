using System.Collections.Generic;
using GmlCore.Interfaces.Mods;

namespace Gml.Dto.Mods;

public class ExtendedModInfoReadDto : ExtendedModReadDto
{
    public IReadOnlyCollection<ModVersionDto> Versions { get; set; }
}
