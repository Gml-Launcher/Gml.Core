using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto.Entities
{
    public class File
    {
        [JsonPropertyName("hashes")] public Hashes Hashes { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("filename")] public string Filename { get; set; }

        [JsonPropertyName("primary")] public bool Primary { get; set; }

        [JsonPropertyName("size")] public int Size { get; set; }

        [JsonPropertyName("file_type")] public string FileType { get; set; }
    }
}
