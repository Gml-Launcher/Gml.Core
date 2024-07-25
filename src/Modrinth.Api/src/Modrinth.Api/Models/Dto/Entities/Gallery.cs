using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto.Entities
{
    public class Gallery
    {
        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("featured")] public bool Featured { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("created")] public string Created { get; set; }

        [JsonPropertyName("ordering")] public int Ordering { get; set; }
    }
}
