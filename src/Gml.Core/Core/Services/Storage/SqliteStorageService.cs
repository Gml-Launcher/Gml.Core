using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Core.Launcher;
using Gml.Models.Converters;
using Gml.Models.Storage;
using GmlCore.Interfaces.Launcher;
using Newtonsoft.Json;
using SQLite;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NotImplementedException = System.NotImplementedException;

namespace Gml.Core.Services.Storage
{
    public class SqliteStorageService : IStorageService
    {
        private const string DatabaseFileName = "data.db";
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;
        private readonly IGmlSettings _settings;

        public SqliteStorageService(IGmlSettings settings)
        {
            _settings = settings;
            _databasePath = Path.Combine(settings.InstallationDirectory, DatabaseFileName);
            _database = new SQLiteAsyncConnection(_databasePath);

            InitializeTables();
        }

        public async Task SetAsync<T>(string key, T? value)
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var storageItem = new StorageItem
            {
                Key = key,
                TypeName = typeof(T).FullName,
                Value = serializedValue
            };

            await _database.InsertOrReplaceAsync(storageItem);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var storageItem = await _database.Table<StorageItem>()
                .Where(si => si.Key == key)
                .FirstOrDefaultAsync();

            return storageItem != null
                ?  JsonSerializer.Deserialize<T>(storageItem.Value)
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

        public async Task SetUserAsync<T>(string login, string uuid, T value)
        {
            var serializedValue = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
            var storageItem = new UserStorageItem
            {
                Login = login,
                Uuid = uuid,
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

        public async Task AddBugAsync(IBugInfo bugInfo)
        {
            var serializedValue = JsonSerializer.Serialize(bugInfo, new JsonSerializerOptions { WriteIndented = true });

            var storageItem = new BugItem
            {
                Date = DateTime.Now,
                Attachment = string.Empty,
                Value = serializedValue
            };

            await _database.InsertAsync(storageItem);
        }

        public Task ClearBugsAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<T>> GetBugsAsync<T>()
        {
            var bugs = await _database
                    .Table<BugItem>()
                    .ToListAsync();

            return bugs.Select(x => JsonConvert.DeserializeObject<T>(x.Value, new JsonSerializerSettings { Converters = new List<JsonConverter> { new MemoryInfoConverter(), new ExceptionReportConverter() } }))!;
        }

        public async Task<IBugInfo> GetBugIdAsync(string id)
        {
            var bugs = (await _database
                .Table<BugItem>()
                .ToListAsync())
                .Select(x =>
                {
                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,

                        Converters = new List<JsonConverter> { new MemoryInfoConverter(), new ExceptionReportConverter() }
                    };
                    return JsonConvert.DeserializeObject<BugInfo>(x.Value, settings);
                });

            return bugs.FirstOrDefault(x => x.Id == id);
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

        public async Task<T?> GetUserByUuidAsync<T>(string uuid, JsonSerializerOptions jsonSerializerOptions)
        {
            var storageItem = await _database.Table<UserStorageItem>()
                .Where(si => si.Uuid.ToLower() == uuid.ToLower())
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
        }
    }
}
