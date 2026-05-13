/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: raw string backend contract for datastore persistence
 */

namespace majo.Core
{
    /// <summary>
    ///     synchronous raw string backend for datastore persistence.
    /// </summary>
    public interface IDatastoreBackend
    {
        /// <summary>
        ///     checks whether a key exists in the backend.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <returns>true if the key exists</returns>
        bool HasKey(string key);

        /// <summary>
        ///     gets a string value from the backend.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="fallback">value returned if the key is missing</param>
        /// <returns>stored string value, or fallback</returns>
        string GetString(string key, string fallback = "");

        /// <summary>
        ///     stores a string value in the backend.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="value">string value to store</param>
        void SetString(string key, string value);

        /// <summary>
        ///     deletes a key from the backend.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        void DeleteKey(string key);

        /// <summary>
        ///     deletes all keys tracked by the backend.
        /// </summary>
        void DeleteAll();

        /// <summary>
        ///     flushes pending backend writes.
        /// </summary>
        void Save();
    }
}