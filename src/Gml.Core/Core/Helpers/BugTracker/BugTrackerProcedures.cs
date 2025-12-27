using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Sentry;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Helpers.BugTracker;

public class BugTrackerProcedures : FileStorageService, IBugTrackerProcedures
{
    private readonly BlockingCollection<IBugInfo> _bugQueue = new();
    private readonly ISubject<IBugInfo> _bugStream = new Subject<IBugInfo>();
    private readonly IDisposable _clearTimer;
    private readonly IGmlSettings _settings;
    private readonly IStorageService _storage;
    private readonly IDisposable _subscription;
    private IDisposable _timerDispose;

    public BugTrackerProcedures(IStorageService storage,
        IObservable<IStorageSettings> observableSettings,
        IGmlSettings settings) : base("BugStorage.json")
    {
        _storage = storage;
        _settings = settings;

        _subscription = _bugStream
            .SelectMany(bugInfo => Observable.FromAsync(() => ProcessBugAsync(bugInfo)))
            .Subscribe();

        _clearTimer = observableSettings.Subscribe(UpdateClearTimer);

        Task.Run(ProcessQueueAsync);

        _ = LoadUnprocessedBugsFromStorage();
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

    public IBugInfo CaptureException(Exception exception)
    {
        var bugInfo = new BugInfo
        {
            SendAt = DateTime.Now,
            Username = _settings.Name,
            PcName = _settings.Name,
            IpAddress = "localhost",
            MemoryInfo = new MemoryInfo(),
            OsIdentifier = "GmlBackendRuntime",
            OsVersion = "GmlServer",
            Exceptions = new List<ExceptionReport>
            {
                new()
                {
                    Type = exception.GetType().FullName,
                    Module = exception.GetType().Assembly.FullName,
                    ValueData = exception.Message,
                    ThreadId = Environment.CurrentManagedThreadId,
                    StackTrace = new List<IStackTrace>
                    {
                        new StackTrace
                        {
                            Function = exception.StackTrace
                        }
                    }
                }
            },
            ProjectType = ProjectType.Backend
        };

        CaptureException(bugInfo);

        return bugInfo;
    }

    public async Task<IEnumerable<IBugInfo>> GetAllBugs()
    {
        return await _storage.GetBugsAsync<BugInfo>();
    }

    public async Task<IBugInfo?> GetBugId(Guid id)
    {
        return await _storage.GetBugIdAsync(id);
    }

    public Task<IEnumerable<IBugInfo>> GetFilteredBugs(Expression<Func<IStorageBug, bool>> filter)
    {
        return _storage.GetFilteredBugsAsync(filter);
    }

    public Task SolveAllAsync()
    {
        return _storage.ClearBugsAsync();
    }

    public void UpdateClearTimer(IStorageSettings newSettings)
    {
        _timerDispose?.Dispose();

        var timeSpan = newSettings.SentryAutoClearPeriod;
        var needRunPeriodTimer = newSettings.SentryNeedAutoClear;

        if (needRunPeriodTimer && timeSpan > TimeSpan.Zero)
        {
            _timerDispose = Observable.Interval(timeSpan)
                .Subscribe(_ => Task.Run(async () => await _storage.ClearBugsAsync()));
        }
    }

    private async Task ProcessQueueAsync()
    {
        foreach (var bug in _bugQueue.GetConsumingEnumerable()) await SaveBugAsync(bug);
    }

    private async Task LoadUnprocessedBugsFromStorage()
    {
        await LoadUnprocessedBugsAsync();

        foreach (var bug in _bugBuffer.Values) _bugStream.OnNext(bug);
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
}
