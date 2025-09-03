using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gml.Core.Services.Storage;
using Gml.Models.System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.System;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Tags;

namespace Gml.Core.Helpers.Files
{
    public class FileStorageProcedures : IFileStorageProcedures
    {
        private readonly ILauncherInfo _launcherInfo;
        private readonly IStorageService _storage;
        private readonly IBugTrackerProcedures _bugTracker;
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

        public FileStorageProcedures(ILauncherInfo launcherInfo, IStorageService storage,
            IBugTrackerProcedures bugTracker)
        {
            _launcherInfo = launcherInfo;
            _storage = storage;
            _bugTracker = bugTracker;

            launcherInfo.SettingsUpdated.Subscribe(SettingsUpdated);
        }

        private void SettingsUpdated(IStorageSettings settings)
        {
            switch (_launcherInfo.StorageSettings.StorageType)
            {
                case StorageType.LocalStorage:
                    break;
                case StorageType.S3:
                    _minioClient?.Dispose();
                    _minioClient = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<IFileInfo?> DownloadFileStream(
            string fileHash,
            Stream outputStream)
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
                            // headers.Add("Content-Disposition", $"attachment; filename={metadata.Tags["file-name"]}");
                            await MinioClient.GetObjectAsync(getObjectArgs);
                        }
                        else
                        {
                            getObjectArgs = new GetObjectArgs()
                                .WithBucket("profile-backgrounds")
                                .WithObject(fileHash)
                                .WithCallbackStream(async (stream, token) =>
                                    await stream.CopyToAsync(outputStream, token));

                            // headers.Add("Content-Disposition", $"attachment; filename={fileHash}");
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

        public async Task<string> LoadFile(Stream fileStream,
            string? folder = null,
            string? defaultFileName = null,
            Dictionary<string, string>? tags = null)
        {
            var fileName = defaultFileName ?? Guid.NewGuid().ToString();

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

                    string bucketName = folder ?? "other";

                    await CreateBuсketIfNotExists(bucketName);

                    if (fileStream.Length > 0)
                    {
                        var putObjectArgs = new PutObjectArgs()
                            .WithContentType("application/octet-stream")
                            .WithObjectSize(fileStream.Length)
                            .WithStreamData(fileStream)
                            .WithBucket(bucketName)
                            .WithObject(fileName);

                        if (tags is not null && tags.Any())
                        {
                            putObjectArgs.WithTagging(new Tagging(tags, true));
                        }

                        await MinioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return fileName;
        }

        private async Task CreateBuсketIfNotExists(string bucketName)
        {
            var beArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool found = await MinioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);

            if (!found)
            {
                var mbArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await MinioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            }
        }

        public async Task<(Stream File, string fileName, long Length)> GetFileStream(string fileHash, string? backet = null)
        {
            Stream fileStream = new MemoryStream();
            string fileName = string.Empty;
            long length = 0;

            switch (_launcherInfo.StorageSettings.StorageType)
            {
                case StorageType.LocalStorage:
                    var localFileInfo = await _storage.GetAsync<LocalFileInfo>(fileHash).ConfigureAwait(false);

                    if (localFileInfo is not null && File.Exists(localFileInfo.FullPath))
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

                        fileStream = new FileStream(localFileInfo.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                        fileName = localFileInfo.Name;
                        length = new FileInfo(localFileInfo.FullPath).Length;
                    }

                    break;

                case StorageType.S3:
                    try
                    {
                        var statObjectArgs = new GetObjectTagsArgs()
                            .WithBucket("profiles")
                            .WithObject(fileHash);

                        var getObjectArgs = new GetObjectArgs()
                            .WithBucket("profiles")
                            .WithObject(fileHash)
                            .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(fileStream, token));

                        var metadata = await MinioClient.GetObjectTagsAsync(statObjectArgs);

                        if (metadata is null)
                        {
                            statObjectArgs = new GetObjectTagsArgs()
                                .WithBucket("profile-backgrounds")
                                .WithObject(fileHash);

                            metadata = await MinioClient.GetObjectTagsAsync(statObjectArgs);


                            getObjectArgs = new GetObjectArgs()
                                .WithBucket("profile-backgrounds")
                                .WithObject(fileHash)
                                .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(fileStream, token));
                        }

                        if (metadata != null)
                        {
                            fileName = metadata.Tags?["file-name"] ?? fileHash;
                            await MinioClient.GetObjectAsync(getObjectArgs);
                            length = fileStream.Length;
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

        public async Task<bool> CheckFileExists(string folder, string fileHash)
        {
            try
            {
                switch (_launcherInfo.StorageSettings.StorageType)
                {
                    case StorageType.LocalStorage:
                        var localFileInfo = await _storage.GetAsync<LocalFileInfo>(fileHash).ConfigureAwait(false);

                        return localFileInfo is not null;

                    case StorageType.S3:
                        var fileCheck = new StatObjectArgs()
                            .WithBucket(folder)
                            .WithObject(fileHash);

                        var data = await MinioClient.StatObjectAsync(fileCheck);

                        return data.Size != 0;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                _bugTracker.CaptureException(exception);
                return false;
            }

            return false;
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
