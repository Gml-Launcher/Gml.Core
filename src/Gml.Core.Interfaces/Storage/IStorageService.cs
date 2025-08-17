using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Sentry;

namespace GmlCore.Interfaces.Storage
{
    /// <summary>
    ///     Represents a service for managing local storage.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        ///     Sets the value of an item in the local storage asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>An asynchronous operation representing the completion of the set operation.</returns>
        Task SetAsync<T>(string key, T? value);

        /// <summary>
        ///     Gets the value of an item from the local storage asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns>An asynchronous operation that yields the retrieved value.</returns>
        Task<T?> GetAsync<T>(string key, JsonSerializerOptions? jsonSerializerOptions = null);

        Task<T?> GetUserAsync<T>(string login, JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        ///     Saves a record in local storage asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="record">The record to store.</param>
        /// <returns>An asynchronous operation that yields the number of records saved.</returns>
        Task<int> SaveRecord<T>(T record);

        /// <summary>
        /// Asynchronously retrieves a user by their username from the storage.
        /// </summary>
        /// <typeparam name="T">The type of the user to retrieve.</typeparam>
        /// <param name="userName">The username of the user to retrieve.</param>
        /// <param name="jsonSerializerOptions">The JSON serializer options used to deserialize the user data.</param>
        /// <returns>An asynchronous operation that returns the user if found, otherwise null.</returns>
        Task<T?> GetUserByNameAsync<T>(string userName, JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        /// Retrieves a user object associated with the given access token.
        /// </summary>
        /// <typeparam name="T">The type of the user object to retrieve.</typeparam>
        /// <param name="accessToken">The access token used to uniquely identify the user.</param>
        /// <param name="jsonSerializerOptions">The JSON serialization options used to deserialize the stored user object.</param>
        /// <returns>An asynchronous operation containing the user object of type <typeparamref name="T"/> or null if not found.</returns>
        Task<T?> GetUserByAccessToken<T>(string accessToken, JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        /// Retrieves a user by their UUID asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the user to retrieve.</typeparam>
        /// <param name="uuid">The universally unique identifier (UUID) of the user.</param>
        /// <param name="jsonSerializerOptions">The options to use for JSON deserialization.</param>
        /// <returns>An asynchronous operation containing the user data if found; otherwise, <c>null</c>.</returns>
        Task<T?> GetUserByUuidAsync<T>(string uuid, JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        /// Retrieves a user asynchronously using the specified cloak GUID.
        /// </summary>
        /// <typeparam name="T">The type of the user to retrieve.</typeparam>
        /// <param name="guid">The cloak GUID associated with the user.</param>
        /// <param name="jsonSerializerOptions">The options to use when deserializing the user data.</param>
        /// <returns>An asynchronous operation that returns the user object or null if not found.</returns>
        Task<T?> GetUserByCloakAsync<T>(string guid, JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        /// Retrieves a user asynchronously based on their skin identifier.
        /// </summary>
        /// <typeparam name="T">The type of the user to return.</typeparam>
        /// <param name="guid">The skin identifier associated with the user.</param>
        /// <param name="jsonSerializerOptions">The options for customizing JSON serialization.</param>
        /// <returns>An asynchronous operation that returns the user matching the skin identifier, or null if not found.</returns>
        Task<T?> GetUserBySkinAsync<T>(string guid, JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        /// Sets the user data asynchronously in the local storage.
        /// </summary>
        /// <typeparam name="T">The type of the user data.</typeparam>
        /// <param name="login">The login associated with the user.</param>
        /// <param name="uuid">The unique identifier (UUID) of the user.</param>
        /// <param name="value">The user data to store.</param>
        /// <returns>An asynchronous operation representing the completion of the set operation.</returns>
        Task SetUserAsync<T>(string login, string uuid, T value);

        /// <summary>
        /// Retrieves a collection of users asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the users.</typeparam>
        /// <param name="jsonSerializerOptions">Options for JSON serialization and deserialization.</param>
        /// <returns>An asynchronous operation representing a collection of users.</returns>
        Task<IEnumerable<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions);

        /// <summary>
        /// Retrieves a collection of users asynchronously based on their UUIDs.
        /// </summary>
        /// <typeparam name="T">The type representing the user data.</typeparam>
        /// <param name="jsonSerializerOptions">The options used for JSON serialization and deserialization.</param>
        /// <param name="userUuids">The collection of UUIDs identifying the users to retrieve.</param>
        /// <returns>A task representing the asynchronous operation, with a result containing a collection of users.</returns>
        Task<IEnumerable<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions,
            IEnumerable<string> userUuids);

        /// <summary>
        /// Retrieves a collection of users asynchronously from the storage with pagination and search functionality.
        /// </summary>
        /// <typeparam name="T">The type of the users to retrieve.</typeparam>
        /// <param name="jsonSerializerOptions">The JSON serialization options for deserializing the user data.</param>
        /// <param name="take">The maximum number of users to retrieve.</param>
        /// <param name="offset">The number of users to skip for pagination purposes.</param>
        /// <param name="findName">The name or partial name to search for.</param>
        /// <returns>An asynchronous operation that returns a collection of users.</returns>
        Task<ICollection<T>> GetUsersAsync<T>(JsonSerializerOptions jsonSerializerOptions, int take, int offset,
            string findName);

        /// <summary>
        /// Adds a bug report to the storage asynchronously.
        /// </summary>
        /// <param name="bugInfo">The bug information to be added.</param>
        /// <returns>An asynchronous operation representing the completion of the add operation.</returns>
        Task AddBugAsync(IBugInfo bugInfo);

        /// <summary>
        /// Clears all bugs from the storage asynchronously by dropping and recreating the bug table.
        /// </summary>
        /// <returns>An asynchronous operation representing the completion of the clear operation.</returns>
        Task ClearBugsAsync();

        /// <summary>
        /// Retrieves a collection of bugs from the storage asynchronously.
        /// </summary>
        /// <typeparam name="T">The type representing the bug information.</typeparam>
        /// <returns>A task that represents the asynchronous operation, containing the collection of bugs.</returns>
        Task<IEnumerable<T>> GetBugsAsync<T>();

        /// <summary>
        /// Retrieves bug information by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the bug to retrieve.</param>
        /// <returns>An asynchronous operation that returns the retrieved bug information if found; otherwise, null.</returns>
        Task<IBugInfo?> GetBugIdAsync(Guid id);

        /// <summary>
        /// Retrieves a filtered list of bugs from the storage asynchronously based on a specified filter condition.
        /// </summary>
        /// <param name="filter">An expression that defines the filtering condition for the bugs.</param>
        /// <returns>An asynchronous operation that returns an enumerable collection of bugs matching the specified filter.</returns>
        Task<IEnumerable<IBugInfo>> GetFilteredBugsAsync(Expression<Func<IStorageBug, bool>> filter);

        /// <summary>
        /// Removes a user from the storage asynchronously using their UUID.
        /// </summary>
        /// <param name="userUuid">The UUID of the user to remove.</param>
        /// <returns>An asynchronous operation representing the completion of the removal process.</returns>
        Task RemoveUserByUuidAsync(string userUuid);
    }
}
