using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Core.System;

namespace Modrinth.Api.Models.Dto.Entities
{
    public class Hit
    {
        [JsonPropertyName("slug")] public string Slug { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("categories")] public List<string> Categories { get; set; }

        [JsonPropertyName("client_side")] public string ClientSide { get; set; }

        [JsonPropertyName("server_side")] public string ServerSide { get; set; }

        [JsonPropertyName("project_type")] public string ProjectType { get; set; }

        [JsonPropertyName("downloads")] public int Downloads { get; set; }

        [JsonPropertyName("icon_url")] public string IconUrl { get; set; }

        [JsonPropertyName("color")] public int? Color { get; set; }

        [JsonPropertyName("thread_id")] public string ThreadId { get; set; }

        [JsonPropertyName("monetization_status")]
        public string MonetizationStatus { get; set; }

        [JsonPropertyName("project_id")] public string ProjectId { get; set; }

        [JsonPropertyName("author")] public string Author { get; set; }

        [JsonPropertyName("display_categories")]
        public List<string> DisplayCategories { get; set; }

        [JsonPropertyName("versions")] public List<string> Versions { get; set; }

        [JsonPropertyName("follows")] public int Follows { get; set; }

        [JsonPropertyName("date_created")] public DateTimeOffset DateCreated { get; set; }

        [JsonPropertyName("date_modified")] public DateTimeOffset DateModified { get; set; }

        [JsonPropertyName("latest_version")] public string LatestVersion { get; set; }

        [JsonPropertyName("license")] public string License { get; set; }

        [JsonPropertyName("gallery")] public List<string> Gallery { get; set; }

        [JsonPropertyName("featured_gallery")] public string FeaturedGallery { get; set; }

        public ProjectType ProjectTypeEnum => ProjectHelper.GetProjectType(ProjectType);
    }
}
