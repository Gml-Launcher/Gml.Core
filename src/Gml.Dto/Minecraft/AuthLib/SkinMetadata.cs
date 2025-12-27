using Newtonsoft.Json;

namespace Gml.Dto.Minecraft.AuthLib;

public class SkinMetadata
{
    [JsonProperty("model")] public string Model { get; set; }
}
