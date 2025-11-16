using GmlCore.Interfaces.Integrations;

namespace Gml.Domains.Integrations;

public class DiscordRpcClient : IDiscordRpcClient
{
    public string ClientId { get; set; } = null!;
    public string Details { get; set; } = null!;
    public string LargeImageKey { get; set; } = null!;
    public string LargeImageText { get; set; } = null!;
    public string SmallImageKey { get; set; } = null!;
    public string SmallImageText { get; set; } = null!;
}
