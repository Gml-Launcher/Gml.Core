using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gml.Dto.Minecraft.AuthLib;

public class Profile
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("properties")] public List<ProfileProperties> Properties { get; set; }
}
