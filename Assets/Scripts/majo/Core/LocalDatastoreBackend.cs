/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: local playerprefs datastore backend with prefixed keys
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace majo.Core
{
    /// <summary>
    ///     PlayerPrefs-backed datastore backend that prefixes stored keys.
    /// </summary>
    public sealed class LocalDatastoreBackend : IDatastoreBackend
    {
        private const char KeySeparator = '\n';
        private const string KeyIndexName = "__keys";

        private readonly string _keyPrefix;

        /// <summary>
        ///     creates a local datastore backend.
        /// </summary>
        /// <param name="keyPrefix">prefix added before keys stored in PlayerPrefs</param>
        public LocalDatastoreBackend(string keyPrefix = "majo.")
        {
            _keyPrefix = keyPrefix ?? string.Empty;
        }

        private string KeyIndexKey => $"{_keyPrefix}{KeyIndexName}";

        /// <inheritdoc />
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(BuildStoredKey(key));
        }

        /// <inheritdoc />
        public string GetString(string key, string fallback = "")
        {
            return PlayerPrefs.GetString(BuildStoredKey(key), fallback);
        }

        /// <inheritdoc />
        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(BuildStoredKey(key), value ?? string.Empty);
            AddTrackedKey(key);
        }

        /// <inheritdoc />
        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(BuildStoredKey(key));
            RemoveTrackedKey(key);
        }

        /// <inheritdoc />
        public void DeleteAll()
        {
            foreach (var key in LoadTrackedKeys()) PlayerPrefs.DeleteKey(BuildStoredKey(key));

            PlayerPrefs.DeleteKey(KeyIndexKey);
        }

        /// <inheritdoc />
        public void Save()
        {
            PlayerPrefs.Save();
        }

        private string BuildStoredKey(string key)
        {
            ValidateKey(key);
            return $"{_keyPrefix}{key}";
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("datastore key cannot be empty", nameof(key));

            if (key.IndexOf(KeySeparator) >= 0)
                throw new ArgumentException("datastore key cannot contain newline characters", nameof(key));
        }

        private void AddTrackedKey(string key)
        {
            var trackedKeys = LoadTrackedKeys();
            if (trackedKeys.Contains(key)) return;

            trackedKeys.Add(key);
            SaveTrackedKeys(trackedKeys);
        }

        private void RemoveTrackedKey(string key)
        {
            var trackedKeys = LoadTrackedKeys();
            if (!trackedKeys.Remove(key)) return;

            SaveTrackedKeys(trackedKeys);
        }

        private List<string> LoadTrackedKeys()
        {
            var storedKeys = PlayerPrefs.GetString(KeyIndexKey, string.Empty);
            var trackedKeys = new List<string>();

            if (string.IsNullOrEmpty(storedKeys)) return trackedKeys;

            foreach (var key in storedKeys.Split(KeySeparator))
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                trackedKeys.Add(key);
            }

            return trackedKeys;
        }

        private void SaveTrackedKeys(List<string> trackedKeys)
        {
            if (trackedKeys.Count == 0)
            {
                PlayerPrefs.DeleteKey(KeyIndexKey);
                return;
            }

            PlayerPrefs.SetString(KeyIndexKey, string.Join(KeySeparator.ToString(), trackedKeys));
        }
    }
}