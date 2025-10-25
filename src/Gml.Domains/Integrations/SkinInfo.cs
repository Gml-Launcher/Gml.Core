using Newtonsoft.Json;

namespace Gml.Domains.Integrations;

public class SkinInfo
{
    [JsonProperty("slim")]
    public bool Slim { get; set; }
}
