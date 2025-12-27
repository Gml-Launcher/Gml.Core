using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gml.Models.News;

public class Datum
{
    [JsonProperty("created")] public DateTime Created;

    [JsonProperty("description")] public string? Description;
    [JsonProperty("id")] public int Id;

    [JsonProperty("image")] public string? Image;

    [JsonProperty("link")] public object? Link;

    [JsonProperty("title")] public string? Title;
}

public class Links
{
    [JsonProperty("current")] public string Current;
}

public class Meta
{
    [JsonProperty("currentPage")] public int CurrentPage;
    [JsonProperty("itemsPerPage")] public int ItemsPerPage;

    [JsonProperty("sortBy")] public List<List<string>> SortBy;

    [JsonProperty("totalItems")] public int TotalItems;

    [JsonProperty("totalPages")] public int TotalPages;
}

public class UnicoreNewsResponse
{
    [JsonProperty("data")] public List<Datum> Data;

    [JsonProperty("links")] public Links Links;

    [JsonProperty("meta")] public Meta Meta;
}
