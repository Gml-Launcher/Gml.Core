using System;
using GmlCore.Interfaces.Enums;

namespace Gml.Dto.News;

public record NewsReadDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset? Date { get; set; }
    public NewsListenerType Type { get; set; }
}
