using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Models.News;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.News;

namespace Gml.Core.Integrations;

public class VkNewsProvider : BaseNewsProvider
{
    public override string Name => "Вконтакте";

    private readonly IGmlManager _gmlManager;
    private string _accessToken;

    public VkNewsProvider(string groupId, IGmlManager gmlManager)
    {
        Type = NewsListenerType.VK;
        _gmlManager = gmlManager;
        Url = groupId.Split(".com/").Last();

        SaveToken(gmlManager.LauncherInfo);

        gmlManager.LauncherInfo.SettingsUpdated.Subscribe(_ => SaveToken(gmlManager.LauncherInfo));
    }

    private void SaveToken(ILauncherInfo launcherInfo)
    {
        if (launcherInfo.AccessTokens.TryGetValue(AccessTokenTokens.VkKey, out var token) &&
            !string.IsNullOrEmpty(token))
        {
            _accessToken = token;
        }

        _ = GetNews();
    }

    public override async Task<IReadOnlyCollection<INewsData>> GetNews(int count = 20)
    {
        try
        {
            var url = "https://api.vk.com/method/wall.get";
            using var client = new HttpClient();

            var parameters = new Dictionary<string, string>
            {
                { "domain", Url },
                { "count", count.ToString() },
                { "access_token", _accessToken },
                { "v", "5.131" }
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await client.GetAsync(url + "?" + await content.ReadAsStringAsync());

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<VkNewsResponse>(json);

                if (data is null)
                    return Array.Empty<INewsData>();

                return data.Response?.Items.Select(x => new NewsData
                {
                    Title = x.Title ?? "Нет заголовка",
                    Content = x.Text,
                    Type = NewsListenerType.VK,
                    Date = DateTimeOffset.FromUnixTimeSeconds(x.Date),
                }).ToList() ?? [];
            }

            return [];
        }
        catch (Exception e)
        {
            _gmlManager.BugTracker.CaptureException(e);

            return [];
        }
    }
}
