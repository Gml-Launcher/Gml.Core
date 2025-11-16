using System;
using Newtonsoft.Json;

namespace Gml.Domains.Integrations;

public class AzuriomAuthResult
{
    [JsonProperty("id")] public int Id { get; set; }

    [JsonProperty("username")] public string Username { get; set; } = null!;

    [JsonProperty("email")] public string Email { get; set; } = null!;

    [JsonProperty("email_verified")] public bool EmailVerified { get; set; }

    [JsonProperty("money")] public decimal Money { get; set; }

    [JsonProperty("banned")] public bool Banned { get; set; }

    [JsonProperty("uuid")] public string Uuid { get; set; } = null!;

    [JsonProperty("access_token")] public string AccessToken { get; set; } = null!;

    [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }

    [JsonProperty("skin")] public SkinInfo Skin { get; set; } = null!;
}
