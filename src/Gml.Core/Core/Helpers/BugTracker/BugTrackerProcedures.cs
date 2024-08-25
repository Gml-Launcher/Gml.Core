using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Models.Converters;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.BugTracker;

public class BugTrackerProcedures : FileStorageService, IBugTrackerProcedures
{
    private readonly IStorageService _storage;
    private readonly ISubject<IBugInfo> _bugStream = new Subject<IBugInfo>();
    private readonly BlockingCollection<IBugInfo> _bugQueue = new();
    private readonly IDisposable _subscription;

    public BugTrackerProcedures(IStorageService storage) : base("BugStorage.json")
    {
        _storage = storage;

        _subscription = _bugStream
            .SelectMany(bugInfo => Observable.FromAsync(() => ProcessBugAsync(bugInfo)))
            .Subscribe();

        Task.Run(ProcessQueueAsync);

        _ = LoadUnprocessedBugsFromStorage();
    }

    private async Task ProcessQueueAsync()
    {
        foreach (var bug in _bugQueue.GetConsumingEnumerable())
        {
            await SaveBugAsync(bug);
        }
    }

    private async Task LoadUnprocessedBugsFromStorage()
    {
        await LoadUnprocessedBugsAsync();

        foreach (var bug in _bugBuffer.Values)
        {
            _bugStream.OnNext(bug);
        }
    }

    public void CaptureException(IBugInfo bugInfo)
    {
        bugInfo.Id = Guid.NewGuid().ToString();

        _bugQueue.Add(bugInfo);

        Task.Run(async () =>
        {
            await SaveBugAsync(bugInfo).ConfigureAwait(false);
            _bugStream.OnNext(bugInfo);
        });
    }

    private async Task ProcessBugAsync(IBugInfo bug)
    {
        try
        {
            await _storage.AddBugAsync(bug);

            await RemoveBugAsync(bug.Id);
        }
        catch (Exception ex)
        {
            // ignore
        }
    }

    public void StopProcessing()
    {
        _subscription.Dispose();
    }

    public async Task<IEnumerable<IBugInfo>> GetAllBugs()
    {
        return await _storage.GetBugsAsync<BugInfo>(new JsonSerializerOptions
        {
            Converters = { new SessionConverter() }
        });;
    }
}
