using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gml.Models.News;

public class Asset
{
    [JsonProperty("animation_url")] public string AnimationUrl;

    [JsonProperty("images")] public List<Image> Images;

    [JsonProperty("title")] public Title Title;

    [JsonProperty("title_color")] public TitleColor TitleColor;
}

public class Background
{
    [JsonProperty("light")] public string Light;

    [JsonProperty("dark")] public string Dark;
}

public class Color
{
    [JsonProperty("foreground")] public Foreground Foreground;

    [JsonProperty("background")] public Background Background;
}

public class Comments
{
    [JsonProperty("count")] public int Count;
}

public class Donut
{
    [JsonProperty("is_donut")] public bool IsDonut;
}

public class Foreground
{
    [JsonProperty("light")] public string Light;

    [JsonProperty("dark")] public string Dark;
}

public class Image
{
    [JsonProperty("url")] public string Url;

    [JsonProperty("width")] public int Width;

    [JsonProperty("height")] public int Height;
}

public class Item
{
    [JsonProperty("inner_type")] public string InnerType;

    [JsonProperty("donut")] public Donut Donut;

    [JsonProperty("comments")] public Comments Comments;

    [JsonProperty("marked_as_ads")] public int MarkedAsAds;

    [JsonProperty("zoom_text")] public bool ZoomText;

    [JsonProperty("hash")] public string Hash;

    [JsonProperty("type")] public string Type;

    [JsonProperty("push_subscription")] public PushSubscription PushSubscription;

    [JsonProperty("attachments")] public List<object> Attachments;

    [JsonProperty("date")] public int Date;

    [JsonProperty("from_id")] public int FromId;

    [JsonProperty("id")] public int Id;

    [JsonProperty("likes")] public Likes Likes;

    [JsonProperty("reaction_set_id")] public string ReactionSetId;

    [JsonProperty("owner_id")] public int OwnerId;

    [JsonProperty("post_type")] public string PostType;

    [JsonProperty("reposts")] public Reposts Reposts;

    [JsonProperty("text")] public string Text;

    [JsonProperty("views")] public Views Views;

    [JsonProperty("title")] public string? Title;

    [JsonProperty("asset")] public Asset Asset;
}

public class Likes
{
    [JsonProperty("can_like")] public int CanLike;

    [JsonProperty("count")] public int Count;

    [JsonProperty("user_likes")] public int UserLikes;
}

public class PushSubscription
{
    [JsonProperty("is_subscribed")] public bool IsSubscribed;
}

public class ReactionSet
{
    [JsonProperty("id")] public string Id;

    [JsonProperty("items")] public List<Item> Items;
}

public class Reposts
{
    [JsonProperty("count")] public int Count;
}

public class Response
{
    [JsonProperty("count")] public int Count;

    [JsonProperty("items")] public List<Item> Items;

    [JsonProperty("reaction_sets")] public List<ReactionSet> ReactionSets;
}

public class VkNewsResponse
{
    [JsonProperty("response")] public Response Response;
}

public class Title
{
    [JsonProperty("color")] public Color Color;
}

public class TitleColor
{
    [JsonProperty("light")] public string Light;

    [JsonProperty("dark")] public string Dark;
}

public class Views
{
    [JsonProperty("count")] public int Count;
}
