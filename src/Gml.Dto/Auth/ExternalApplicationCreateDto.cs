using System.Collections.Generic;

namespace Gml.Dto.Auth;

public class ExternalApplicationCreateDto
{
    public string Name { get; set; } = null!;
    public List<int> PermissionIds { get; set; } = new();
}
