using GmlCore.Interfaces.Enums;

namespace Gml.Dto.News;

public class NewsGetListenerDto
{
    public string Url { get; set; }
    public NewsListenerType Type { get; set; }
}
