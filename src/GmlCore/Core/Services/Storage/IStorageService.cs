using System.Threading.Tasks;

namespace Gml.Core.Services.Storage
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
        /// <returns>An asynchronous operation that yields the retrieved value.</returns>
        Task<T?> GetAsync<T>(string key);

        Task<T?> GetUserAsync<T>(string login);
        Task SetUserAsync<T>(string login, T value);

        /// <summary>
        ///     Saves a record in local storage asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="record">The record to store.</param>
        /// <returns>An asynchronous operation that yields the number of records saved.</returns>
        Task<int> SaveRecord<T>(T record);
    }
}
