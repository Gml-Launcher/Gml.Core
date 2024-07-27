using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto.Entities
{
    public class License
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }
}
