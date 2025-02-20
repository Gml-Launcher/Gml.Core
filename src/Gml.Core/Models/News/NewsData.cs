using System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.News;

namespace Gml.Models.News;

public class NewsData : INewsData
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Date { get; set; }
    public NewsListenerType Type { get; set; }
}
