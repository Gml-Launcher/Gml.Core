using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Services.Storage;
using Gml.Models.Converters;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.News;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Integrations;

public class NewsListenerProvider : INewsListenerProvider, IDisposable, IAsyncDisposable
{
    private List<INewsProvider> _providers = [];
    private readonly LinkedList<INewsData> _cache = [];
    private readonly IDisposable _timer;
    private readonly IStorageService _storage;
    private readonly IBugTrackerProcedures _bugTracker;

    public IReadOnlyCollection<INewsProvider> Providers => _providers;

    private const int MaxCacheSize = 20;

    public NewsListenerProvider(TimeSpan timespan, IStorageService storage, IBugTrackerProcedures bugTracker)
    {
        _storage = storage;
        _bugTracker = bugTracker;
        _timer = Observable.Timer(timespan).Subscribe(OnProvide);
    }

    private async void OnProvide(long _)
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception e)
        {
            _bugTracker.CaptureException(e);
        }
    }

    public Task<ICollection<INewsData>> GetNews(int count = 20)
    {
        return Task.FromResult<ICollection<INewsData>>(_cache);
    }

    public async Task RefreshAsync(long number = 0)
    {
        try
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
        catch (Exception exception)
        {
            _bugTracker.CaptureException(exception);
        }
    }

    public Task AddListener(INewsProvider newsProvider)
    {
        _providers.Remove(newsProvider);
        _providers.Add(newsProvider);

        return Task.WhenAll([_storage.SetAsync(StorageConstants.NewsProviders, _providers), RefreshAsync()]);
    }

    public Task RemoveListener(INewsProvider newsProvider)
    {
        if (!_providers.Remove(newsProvider))
        {
            throw new NewsProviderNotFoundException("The provider was not found.");
        }

        return _storage.SetAsync(StorageConstants.NewsProviders, _providers);
    }

    public async Task Restore()
    {
        _providers = await _storage.GetAsync<List<INewsProvider>>(StorageConstants.NewsProviders,
            new JsonSerializerOptions
            {
                Converters = { new NewsProviderConverter() }
            }) ?? [];
    }

    public Task RemoveListenerByType(NewsListenerType type)
    {
        _providers.RemoveAll(x => x.Type == type);

        return _storage.SetAsync(StorageConstants.NewsProviders, _providers);
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
