using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto
{
    public class Category
    {
        [JsonPropertyName("icon")] public string Icon { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("project_type")] public string ProjectType { get; set; }

        [JsonPropertyName("header")] public string Header { get; set; }
    }
}
