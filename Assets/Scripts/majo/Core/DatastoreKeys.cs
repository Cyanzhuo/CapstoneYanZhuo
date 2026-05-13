/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: reusable datastore key constants for core systems
 */

namespace majo.Core
{
    /// <summary>
    ///     common reusable datastore keys for core systems.
    /// </summary>
    public static class DatastoreKeys
    {
        /// <summary>
        ///     master audio volume key.
        /// </summary>
        public const string MasterVolume = "audio.masterVolume";

        /// <summary>
        ///     music audio volume key.
        /// </summary>
        public const string MusicVolume = "audio.musicVolume";

        /// <summary>
        ///     effects audio volume key.
        /// </summary>
        public const string EffectsVolume = "audio.effectsVolume";

        /// <summary>
        ///     ambience audio volume key.
        /// </summary>
        public const string AmbienceVolume = "audio.ambienceVolume";

        /// <summary>
        ///     user interface audio volume key.
        /// </summary>
        public const string UiVolume = "audio.uiVolume";

        /// <summary>
        ///     last loaded scene key.
        /// </summary>
        public const string LastScene = "game.lastScene";
    }
}