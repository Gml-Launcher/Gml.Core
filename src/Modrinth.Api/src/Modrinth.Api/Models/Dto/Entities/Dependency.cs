using System.Text.Json.Serialization;

namespace Modrinth.Api.Models.Dto.Entities
{
    public class Dependency
    {
        [JsonPropertyName("version_id")] public string VersionId { get; set; }

        [JsonPropertyName("project_id")] public string ProjectId { get; set; }

        [JsonPropertyName("file_name")] public string FileName { get; set; }

        [JsonPropertyName("dependency_type")] public string DependencyType { get; set; }
    }
}
