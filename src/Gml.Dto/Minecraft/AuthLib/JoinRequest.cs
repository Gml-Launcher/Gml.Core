using Newtonsoft.Json;

namespace Gml.Dto.Minecraft.AuthLib;

public class JoinRequest
{
    [JsonProperty("accessToken")] public string AccessToken { get; set; }

    [JsonProperty("selectedProfile")] public string SelectedProfile { get; set; }

    [JsonProperty("serverId")] public string ServerId { get; set; }
}
