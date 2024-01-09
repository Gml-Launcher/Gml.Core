using System;
using System.IO;
using System.Threading.Tasks;
using Gml.Models.Storage;
using GmlCore.Interfaces.Launcher;
using Newtonsoft.Json;
using SQLite;

namespace Gml.Core.Services.Storage
{
    public class SqliteStorageService : IStorageService
    {
        private readonly string _databasePath;
        private readonly IGmlSettings _settings;
        private readonly SQLiteAsyncConnection _database;

        private const string DatabaseFileName = "data.db";


        public SqliteStorageService(IGmlSettings settings)
        {
            _settings = settings;
            _databasePath = Path.Combine(settings.InstallationDirectory, DatabaseFileName);
            _database = new SQLiteAsyncConnection(_databasePath);

            InitializeTables();
        }

        private void InitializeTables()
        {
            var fileInfo = new FileInfo(_databasePath);

            if (!fileInfo.Directory!.Exists)
                fileInfo.Directory.Create();

            _database.CreateTableAsync<StorageItem>().Wait();
        }

        public async Task SetAsync<T>(string key, T value)
        {
            var serializedValue = JsonConvert.SerializeObject(value);
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
                       ? JsonConvert.DeserializeObject<T>(storageItem.Value)
                       : default;
        }

        public Task<int> SaveRecord<T>(T record)
        {
            return _database.InsertOrReplaceAsync(record);
        }
    }
}
