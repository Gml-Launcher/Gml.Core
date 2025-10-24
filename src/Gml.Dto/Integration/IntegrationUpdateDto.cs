using GmlCore.Interfaces.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gml.Dto.Integration;

public class IntegrationUpdateDto
{
    [JsonConverter(typeof(StringEnumConverter))]
    public AuthType AuthType { get; set; }

    public string Endpoint { get; set; } = null!;
}
