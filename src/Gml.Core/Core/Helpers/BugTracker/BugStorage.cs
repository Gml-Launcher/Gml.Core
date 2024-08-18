using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Models.Converters;
using GmlCore.Interfaces.Launcher;
using Newtonsoft.Json;

namespace Gml.Core.Helpers.BugTracker;

public class FileStorageService(string filePath)
{
    private readonly JsonSerializerSettings _converterSettings = new()
    {
        Converters =
        [
            new MemoryInfoConverter(),
            new ExceptionReportConverter()
        ]
    };

    public async Task SaveBugAsync(IBugInfo bugInfo)
    {
        try
        {
            var json = JsonConvert.SerializeObject(bugInfo);
            await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving bug: {ex.Message}");
        }
        finally
        {
            Debug.WriteLine($"[{bugInfo.SendAt}] Error saving bug ");
        }
    }

    public async Task<IEnumerable<IBugInfo>> LoadUnprocessedBugs()
    {
        var bugs = new List<IBugInfo>();

        try
        {
            if (!File.Exists(filePath))
                return bugs;

            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var bug = JsonConvert.DeserializeObject<BugInfo>(line, _converterSettings);
                if (bug != null) bugs.Add(bug);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading unprocessed bugs: {ex.Message}");
        }

        return bugs;
    }

    public async Task MarkBugAsProcessedAsync(IBugInfo bugInfo)
    {
        try
        {
            var bugs = await LoadUnprocessedBugs();
            var updatedBugs = new List<IBugInfo>(bugs);
            updatedBugs.RemoveAll(b => b.SendAt.ToBinary() == bugInfo.SendAt.ToBinary());

            await SaveBugsToFileAsync(updatedBugs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking bug as processed: {ex.Message}");
        }
    }

    private async Task SaveBugsToFileAsync(List<IBugInfo> bugs)
    {
        var tempFilePath = Path.GetTempFileName();

        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var bug in bugs)
                {
                    var json = JsonConvert.SerializeObject(bug);
                    await writer.WriteLineAsync(json);
                }
            }

            File.Replace(tempFilePath, filePath, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving bugs to file: {ex.Message}");
        }
        finally
        {
            if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
        }
    }
}
