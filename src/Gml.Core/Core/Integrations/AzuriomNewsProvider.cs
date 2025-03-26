using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Gml.Models.News;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.News;
using Newtonsoft.Json;

namespace Gml.Core.Integrations;

public class AzuriomNewsProvider : BaseNewsProvider
{
    public NewsListenerType Type { get; }

    public AzuriomNewsProvider(string url, NewsListenerType type)
    {
        Type = type;
        Url = url;
    }

    public override async Task<IReadOnlyCollection<INewsData>> GetNews(int count = 20)
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync(Url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<AzuriomNewsResponse[]>(content);

            if (data is null)
                return Array.Empty<INewsData>();

            return data.Select(x => new NewsData
            {
                Title = x.Title ?? "Нет заголовка",
                Content = x.Description ?? "Нет описания",
                Date = x.PublishedAt
            }).ToList();
        }

        return Array.Empty<INewsData>();
    }
}
