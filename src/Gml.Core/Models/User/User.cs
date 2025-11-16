using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GmlCore.Interfaces;
using GmlCore.Interfaces.User;

namespace Gml.Models.User;

public class User : IUser
{
    [JsonIgnore] internal bool IsValid => ExpiredDate != DateTime.MinValue && ExpiredDate > DateTime.Now;

    public static IUser Empty { get; } = new User
    {
        Name = "Default123",
        Uuid = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser",
        AccessToken = "sergsecgrfsecgriseuhcygrshecngrysicugrbn7csewgrfcsercgser"
    };

    public string Name { get; set; } = null!;
    public string? TextureSkinUrl { get; set; }
    public string? ExternalTextureSkinUrl { get; set; }
    public string? TextureCloakUrl { get; set; }
    public string? ExternalTextureCloakUrl { get; set; }
    public string ServerUuid { get; set; }

    public string? TextureSkinGuid { get; set; }
    public string? TextureCloakGuid { get; set; }
    public bool IsBanned { get; set; }
    public bool IsBannedPermanent { get; set; }
    public DateTime ServerExpiredDate { get; set; }
    public string? AccessToken { get; set; }
    public string? Uuid { get; set; }
    public DateTime ExpiredDate { get; set; }
    public bool IsSlim { get; set; }
    public List<ISession> Sessions { get; set; } = [];

    [JsonIgnore] public IGmlManager Manager { get; set; }

    public virtual async Task Block(bool isPermanent)
    {
        IsBanned = true;
        IsBannedPermanent = isPermanent;

        await Manager.Users.UpdateUser(this);
    }

    public virtual async Task Unblock(bool isPermanent)
    {
        IsBanned = false;

        if (isPermanent) IsBannedPermanent = false;

        await Manager.Users.UpdateUser(this);
    }

    private UriBuilder ReplaceHost(string url, string hostValue)
    {
        var originalUri = new Uri(url);
        var builder = new UriBuilder(originalUri);

        var newScheme = originalUri.Scheme;
        var newHost = originalUri.Host;
        var newPort = originalUri.Port;

        if (hostValue.Contains("://"))
        {
            var newUri = new Uri(hostValue);

            newScheme = newUri.Scheme;
            newHost = newUri.Host;

            newPort = newUri.IsDefaultPort ? -1 : newUri.Port;
        }
        else
        {
            var hostPart = hostValue;
            var port = (int?)null;

            var colonIndex = hostPart.LastIndexOf(':');
            if (colonIndex > 0 && colonIndex < hostPart.Length - 1)
            {
                var hostOnly = hostPart[..colonIndex];
                var portPart = hostPart[(colonIndex + 1)..];

                if (int.TryParse(portPart, out var parsedPort))
                {
                    hostPart = hostOnly;
                    port = parsedPort;
                }
            }

            newHost = hostPart;

            if (port.HasValue)
                newPort = port.Value;
        }

        builder.Scheme = newScheme;
        builder.Host = newHost;
        builder.Port = newPort;

        return builder;
    }

    public async Task DownloadAndInstallSkinAsync(string skinUrl, string? hostValue = null)
    {
        Debug.WriteLine($"Get skin: {skinUrl}");
        TextureSkinUrl = await Manager.Integrations.TextureProvider.SetSkin(this, skinUrl);
        TextureSkinGuid = !string.IsNullOrEmpty(TextureSkinUrl)
            ? Guid.NewGuid().ToString()
            : string.Empty;

        if (hostValue is not null && !string.IsNullOrEmpty(TextureSkinUrl))
        {
            var host = ReplaceHost(TextureSkinUrl, hostValue);

            host.Path = $"/api/v1/integrations/texture/skins/{TextureSkinGuid}";

            ExternalTextureSkinUrl = host.Uri.AbsoluteUri;
        }
    }


    public async Task DownloadAndInstallCloakAsync(string cloakUrl, string? hostValue = null)
    {
        Debug.WriteLine($"Get cloak: {cloakUrl}");
        TextureCloakUrl = await Manager.Integrations.TextureProvider.SetCloak(this, cloakUrl);

        TextureCloakGuid = !string.IsNullOrEmpty(TextureCloakUrl)
            ? Guid.NewGuid().ToString()
            : string.Empty;

        if (hostValue is not null && !string.IsNullOrEmpty(TextureCloakUrl))
        {
            var host = ReplaceHost(TextureCloakUrl, hostValue);

            host.Path = $"/api/v1/integrations/texture/capes/{TextureCloakGuid}";

            ExternalTextureCloakUrl = host.Uri.AbsoluteUri;
        }
    }

    public Task SaveUserAsync()
    {
        return Manager.Users.UpdateUser(this);
    }
}
