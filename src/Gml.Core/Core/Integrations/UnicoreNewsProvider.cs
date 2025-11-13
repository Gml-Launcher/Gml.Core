using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Gml.Models.News;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.News;
using Newtonsoft.Json;

namespace Gml.Core.Integrations;

[JsonDerivedType(typeof(UnicoreNewsProvider), "unicore")]
public class UnicoreNewsProvider : BaseNewsProvider
{
    public UnicoreNewsProvider(string url)
    {
        Type = NewsListenerType.UnicoreCMS;
        Url = url;
    }

    public override string Name => "UniCoreCMS";

    public override async Task<IReadOnlyCollection<INewsData>> GetNews(int count = 20)
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync(Url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            var decoded = WebUtility.HtmlDecode(content);

            var data = JsonConvert.DeserializeObject<UnicoreNewsResponse>(decoded);

            if (data is null)
                return Array.Empty<INewsData>();

            return data.Data.Select(x => new NewsData
            {
                Title = x.Title ?? "Нет заголовка",
                Content = x.Description ?? "Нет описания",
                Type = Type,
                Date = x.Created
            }).ToList();
        }

        return Array.Empty<INewsData>();
    }
}
