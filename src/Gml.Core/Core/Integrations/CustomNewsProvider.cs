using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Gml.Models.News;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.News;
using Newtonsoft.Json;

namespace Gml.Core.Integrations;

public class CustomNewsProvider : BaseNewsProvider
{
    public CustomNewsProvider(string url)
    {
        Type = NewsListenerType.Custom;
        Url = url;
    }

    public override async Task<IReadOnlyCollection<INewsData>> GetNews(int count = 20)
    {
        try
        {
            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(Url);

            if (!response.IsSuccessStatusCode)
                return [];

            var content = await response.Content.ReadAsStringAsync();

            var decoded = WebUtility.HtmlDecode(content);

            var data = JsonConvert.DeserializeObject<CustomNewsResponse[]>(decoded);

            if (data is null)
                return [];

            return data.Select(x => new NewsData
            {
                Title = x.Title ?? "Нет заголовка",
                Content = x.Description ?? "Нет описания",
                Type = Type,
                Date = x.CreatedAt
            }).ToList();

        }
        catch (Exception e)
        {
            GmlManager.BugTracker.CaptureException(e);

            return [];
        }
    }
}
