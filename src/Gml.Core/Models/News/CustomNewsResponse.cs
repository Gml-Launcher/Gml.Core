using System;
using Newtonsoft.Json;

namespace Gml.Models.News;

public class CustomNewsResponse
{
    [JsonProperty("createdAt")] public DateTime CreatedAt;
    [JsonProperty("description")] public string? Description;
    [JsonProperty("id")] public int Id;
    [JsonProperty("title")] public string? Title;
}
