using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Gml.Dto.User;

public class BaseUserPassword
{
    public string Login { get; set; }
    public string Password { get; set; }
    public string AccessToken { get; set; }

    [JsonPropertyName("2FACode")]
    [JsonProperty("2FACode")]
    public string? TwoFactorCode { get; set; }
}
