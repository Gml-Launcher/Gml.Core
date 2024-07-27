using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Modrinth.Api.Core.Projects;
using Modrinth.Api.Core.System;
using Modrinth.Api.Models.Dto;
using Modrinth.Api.Models.Dto.Entities;

namespace Modrinth.Api.Models.Projects
{
    public class Project
    {
        [JsonPropertyName("slug")] public string Slug { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("categories")] public List<string> Categories { get; set; }

        [JsonPropertyName("client_side")] public string ClientSide { get; set; }

        [JsonPropertyName("server_side")] public string ServerSide { get; set; }

        [JsonPropertyName("body")] public string Body { get; set; }

        [JsonPropertyName("status")] public string Status { get; set; }

        [JsonPropertyName("requested_status")] public string RequestedStatus { get; set; }

        [JsonPropertyName("additional_categories")]
        public List<string> AdditionalCategories { get; set; }

        [JsonPropertyName("issues_url")] public string IssuesUrl { get; set; }

        [JsonPropertyName("source_url")] public string SourceUrl { get; set; }

        [JsonPropertyName("wiki_url")] public string WikiUrl { get; set; }

        [JsonPropertyName("discord_url")] public string DiscordUrl { get; set; }

        [JsonPropertyName("donation_urls")] public List<DonationUrl> DonationUrls { get; set; }

        [JsonPropertyName("project_type")] public string ProjectType { get; set; }

        [JsonPropertyName("downloads")] public int Downloads { get; set; }

        [JsonPropertyName("icon_url")] public string IconUrl { get; set; }

        [JsonPropertyName("color")] public int? Color { get; set; }

        [JsonPropertyName("thread_id")] public string ThreadId { get; set; }

        [JsonPropertyName("monetization_status")]
        public string MonetizationStatus { get; set; }

        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("team")] public string Team { get; set; }

        [JsonPropertyName("body_url")] public object BodyUrl { get; set; }

        [JsonPropertyName("moderator_message")]
        public object ModeratorMessage { get; set; }

        [JsonPropertyName("published")] public string Published { get; set; }

        [JsonPropertyName("updated")] public string Updated { get; set; }

        [JsonPropertyName("approved")] public string Approved { get; set; }

        [JsonPropertyName("queued")] public string Queued { get; set; }

        [JsonPropertyName("followers")] public int Followers { get; set; }

        [JsonPropertyName("license")] public License License { get; set; }

        [JsonPropertyName("versions")] public List<string> Versions { get; set; }

        [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; }

        [JsonPropertyName("loaders")] public List<string> Loaders { get; set; }

        [JsonPropertyName("gallery")] public List<Gallery> Gallery { get; set; }
        public ProjectType ProjectTypeEnum => ProjectHelper.GetProjectType(ProjectType);
        public ModrinthApi Api { get; set; }

        public Task<IEnumerable<Version>> GetVersionsAsync(CancellationToken token)
        {
            return Api.Mods.GetVersionsAsync(Id, token);
        }
    }
}
