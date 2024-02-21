using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using Gml.Core.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;

namespace Gml.Core.Helpers.Files
{
    public class FileStorageProcedures : IFileStorageProcedures
    {
        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storage;

        public FileStorageProcedures(ILauncherInfo launcherInfo, IStorageService storage)
        {
            _launcherInfo = launcherInfo;
            _storage = storage;
        }

        public async Task<IFileInfo?> GetFileInfo(string fileHash)
        {
            return await _storage.GetAsync<LocalFileInfo>(fileHash).ConfigureAwait(false);
        }
    }
}
