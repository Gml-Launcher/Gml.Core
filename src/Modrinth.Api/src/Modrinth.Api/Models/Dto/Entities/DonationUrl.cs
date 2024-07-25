using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto.Entities
{
    public class DonationUrl
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("platform")] public string Platform { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }
}
