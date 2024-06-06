using System;
using System.IO;
using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using Gml.Core.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.System;
using Microsoft.AspNetCore.Http;
using Minio;
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

                    if (Path.GetFileName(localFileInfo?.FullPath) is { } fileName &&
                        Guid.TryParse(fileName, out var guid) && localFileInfo!.FullPath.Contains("Attachments"))
                    {
                        // Если это дополнительный файл
                    }
                    else if (localFileInfo != null) // Загрузка файлов minecraft
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
                        //ToDo: Rewrite
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
                        else
                        {
                            getObjectArgs = new GetObjectArgs()
                                .WithBucket("profile-backgrounds")
                                .WithObject(fileHash)
                                .WithCallbackStream(async (stream, token) =>
                                    await stream.CopyToAsync(outputStream, token));

                            headers.Add("Content-Disposition", $"attachment; filename={fileHash}");
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
                        .WithBucket(bucketName)
                        .WithContentType("application/octet-stream")
                        .WithObject(fileName)
                        .WithObjectSize(fileStream.Length)
                        .WithStreamData(fileStream);

                    await MinioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return fileName;
        }

        public async Task<(Stream File, string fileName, long Length)> GetFileStream(string fileHash)
        {
            Stream fileStream = new MemoryStream();
            string fileName = string.Empty;
            long length = 0;

            switch (_launcherInfo.StorageSettings.StorageType)
            {
                case StorageType.LocalStorage:
                    var localFileInfo = await _storage.GetAsync<LocalFileInfo>(fileHash).ConfigureAwait(false);

                    if (localFileInfo is not null)
                    {
                        // If it's an additional file
                        if (Path.GetFileName(localFileInfo.FullPath) is { } name && Guid.TryParse(name, out _))
                        {
                            // fileStream = [ Something related to additional files ]
                        }
                        // If it is a Minecraft file
                        else
                        {
                            // fileStream = [ Something related to Minecraft files ]
                        }

                        using (var stream = new FileStream(localFileInfo.FullPath, FileMode.Open))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        fileName = localFileInfo.Name;
                        length = fileStream.Length;
                    }

                    break;

                case StorageType.S3:
                    try
                    {
                        var statObjectArgs = new GetObjectTagsArgs()
                            .WithBucket("profiles")
                            .WithObject(fileHash);

                        var metadata = await MinioClient.GetObjectTagsAsync(statObjectArgs);

                        var getObjectArgs = new GetObjectArgs()
                            .WithBucket("profiles")
                            .WithObject(fileHash)
                            .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(fileStream, token));

                        if (metadata != null)
                        {
                            fileName = metadata.Tags["file-name"];
                            length = fileStream.Length;
                            await MinioClient.GetObjectAsync(getObjectArgs);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Resetting the position for the caller to be able to read the stream from the beginning
            fileStream.Position = 0;
            return (fileStream, fileName, length);
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
