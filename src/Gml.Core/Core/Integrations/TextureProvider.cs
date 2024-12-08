﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Gml.Common.TextureService;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.User;
using Newtonsoft.Json;

namespace Gml.Core.Integrations;

public class TextureProvider(string textureServiceEndpoint, IBugTrackerProcedures bugTracker) : ITextureProvider
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
        try
        {
            var requestUri = $"skin/{user.Name}";

            var model = await UpdateTexture(user, skinUrl, requestUri, _skinPrefix);

            return model?.SkinUrl!;
        }
        catch (Exception exception)
        {
            bugTracker.CaptureException(exception);
            Debug.WriteLine(exception);
            return string.Empty;
        }
    }

    public async Task<string> SetCloak(IUser user, string cloakUrl)
    {
        try
        {
            var requestUri = $"cloak/{user.Name}";

            var model = await UpdateTexture(user, cloakUrl, requestUri, _cloakPrefix);

            return model?.ClockUrl!;
        }
        catch (Exception exception)
        {
            bugTracker.CaptureException(exception);
            Debug.WriteLine(exception);
            return string.Empty;
        }
    }

    public Task<Stream> GetSkinStream(string? textureUrl)
    {
        return _httpClintSkinChecker.GetStreamAsync(textureUrl);
    }

    public Task<Stream> GetCloakStream(string? textureUrl)
    {
        return _httpClintSkinChecker.GetStreamAsync(textureUrl);
    }

    public Task<Stream> GetHeadByNameStream(string? userName)
    {
        return _httpClientLoader.GetStreamAsync($"/skin/{userName}/head/128");
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
