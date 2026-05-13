/*
 * author: mark joshwel
 * date: 17/12/2025
 * description: logger to standardise debug output for the sake of logcat filtering
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace majo.Core
{
    public static class Logkat
    {
        /// <summary>
        ///     whether verbose/development logging is enabled
        /// </summary>
        private const bool VerboseLogging = true;

        /// <summary>
        ///     minimum seconds between identical log messages (spam prevention)
        /// </summary>
        private const float RepeatCooldownSeconds = 1.0f;

        /// <summary>
        ///     cache of recent messages with their last log time
        /// </summary>
        private static readonly Dictionary<string, float> RecentMessages = new();

        /// <summary>
        ///     checks if a message was logged recently (within cooldown period)
        /// </summary>
        private static bool HasRecentlyBeenLogged(string message)
        {
            var currentTime = Time.unscaledTime;

            // check if message exists in cache and is within cooldown
            if (RecentMessages.TryGetValue(message, out var lastTime))
                if (currentTime - lastTime < RepeatCooldownSeconds)
                    return true;

            // update cache with current time
            RecentMessages[message] = currentTime;

            // periodically clean old entries to prevent overgrown memory
            if (RecentMessages.Count > 100)
                CleanOldEntries(currentTime);

            return false;
        }

        /// <summary>
        ///     removes cache entries older than cooldown period
        /// </summary>
        private static void CleanOldEntries(float currentTime)
        {
            var keysToRemove = (from kvp in RecentMessages
                where currentTime - kvp.Value >= RepeatCooldownSeconds
                select kvp.Key).ToList();

            foreach (var key in keysToRemove)
                RecentMessages.Remove(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Out(string message)
        {
            if (HasRecentlyBeenLogged(message)) return;
            Debug.Log($"(Logkat) OUT: {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dev(string message)
        {
            if (!VerboseLogging) return;
            if (HasRecentlyBeenLogged(message)) return;
            Debug.Log($"(Logkat Verbose) DEV: {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Err(string message)
        {
            if (HasRecentlyBeenLogged(message)) return;
            Debug.LogError($"(Logkat) ERROR: {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(string message)
        {
            if (HasRecentlyBeenLogged(message)) return;
            Debug.LogWarning($"(Logkat) WARN: {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Panic(string message)
        {
            // panic always logs - never suppressed
            Debug.LogError($"(Logkat) PANIC: {message}");
            throw new Exception(message);
        }
    }
}