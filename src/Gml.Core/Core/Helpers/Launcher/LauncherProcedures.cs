using System;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;

namespace Gml.Core.Helpers.Launcher;

public class LauncherProcedures : ILauncherProcedures
{
    private readonly ILauncherInfo _launcherInfo;
    private readonly IStorageService _storage;
    private readonly IFileStorageProcedures _files;

    public LauncherProcedures(ILauncherInfo launcherInfo, IStorageService storage, IFileStorageProcedures files)
    {
        _launcherInfo = launcherInfo;
        _storage = storage;
        _files = files;
    }

    public async Task<string> CreateVersion(IVersionFile version, OsType osTypeEnum)
    {
        if (version.File is null)
        {
            throw new ArgumentNullException(nameof(version.File));
        }

        version.Guid = await _files.LoadFile(version.File, "launcher");

        _launcherInfo.ActualLauncherVersion[osTypeEnum] = version;

        await _storage.SetAsync(StorageConstants.ActualVersion, version.Guid);
        await _storage.SetAsync(StorageConstants.ActualVersionInfo, _launcherInfo.ActualLauncherVersion);

        await version.File.DisposeAsync();
        version.File = null;

        return version.Guid;

    }
}
