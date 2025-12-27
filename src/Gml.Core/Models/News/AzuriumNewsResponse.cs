using System;
using Newtonsoft.Json;

namespace Gml.Models.News;

public class Author
{
    [JsonProperty("id")] public int Id;

    [JsonProperty("name")] public string Name;

    [JsonProperty("registered")] public DateTime Registered;

    [JsonProperty("role")] public Role Role;
}

public class Role
{
    [JsonProperty("color")] public string Color;
    [JsonProperty("id")] public int Id;

    [JsonProperty("name")] public string Name;
}

public class AzuriomNewsResponse
{
    [JsonProperty("author")] public Author Author;

    [JsonProperty("content")] public string Content;

    [JsonProperty("description")] public string? Description;
    [JsonProperty("id")] public int Id;

    [JsonProperty("image")] public string Image;

    [JsonProperty("published_at")] public DateTime PublishedAt;

    [JsonProperty("slug")] public string Slug;

    [JsonProperty("title")] public string? Title;

    [JsonProperty("url")] public string Url;
}
