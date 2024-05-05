using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using Gml.Core.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Gml.Core.Helpers.Files
{
    public class FileStorageProcedures : IFileStorageProcedures
    {
        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storage;
        private IMinioClient? _minioClient;

        internal IMinioClient MinioClient
        {
            get
            {
                return _minioClient ??= new MinioClient()
                    .WithEndpoint(_launcherInfo.StorageSettings.StorageHost)
                    .WithCredentials(_launcherInfo.StorageSettings.StorageLogin,
                        _launcherInfo.StorageSettings.StoragePassword)
                    .Build();
            }
        }

        public FileStorageProcedures(ILauncherInfo launcherInfo, IStorageService storage)
        {
            _launcherInfo = launcherInfo;
            _storage = storage;
        }

        public async Task<IFileInfo?> DownloadFileStream(string fileHash, Stream outputStream, IHeaderDictionary headers)
        {
            LocalFileInfo? localFileInfo = default;

            switch (_launcherInfo.StorageSettings.StorageType)
            {
                case StorageType.LocalStorage:
                    localFileInfo = await _storage.GetAsync<LocalFileInfo>(fileHash).ConfigureAwait(false);

                    if (localFileInfo != null)
                    {

                        localFileInfo.FullPath = Path.GetFullPath(string.Join("/", _launcherInfo.InstallationDirectory,
                            localFileInfo.Directory));

                        using (var stream = new FileStream(localFileInfo.FullPath, FileMode.Open))
                        {
                            await stream.CopyToAsync(outputStream);
                        }
                    }



                    break;

                case StorageType.S3:

                    try
                    {
                        GetObjectTagsArgs statObjectArgs = new GetObjectTagsArgs()
                            .WithBucket("profiles")
                            .WithObject(fileHash);

                        var metadata = await MinioClient.GetObjectTagsAsync(statObjectArgs);

                        var getObjectArgs = new GetObjectArgs()
                            .WithBucket("profiles")
                            .WithObject(fileHash)
                            .WithCallbackStream(async (stream, token) =>  await stream.CopyToAsync(outputStream, token));

                        if (metadata is not null)
                        {
                            headers.Add("Content-Disposition", $"attachment; filename={metadata.Tags["file-name"]}");
                            await MinioClient.GetObjectAsync(getObjectArgs);
                        }

                        localFileInfo = new LocalFileInfo();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return localFileInfo;
        }
    }
}
