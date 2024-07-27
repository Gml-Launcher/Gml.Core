using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Modrinth.Api.Core.System
{
    public class FileLoader
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task DownloadFileAsync(string url, string destinationDirectory, CancellationToken token)
        {
            var fileName = Path.GetFileName(url);
            var fileInfo = new FileInfo(Path.Combine(destinationDirectory, fileName));

            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                return;
            }

            var stream = await _httpClient.GetStreamAsync(url);
            await using (var fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream, token);
            }
        }

        public Task DownloadFilesAsync(IEnumerable<string> urls, string destinationDirectory, CancellationToken token)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            var downloadTasks = urls.Select(url => DownloadFileAsync(url, destinationDirectory, token));

            return Task.WhenAll(downloadTasks);
        }
    }
}
