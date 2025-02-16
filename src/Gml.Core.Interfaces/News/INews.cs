using System;

namespace GmlCore.Interfaces.News;

public interface INews
{
    public string Title { get; }
    public string Content { get; }
    public DateTimeOffset Date { get; }
}
