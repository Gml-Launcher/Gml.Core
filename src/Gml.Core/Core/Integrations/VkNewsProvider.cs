using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Models.News;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.News;

namespace Gml.Core.Integrations;

public class VkNewsProvider : INewsProvider
{
    private readonly string _groupId;
    private readonly string _apikey;

    public VkNewsProvider(string apiKey, string id)
    {
        _apikey = apiKey;
        _groupId = id;
    }

    public async Task<IReadOnlyCollection<INewsData>> GetNews(int count = 20)
    {
        var url = "https://api.vk.com/method/wall.get";
        using var client = new HttpClient();

        var parameters = new Dictionary<string, string>
        {
            { "owner_id", _groupId }, // ID группы со знаком минус
            { "count", count.ToString() },
            { "access_token", _apikey }, // Сервисный ключ
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

            return data.Response.Items.Select(x => new NewsData
            {
                Title = x.Title ?? "Нет заголовка",
                Content = x.Text,
                Date = DateTimeOffset.Now,
            }).ToList();
        }

        return Array.Empty<INewsData>();
    }
}
