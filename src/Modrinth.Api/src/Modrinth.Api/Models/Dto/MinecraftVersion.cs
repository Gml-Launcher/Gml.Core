using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto
{
    public class MinecraftVersion
    {
        [JsonPropertyName("version")] public string Version { get; set; }

        [JsonPropertyName("version_type")] public string VersionType { get; set; }

        [JsonPropertyName("date")] public string Date { get; set; }

        [JsonPropertyName("major")] public bool Major { get; set; }
    }
}
