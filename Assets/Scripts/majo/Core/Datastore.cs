/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: typed local-first datastore wrapper for reusable game settings
 */

using System;
using System.Globalization;
using UnityEngine;

namespace majo.Core
{
    /// <summary>
    ///     typed local-first key-value store built on a raw string backend.
    /// </summary>
    public sealed class Datastore
    {
        private readonly bool _autoSave;
        private readonly IDatastoreBackend _backend;

        /// <summary>
        ///     creates a datastore wrapper around a raw string backend.
        /// </summary>
        /// <param name="backend">raw string backend used for persistence</param>
        /// <param name="autoSave">true to save after every write</param>
        public Datastore(IDatastoreBackend backend, bool autoSave = true)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _autoSave = autoSave;
        }

        /// <summary>
        ///     fired after a value is written.
        /// </summary>
        public event Action<string> OnValueChanged;

        /// <summary>
        ///     checks whether a key exists in the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <returns>true if the key exists</returns>
        public bool HasKey(string key)
        {
            return _backend.HasKey(key);
        }

        /// <summary>
        ///     deletes a key from the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        public void DeleteKey(string key)
        {
            _backend.DeleteKey(key);
            SaveIfNeeded();
        }

        /// <summary>
        ///     deletes all keys tracked by the datastore backend.
        /// </summary>
        public void DeleteAll()
        {
            _backend.DeleteAll();
            SaveIfNeeded();
        }

        /// <summary>
        ///     flushes pending datastore writes.
        /// </summary>
        public void Save()
        {
            _backend.Save();
        }

        /// <summary>
        ///     gets a string value from the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="fallback">value returned if the key is missing</param>
        /// <returns>stored string value, or fallback</returns>
        public string GetString(string key, string fallback = "")
        {
            return _backend.GetString(key, fallback);
        }

        /// <summary>
        ///     stores a string value in the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="value">string value to store</param>
        public void SetString(string key, string value)
        {
            WriteString(key, value ?? string.Empty);
        }

        /// <summary>
        ///     gets an integer value from the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="fallback">value returned if the key is missing or invalid</param>
        /// <returns>stored integer value, or fallback</returns>
        public int GetInt(string key, int fallback = 0)
        {
            if (!HasKey(key)) return fallback;

            var storedValue = GetString(key);
            if (int.TryParse(storedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;

            Logkat.Warn($"Datastore: failed to parse int key {key}");
            return fallback;
        }

        /// <summary>
        ///     stores an integer value in the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="value">integer value to store</param>
        public void SetInt(string key, int value)
        {
            WriteString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     gets a float value from the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="fallback">value returned if the key is missing or invalid</param>
        /// <returns>stored float value, or fallback</returns>
        public float GetFloat(string key, float fallback = 0f)
        {
            if (!HasKey(key)) return fallback;

            var storedValue = GetString(key);
            if (float.TryParse(storedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;

            Logkat.Warn($"Datastore: failed to parse float key {key}");
            return fallback;
        }

        /// <summary>
        ///     stores a float value in the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="value">float value to store</param>
        public void SetFloat(string key, float value)
        {
            WriteString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     gets a boolean value from the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="fallback">value returned if the key is missing or invalid</param>
        /// <returns>stored boolean value, or fallback</returns>
        public bool GetBool(string key, bool fallback = false)
        {
            if (!HasKey(key)) return fallback;

            var storedValue = GetString(key);

            if (bool.TryParse(storedValue, out var value)) return value;

            if (storedValue == "1") return true;
            if (storedValue == "0") return false;

            Logkat.Warn($"Datastore: failed to parse bool key {key}");
            return fallback;
        }

        /// <summary>
        ///     stores a boolean value in the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="value">boolean value to store</param>
        public void SetBool(string key, bool value)
        {
            WriteString(key, value ? "true" : "false");
        }

        /// <summary>
        ///     gets a JSON value from the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="fallback">value returned if the key is missing or invalid</param>
        /// <returns>deserialised value, or fallback</returns>
        public T GetJson<T>(string key, T fallback)
        {
            if (!HasKey(key)) return fallback;

            var storedValue = GetString(key);
            if (string.IsNullOrWhiteSpace(storedValue)) return fallback;

            try
            {
                var value = JsonUtility.FromJson<T>(storedValue);
                return value is null ? fallback : value;
            }
            catch (Exception exception)
            {
                Logkat.Warn($"Datastore: failed to parse json key {key}: {exception.Message}");
                return fallback;
            }
        }

        /// <summary>
        ///     stores a JSON value in the datastore.
        /// </summary>
        /// <param name="key">datastore key without backend prefix</param>
        /// <param name="value">value to serialise</param>
        public void SetJson<T>(string key, T value)
        {
            try
            {
                WriteString(key, JsonUtility.ToJson(value));
            }
            catch (Exception exception)
            {
                Logkat.Warn($"Datastore: failed to serialise json key {key}: {exception.Message}");
            }
        }

        private void WriteString(string key, string value)
        {
            _backend.SetString(key, value);
            SaveIfNeeded();
            OnValueChanged?.Invoke(key);
        }

        private void SaveIfNeeded()
        {
            if (!_autoSave) return;

            Save();
        }
    }
}