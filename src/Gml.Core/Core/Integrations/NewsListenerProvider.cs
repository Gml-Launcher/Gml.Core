using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.News;

namespace Gml.Core.Integrations;

public class NewsListenerProvider : INewsListenerProvider, IDisposable, IAsyncDisposable
{
    private readonly List<INewsProvider> _providers = [];
    private readonly LinkedList<INews> _cache = [];
    private readonly IDisposable _timer;

    private const int MaxCacheSize = 20;

    public NewsListenerProvider(TimeSpan timespan)
    {
        _timer = Observable.Timer(timespan).Subscribe(RefreshAsync);

        RefreshAsync(0);
    }
    public Task<ICollection<INews>> GetNews(int count = 20)
    {
        return Task.FromResult<ICollection<INews>>(_cache);
    }

    public async void RefreshAsync(long number = 0)
    {
        foreach (var provider in _providers)
        {
            var providerNews = await provider.GetNews();

            foreach (var newsItem in providerNews)
            {
                _cache.AddLast(newsItem);

                if (_cache.Count > MaxCacheSize)
                {
                    _cache.RemoveFirst();
                }
            }
        }
    }

    public Task AddListener(INewsProvider newsProvider)
    {
        if (!_providers.Contains(newsProvider))
        {
            _providers.Add(newsProvider);
        }

        return Task.CompletedTask;
    }

    public Task RemoveListener(INewsProvider newsProvider)
    {
        if (_providers.Contains(newsProvider))
        {
            _providers.Remove(newsProvider);
        }
        else
        {
            throw new NewsProviderNotFoundException("The provider was not found.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _timer.Dispose();

        return default;
    }
}
