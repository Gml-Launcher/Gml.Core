using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CmlLib.Core.Java;
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
using Minio.DataModel.Tags;

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

        public async Task<IFileInfo?> DownloadFileStream(string fileHash, Stream outputStream,
            IHeaderDictionary headers)
        {
            LocalFileInfo? localFileInfo = default;

            switch (_launcherInfo.StorageSettings.StorageType)
            {
                case StorageType.LocalStorage:
                    localFileInfo = await _storage.GetAsync<LocalFileInfo>(fileHash).ConfigureAwait(false);

                    if (localFileInfo is null)
                    {
                        return localFileInfo;
                    }

                    if (Path.GetFileName(localFileInfo?.FullPath) is {} fileName && Guid.TryParse(fileName, out var guid) && localFileInfo!.FullPath.Contains("Attachments"))
                    {
                        // Если это дополнительный файл

                    }else if (localFileInfo != null) // Загрузка файлов minecraft
                    {
                        // Если это файл профиля
                        localFileInfo.FullPath = Path.GetFullPath(string.Join("/", _launcherInfo.InstallationDirectory,
                            localFileInfo.Directory));
                    }

                    await using (var stream = new FileStream(localFileInfo!.FullPath, FileMode.Open))
                    {
                        await stream.CopyToAsync(outputStream);
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
                            .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(outputStream, token));

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

        public async Task<string> LoadFile(Stream fileStream)
        {
            var fileName = Guid.NewGuid().ToString();

            switch (_launcherInfo.StorageSettings.StorageType)
            {
                case StorageType.LocalStorage:

                    var filePath = Path.Combine(_launcherInfo.InstallationDirectory, "Attachments", fileName);

                    await ConvertStreamToFile(fileStream, filePath);

                    await _storage.SetAsync(fileName, new LocalFileInfo
                    {
                        Name = fileName,
                        Directory = filePath.Replace(Path.GetFullPath(fileName), string.Empty),
                        FullPath = filePath
                    });
                    break;

                case StorageType.S3:

                    string bucketName = "profile-backgrounds";
                    var beArgs = new BucketExistsArgs().WithBucket(bucketName);
                    bool found = await MinioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);

                    if (!found)
                    {
                        var mbArgs = new MakeBucketArgs()
                            .WithBucket(bucketName);

                        await MinioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
                    }

                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket("launcher")
                        .WithObject(fileName)
                        .WithStreamData(fileStream);

                    await MinioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return fileName;
        }

        private async Task ConvertStreamToFile(Stream input, string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Directory!.Exists)
            {
                fileInfo.Directory.Create();
            }

            using (var fileStream = File.Create(filePath))
            {
                await input.CopyToAsync(fileStream);
            }
        }

        private async Task<string> ConvertStreamToBase64Async(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            var fileBytes = memoryStream.ToArray();
            return Convert.ToBase64String(fileBytes);
        }
    }
}
