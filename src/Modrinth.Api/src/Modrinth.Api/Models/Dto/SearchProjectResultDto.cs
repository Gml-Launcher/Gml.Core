using System.Collections.Generic;
using System.Text.Json.Serialization;
using Modrinth.Api.Models.Dto.Entities;

namespace Modrinth.Api.Models.Dto
{
    public class SearchProjectResultDto
    {
        [JsonPropertyName("hits")] public List<Hit> Hits { get; set; }

        [JsonPropertyName("offset")] public int Offset { get; set; }

        [JsonPropertyName("limit")] public int Limit { get; set; }

        [JsonPropertyName("total_hits")] public int TotalHits { get; set; }

        public ModrinthApi Api { get; set; }
        internal static SearchProjectResultDto Empty { get; } = new SearchProjectResultDto
        {
            Hits = new List<Hit>(),
            Limit = 0,
            Offset = 0,
            TotalHits = 0
        };

    }
}
