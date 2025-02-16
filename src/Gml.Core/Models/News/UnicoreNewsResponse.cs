using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gml.Models.News;

public class Datum
{
    [JsonProperty("id")] public int Id;

    [JsonProperty("title")] public string? Title;

    [JsonProperty("description")] public string? Description;

    [JsonProperty("image")] public string? Image;

    [JsonProperty("link")] public object? Link;

    [JsonProperty("created")] public DateTime Created;
}

public class Links
{
    [JsonProperty("current")] public string Current;
}

public class Meta
{
    [JsonProperty("itemsPerPage")] public int ItemsPerPage;

    [JsonProperty("totalItems")] public int TotalItems;

    [JsonProperty("currentPage")] public int CurrentPage;

    [JsonProperty("totalPages")] public int TotalPages;

    [JsonProperty("sortBy")] public List<List<string>> SortBy;
}

public class UnicoreNewsResponse
{
    [JsonProperty("data")] public List<Datum> Data;

    [JsonProperty("meta")] public Meta Meta;

    [JsonProperty("links")] public Links Links;
}
