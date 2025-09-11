using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Models.Converters;
using Gml.Models.Storage;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.News;
using GmlCore.Interfaces.Sentry;
using GmlCore.Interfaces.Storage;
using GmlCore.Interfaces.User;
using Newtonsoft.Json;
using SQLite;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Gml.Core.Services.Storage
{
    public class SqliteStorageService : IStorageService
    {
        private const string DatabaseFileName = "data.db";
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;
        private readonly IGmlSettings _settings;
        private JsonSerializerSettings _bugsConverter;

        public SqliteStorageService(IGmlSettings settings)
        {
            _settings = settings;
            _databasePath = Path.Combine(settings.InstallationDirectory, DatabaseFileName);
            _database = new SQLiteAsyncConnection(_databasePath);

            _bugsConverter = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,

                Converters = new List<JsonConverter> { new MemoryInfoConverter(), new ExceptionReportConverter(), new StackTraceConverter() }
            };

            InitializeTables();
        }

        public async Task SetAsync<T>(string key, T? value)
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                WriteIndented = true
            };

            var serializedValue = JsonSerializer.Serialize(value, options);
            var storageItem = new StorageItem
            {
                Key = key,
                TypeName = typeof(T).FullName,
                Value = serializedValue
            };

            await _database.InsertOrReplaceAsync(storageItem);
        }

        public async Task<T?> GetAsync<T>(string key, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= new JsonSerializerOptions();

            var storageItem = await _database.Table<StorageItem>()
                .Where(si => si.Key == key)
                .FirstOrDefaultAsync();

            return storageItem != null
                ?  JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        public async Task<T?> GetUserAsync<T>(string login, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.Login == login)
                .FirstOrDefaultAsync();

            return storageItem != null
                ? JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        public async Task<T?> GetUserBySkinAsync<T>(string guid, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.SkinGuid == guid)
                .FirstOrDefaultAsync();

            return storageItem != null
                ? JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        public async Task SetUserAsync<T>(string login, string uuid, T value)
        {
            var serializedValue = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
            var storageItem = new UserStorageItem
            {
                Login = login,
                Uuid = uuid,
                SkinGuid = (value as IUser)?.TextureSkinGuid,
                CloakGuid = (value as IUser)?.TextureCloakGuid,
                AccessToken = (value as IUser)?.AccessToken,
                TypeName = typeof(T).FullName,
                Value = serializedValue
            };

            await _database.InsertOrReplaceAsync(storageItem);
        }

        public async Task<IEnumerable<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions)
        {
            var users = (await _database
                .Table<UserStorageItem>()
                .ToListAsync())
                .Select(x => JsonSerializer.Deserialize<T>(x.Value, jsonSerializerOptions));

            return users!;
        }

        public async Task<IEnumerable<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions,
            IEnumerable<string> userUuids)
        {
            var users = (await _database
                    .Table<UserStorageItem>()
                    .Where(c => userUuids.Contains(c.Uuid))
                    .ToListAsync())
                .Select(x => JsonSerializer.Deserialize<T>(x.Value, jsonSerializerOptions));

            return users.OfType<T>();
        }

        public async Task<ICollection<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions, int take,
            int offset, string findName)
        {
            IEnumerable<T> users = (await _database
                    .Table<UserStorageItem>()
                    .Where(c => c.Login.ToLower().Contains(findName.ToLower()))
                    .Take(take)
                    .Skip(offset)
                    .ToListAsync())
                .Select(x => JsonSerializer.Deserialize<T>(x.Value, jsonSerializerOptions))
                .Where(x => x != null)!;

            return users.ToArray();
        }

        public async Task AddBugAsync(IBugInfo bugInfo)
        {
            var serializedValue = JsonSerializer.Serialize(bugInfo, new JsonSerializerOptions { WriteIndented = true });

            var storageItem = new BugItem
            {
                Date = DateTime.Now,
                ProjectType = bugInfo.ProjectType,
                Guid = Guid.Parse(bugInfo.Id),
                Attachment = string.Empty,
                Value = serializedValue
            };

            await _database.InsertAsync(storageItem);
        }

        public async Task ClearBugsAsync()
        {
            await _database.DropTableAsync<BugItem>();
            await _database.CreateTableAsync<BugItem>();
        }

        public async Task<IEnumerable<T>> GetBugsAsync<T>()
        {
            var bugs = await _database
                    .Table<BugItem>()
                    .ToListAsync();

            var listBugs = bugs.Select(x => JsonConvert.DeserializeObject<T>(x.Value,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    Converters = new List<JsonConverter>
                        { new MemoryInfoConverter(), new ExceptionReportConverter(), new StackTraceConverter() }
                }));

            return listBugs!;
        }

        public async Task<IBugInfo?> GetBugIdAsync(Guid id)
        {
            var bug = await _database
                .Table<BugItem>()
                .FirstOrDefaultAsync(c => c.Guid == id);

            return JsonConvert.DeserializeObject<BugInfo>(bug.Value, _bugsConverter);
        }

        public async Task<IEnumerable<IBugInfo>> GetFilteredBugsAsync(Expression<Func<IStorageBug, bool>> filter)
        {
            var parameter = Expression.Parameter(typeof(BugItem), "bug");

            var body = RebindParameter(filter.Body, filter.Parameters[0], parameter);
            var newFilter = Expression.Lambda<Func<BugItem, bool>>(body, parameter);

            var storageItems = await _database.Table<BugItem>()
                .Where(newFilter)
                .ToListAsync();

            if (storageItems is null || !storageItems.Any())
            {
                return [];
            }

            var json = string.Concat("[",string.Join(',', storageItems.Select(c => c.Value)), "]");

            return JsonConvert.DeserializeObject<List<BugInfo>>(json, _bugsConverter) ?? [];
        }

        public async Task RemoveUserByUuidAsync(string userUuid)
        {
            var user = await _database.Table<UserStorageItem>()
                .Where(u => u.Uuid == userUuid)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                await _database.DeleteAsync(user);
            }
        }

        public async Task AddLockedHwid(IHardware hardware)
        {
            var exists = await _database.Table<BannedHardwareItem>()
                .Where(x => x.CpuIdentifier == hardware.CpuIdentifier)
                .Where(x => x.MotherboardIdentifier == hardware.MotherboardIdentifier)
                .Where(x => x.DiskIdentifiers == hardware.DiskIdentifiers)
                .FirstOrDefaultAsync();

            if (exists != null)
            {
                return;
            }

            var storageItem = new BannedHardwareItem
            {
                CpuIdentifier = hardware.CpuIdentifier,
                DiskIdentifiers = hardware.DiskIdentifiers,
                MotherboardIdentifier = hardware.MotherboardIdentifier
            };

            await _database.InsertAsync(storageItem);
        }

        public async Task RemoveLockedHwid(IHardware hardware)
        {
            var query = _database.Table<BannedHardwareItem>()
                .Where(x => x.CpuIdentifier == hardware.CpuIdentifier)
                .Where(x => x.MotherboardIdentifier == hardware.MotherboardIdentifier)
                .Where(x => x.DiskIdentifiers == hardware.DiskIdentifiers);

            var items = await query.ToListAsync();

            foreach (var item in items)
            {
                await _database.DeleteAsync(item);
            }
        }

        public async Task<bool> ContainsLockedHwid(IHardware hardware)
        {
            var exists = await _database.Table<BannedHardwareItem>()
                .Where(x => x.CpuIdentifier == hardware.CpuIdentifier)
                .Where(x => x.MotherboardIdentifier == hardware.MotherboardIdentifier)
                .Where(x => x.DiskIdentifiers == hardware.DiskIdentifiers)
                .FirstOrDefaultAsync();

            return exists != null;
        }

        public async Task AddNewsListenerAsync(INews newsListener)
        {
            var storageNews = await _database.Table<StorageItem>()
                .Where(n => typeof(INews).FullName == newsListener.GetType().FullName)
                .Where(n => n.Key == newsListener.Type.ToString())
                .FirstOrDefaultAsync();

            if (storageNews is null)
            {
                await _database.InsertAsync(storageNews);
            }

            await _database.UpdateAsync(storageNews);
        }

        public async Task RemoveNewsListenerAsync(INews newsListener)
        {
            var storageNews = await _database.Table<StorageItem>()
                .Where(n => typeof(INews).FullName == newsListener.GetType().FullName)
                .Where(n => n.Key == newsListener.Type.ToString())
                .FirstOrDefaultAsync();

            if (storageNews is not null)
            {
                await _database.DeleteAsync(storageNews);
            }
        }

        public async Task<IEnumerable<INews?>> GetNewsListenerAsync()
        {
            var storageNews = await _database.Table<StorageItem>()
                .Where(n => typeof(INews).FullName == n.TypeName)
                .ToListAsync();

            return storageNews.Select(n => JsonSerializer.Deserialize<INews>(n.Value));
        }

        private static Expression RebindParameter(Expression body, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            return new ReplaceParameterVisitor(oldParameter, newParameter).Visit(body);
        }

        public Task<int> SaveRecord<T>(T record)
        {
            return _database.InsertOrReplaceAsync(record);
        }

        public async Task<T?> GetUserByNameAsync<T>(string userName, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.Login == userName)
                .FirstOrDefaultAsync();

            return storageItem != null
                ? JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        public async Task<T?> GetUserByAccessToken<T>(string accessToken, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.AccessToken == accessToken)
                .FirstOrDefaultAsync();

            return storageItem != null
                ? JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        public async Task<T?> GetUserByUuidAsync<T>(string uuid, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.Uuid.ToLower() == uuid.ToLower())
                .FirstOrDefaultAsync();

            return storageItem != null
                ? JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        public async Task<T?> GetUserByCloakAsync<T>(string guid, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.CloakGuid == guid)
                .FirstOrDefaultAsync();

            return storageItem != null
                ? JsonSerializer.Deserialize<T>(storageItem.Value, jsonSerializerOptions)
                : default;
        }

        private void InitializeTables()
        {
            var fileInfo = new FileInfo(_databasePath);

            if (!fileInfo.Directory!.Exists)
                fileInfo.Directory.Create();

            _database.CreateTableAsync<StorageItem>().Wait();
            _database.CreateTableAsync<UserStorageItem>().Wait();
            _database.CreateTableAsync<BugItem>().Wait();
            _database.CreateTableAsync<BannedHardwareItem>().Wait();
        }
    }
}
