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
    [JsonProperty("dark")] public string Dark;
    [JsonProperty("light")] public string Light;
}

public class Color
{
    [JsonProperty("background")] public Background Background;
    [JsonProperty("foreground")] public Foreground Foreground;
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
    [JsonProperty("dark")] public string Dark;
    [JsonProperty("light")] public string Light;
}

public class Image
{
    [JsonProperty("height")] public int Height;
    [JsonProperty("url")] public string Url;

    [JsonProperty("width")] public int Width;
}

public class Item
{
    [JsonProperty("asset")] public Asset Asset;

    [JsonProperty("attachments")] public List<object> Attachments;

    [JsonProperty("comments")] public Comments Comments;

    [JsonProperty("date")] public int Date;

    [JsonProperty("donut")] public Donut Donut;

    [JsonProperty("from_id")] public int FromId;

    [JsonProperty("hash")] public string Hash;

    [JsonProperty("id")] public int Id;
    [JsonProperty("inner_type")] public string InnerType;

    [JsonProperty("likes")] public Likes Likes;

    [JsonProperty("marked_as_ads")] public int MarkedAsAds;

    [JsonProperty("owner_id")] public int OwnerId;

    [JsonProperty("post_type")] public string PostType;

    [JsonProperty("push_subscription")] public PushSubscription PushSubscription;

    [JsonProperty("reaction_set_id")] public string ReactionSetId;

    [JsonProperty("reposts")] public Reposts Reposts;

    [JsonProperty("text")] public string Text;

    [JsonProperty("title")] public string? Title;

    [JsonProperty("type")] public string Type;

    [JsonProperty("views")] public Views Views;

    [JsonProperty("zoom_text")] public bool ZoomText;
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
    [JsonProperty("response")] public Response? Response;
}

public class Title
{
    [JsonProperty("color")] public Color Color;
}

public class TitleColor
{
    [JsonProperty("dark")] public string Dark;
    [JsonProperty("light")] public string Light;
}

public class Views
{
    [JsonProperty("count")] public int Count;
}
