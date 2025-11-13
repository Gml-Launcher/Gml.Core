using System.Collections.Generic;

namespace Gml.Dto.Mods;

public class ExtendedModInfoReadDto : ExtendedModReadDto
{
    public IReadOnlyCollection<ModVersionDto> Versions { get; set; }
}
