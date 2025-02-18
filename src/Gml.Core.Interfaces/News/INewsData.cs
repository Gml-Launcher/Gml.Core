using System;

namespace GmlCore.Interfaces.News;

public interface INewsData
{
    public string Title { get; }
    public string Content { get; }
    public DateTimeOffset Date { get; }
}
