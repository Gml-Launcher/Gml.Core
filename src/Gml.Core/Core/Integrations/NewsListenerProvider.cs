using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Services.Storage;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.News;

namespace Gml.Core.Integrations;

public class NewsListenerProvider : INewsListenerProvider, IDisposable, IAsyncDisposable
{
    private readonly List<INewsProvider> _providers = [];
    private readonly LinkedList<INewsData> _cache = [];
    private readonly IDisposable _timer;
    private readonly IStorageService _storage;

    private const int MaxCacheSize = 20;

    public NewsListenerProvider(TimeSpan timespan, IStorageService storage)
    {
        _storage = storage;
        _timer = Observable.Timer(timespan).Subscribe(RefreshAsync);

        var newsListeners = _storage.GetNewsListenerAsync().Result;

        foreach (var newsListener in newsListeners)
        {
            switch (newsListener!.Type)
            {
                case NewsListenerType.Azuriom:
                    AddListener(new AzuriomNewsProvider(newsListener.Url));
                    break;
                case NewsListenerType.UnicoreCMS:
                    AddListener(new UnicoreNewsProvider(newsListener.Url));
                    break;
                case NewsListenerType.Custom:
                    AddListener(new CustomNewsProvider(newsListener.Url));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        RefreshAsync();
    }

    public Task<ICollection<INewsData>> GetNews(int count = 20)
    {
        return Task.FromResult<ICollection<INewsData>>(_cache);
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
        if (_providers.Contains(newsProvider))
            _providers.Remove(newsProvider);

        _providers.Add(newsProvider);

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
