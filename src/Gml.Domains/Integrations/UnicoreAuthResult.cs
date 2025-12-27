using System;
using Newtonsoft.Json;

namespace Gml.Domains.Integrations;

public class UnicoreAuthResult
{
    [JsonProperty("user")] public User User { get; set; } = null!;

    [JsonProperty("accessToken")] public string AccessToken { get; set; } = null!;

    [JsonProperty("refreshToken")] public string RefreshToken { get; set; } = null!;
}

public class Ban
{
    [JsonProperty("reason")] public string Reason { get; set; } = null!;

    [JsonProperty("expires")] public object Expires { get; set; } = null!;

    [JsonProperty("created")] public DateTime Created { get; set; }
}

public class User
{
    [JsonProperty("uuid")] public string Uuid { get; set; } = null!;

    [JsonProperty("username")] public string Username { get; set; } = null!;

    [JsonProperty("email")] public string Email { get; set; } = null!;

    [JsonProperty("password")] public string Password { get; set; } = null!;

    [JsonProperty("activated")] public bool Activated { get; set; }

    [JsonProperty("accessToken")] public object AccessToken { get; set; } = null!;

    [JsonProperty("serverId")] public object ServerId { get; set; } = null!;

    [JsonProperty("two_factor_enabled")] public object TwoFactorEnabled { get; set; } = null!;

    [JsonProperty("two_factor_secret")] public object TwoFactorSecret { get; set; } = null!;

    [JsonProperty("two_factor_secret_temp")]
    public string TwoFactorSecretTemp { get; set; } = null!;

    [JsonProperty("real")] public decimal Real { get; set; }

    [JsonProperty("virtual")] public decimal Virtual { get; set; }

    [JsonProperty("perms")] public object Perms { get; set; } = null!;

    [JsonProperty("created")] public DateTime Created { get; set; }

    [JsonProperty("updated")] public DateTime Updated { get; set; }

    [JsonProperty("cloak")] public object Cloak { get; set; } = null!;

    [JsonProperty("ban")] public Ban Ban { get; set; } = null!;

    [JsonProperty("skin")] public SkinInfo Skin { get; set; } = null!;
}
