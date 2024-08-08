using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.BugTracker;

public class BugTrackerProcedures : FileStorageService, IBugTrackerProcedures
{
    private readonly IStorageService _storage;
    private readonly ConcurrentBag<IBugInfo> _bugs = new();
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
        var unprocessedBugs = await LoadUnprocessedBugs();

        foreach (var bug in unprocessedBugs)
        {
            _bugs.Add(bug);
            _bugStream.OnNext(bug);
        }
    }

    public void CaptureException(IBugInfo bugInfo)
    {
        _bugQueue.Add(bugInfo);

        Task.Run(async () =>
        {
            await SaveBugAsync(bugInfo);
            _bugStream.OnNext(bugInfo);
        });
    }

    private async Task ProcessBugAsync(IBugInfo bug)
    {
        try
        {
            await Task.Delay(500);

            await MarkBugAsProcessedAsync(bug);
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
}
