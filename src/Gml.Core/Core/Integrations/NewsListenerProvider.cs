using System;
using System.Collections.Generic;
using System.Linq;
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
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Integrations;

public class NewsListenerProvider : INewsListenerProvider, IDisposable, IAsyncDisposable
{
    private List<INewsProvider> _providers = [];
    private readonly List<INewsData> _newsCache = [];
    private readonly IDisposable _timer;
    private readonly IStorageService _storage;
    private readonly IBugTrackerProcedures _bugTracker;
    private readonly GmlManager _gmlManager;

    public IReadOnlyCollection<INewsProvider> Providers => _providers;

    private const int MaxCacheSize = 20;

    public NewsListenerProvider(
        TimeSpan timespan,
        IStorageService storage,
        IBugTrackerProcedures bugTracker,
        GmlManager gmlManager)
    {
        _storage = storage;
        _bugTracker = bugTracker;
        _gmlManager = gmlManager;
        _timer = Observable.Timer(TimeSpan.Zero, timespan).Subscribe(OnProvide);
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
        return Task.FromResult<ICollection<INewsData>>(_newsCache);
    }

    public async Task RefreshAsync(long count = 0)
    {
        try
        {
            foreach (var provider in _providers)
            {
                var providerNews = await provider.GetNews();

                foreach (var news in providerNews)
                {
                    if (_newsCache.All(cachedNews => cachedNews.Date != news.Date))
                    {
                        _newsCache.Add(news);
                    }
                }

                _newsCache.Sort((a, b) => b.Date.CompareTo(a.Date));

                if (_newsCache.Count > MaxCacheSize)
                {
                    _newsCache.RemoveRange(MaxCacheSize, _newsCache.Count - MaxCacheSize);
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

        return Task.WhenAll(_storage.SetAsync(StorageConstants.NewsProviders, _providers), RefreshAsync());
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
                Converters = { new NewsProviderConverter(_gmlManager) }
            }) ?? new List<INewsProvider>();

        await RefreshAsync();
    }

    public Task RemoveListenerByType(NewsListenerType type)
    {
        _providers.RemoveAll(x => x.Type == type);

        _newsCache.RemoveAll(c => c.Type == type);

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
