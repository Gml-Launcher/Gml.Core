using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Models.Converters;
using GmlCore.Interfaces.Launcher;
using Newtonsoft.Json;

namespace Gml.Core.Helpers.BugTracker;

public class FileStorageService(string filePath)
{
    protected readonly ConcurrentDictionary<string, IBugInfo> _bugBuffer = new();

    private readonly JsonSerializerSettings _converterSettings = new()
    {
        Converters =
        [
            new MemoryInfoConverter(),
            new ExceptionReportConverter(),
            new StackTraceConverter()
        ]
    };

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    protected async Task SaveBugAsync(IBugInfo bugInfo)
    {
        _bugBuffer[bugInfo.Id] = bugInfo;

        await SaveBufferedBugsAsync();
    }

    public async Task RemoveBugAsync(string bugId)
    {
        if (_bugBuffer.TryRemove(bugId, out _)) await SaveBufferedBugsAsync();
    }

    private async Task SaveBufferedBugsAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            var bugList = _bugBuffer.Values.ToList();
            if (bugList.Count >= 0)
            {
                var json = JsonConvert.SerializeObject(bugList, Formatting.Indented, _converterSettings);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task LoadUnprocessedBugsAsync()
    {
        await _semaphore.WaitAsync(); // Гарантия, что только один поток будет работать с файлом

        try
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var bugs = JsonConvert.DeserializeObject<List<BugInfo>>(json, _converterSettings);

                if (bugs != null)
                    foreach (var bug in bugs)
                        _bugBuffer[bug.Id] = bug;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
