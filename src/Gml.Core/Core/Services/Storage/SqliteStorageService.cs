﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gml.Models.Storage;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.User;
using SQLite;

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
        }
    }
}
