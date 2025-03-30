using System;
using Newtonsoft.Json;

namespace Gml.Models.News;

public class Author
{
    [JsonProperty("id")] public int Id;

    [JsonProperty("name")] public string Name;

    [JsonProperty("role")] public Role Role;

    [JsonProperty("registered")] public DateTime Registered;
}

public class Role
{
    [JsonProperty("id")] public int Id;

    [JsonProperty("name")] public string Name;

    [JsonProperty("color")] public string Color;
}

public class AzuriomNewsResponse
{
    [JsonProperty("id")] public int Id;

    [JsonProperty("title")] public string? Title;

    [JsonProperty("description")] public string? Description;

    [JsonProperty("slug")] public string Slug;

    [JsonProperty("url")] public string Url;

    [JsonProperty("content")] public string Content;

    [JsonProperty("author")] public Author Author;

    [JsonProperty("published_at")] public DateTime PublishedAt;

    [JsonProperty("image")] public string Image;
}
