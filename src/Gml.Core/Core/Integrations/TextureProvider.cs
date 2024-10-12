using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Gml.Common.TextureService;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.User;
using Newtonsoft.Json;

namespace Gml.Core.Integrations;

public class TextureProvider(string textureServiceEndpoint) : ITextureProvider
{
    private readonly HttpClient _httpClintSkinChecker = new();
    private readonly HttpClient _httpClientLoader = new()
    {
        BaseAddress = new Uri(textureServiceEndpoint)
    };

    private readonly string _skinPrefix = "-s";
    private readonly string _cloakPrefix = "-c";

    public async Task<string> SetSkin(IUser user, string skinUrl)
    {
        var requestUri = $"skin/{user.Name}";

        var model = await UpdateTexture(user, skinUrl, requestUri, _skinPrefix);

        return model?.SkinUrl ?? string.Empty;
    }

    public async Task<string> SetCloak(IUser user, string cloakUrl)
    {
        var requestUri = $"cloak/{user.Name}";

        var model = await UpdateTexture(user, cloakUrl, requestUri, _cloakPrefix);

        return model?.ClockUrl ?? string.Empty;
    }

    public Task<Stream> GetSkinStream(string? textureUrl)
    {
        return _httpClintSkinChecker.GetStreamAsync(textureUrl);
    }

    public Task<Stream> GetCloakStream(string? textureUrl)
    {
        return _httpClintSkinChecker.GetStreamAsync(textureUrl);
    }

    private async Task<TextureReadDto?> UpdateTexture(IUser user, string skinUrl, string requestUri, string prefix)
    {
        var skinResponseMessage = await _httpClintSkinChecker.GetStreamAsync(skinUrl);

        if (skinResponseMessage is not null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var content = new MultipartFormDataContent();

            //ToDo: GetFileSize
            content.Add(new StreamContent(skinResponseMessage, 1000), "file", $"{prefix}{user.Name}.png");

            request.Content = content;
            var response = await _httpClientLoader.SendAsync(request);

            var data = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TextureReadDto>(data);

        }

        return null;
    }
}
