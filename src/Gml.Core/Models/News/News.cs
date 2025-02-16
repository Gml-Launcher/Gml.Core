using System;
using GmlCore.Interfaces.News;

namespace Gml.Models.News;

public class News : INews
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Date { get; set; }
}
