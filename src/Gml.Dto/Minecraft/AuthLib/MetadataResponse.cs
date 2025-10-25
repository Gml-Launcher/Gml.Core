using Newtonsoft.Json;

namespace Gml.Dto.Minecraft.AuthLib;

public class MetadataResponse
{
    [JsonProperty("meta")] public Metadata Meta { get; set; } = new();

    [JsonProperty("skinDomains")] public string[] SkinDomains { get; set; }

    [JsonProperty("signaturePublickey")] public string SignaturePublicKey { get; set; }
}
