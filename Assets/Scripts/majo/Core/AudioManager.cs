/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: central audio channel manager with persisted volume settings
 */

using UnityEngine;

namespace majo.Core
{
    /// <summary>
    ///     central audio manager with music, effects, ambience, and user interface channels.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _effectsSource;
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private AudioSource _uiSource;

        private Datastore _datastore;
        private bool _isInitialised;

        /// <summary>
        ///     master audio volume in the linear 0..1 range.
        /// </summary>
        public float MasterVolume { get; private set; } = 1f;

        /// <summary>
        ///     music audio volume in the linear 0..1 range.
        /// </summary>
        public float MusicVolume { get; private set; } = 1f;

        /// <summary>
        ///     effects audio volume in the linear 0..1 range.
        /// </summary>
        public float EffectsVolume { get; private set; } = 1f;

        /// <summary>
        ///     ambience audio volume in the linear 0..1 range.
        /// </summary>
        public float AmbienceVolume { get; private set; } = 1f;

        /// <summary>
        ///     user interface audio volume in the linear 0..1 range.
        /// </summary>
        public float UiVolume { get; private set; } = 1f;

        private void Awake()
        {
            InitialiseAudioSources();
            ApplyVolumes();
        }

        private void Start()
        {
            if (_isInitialised || GameManager.Instance == null) return;

            Initialise(GameManager.Instance.Datastore);
        }

        /// <summary>
        ///     plays music on the music channel.
        /// </summary>
        /// <param name="clip">music clip to play</param>
        /// <param name="loop">true to loop the clip</param>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null)
            {
                Logkat.Dev("AudioManager: no music clip assigned, skipping");
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        /// <summary>
        ///     stops music playback.
        /// </summary>
        public void StopMusic()
        {
            _musicSource.Stop();
        }

        /// <summary>
        ///     pauses music playback.
        /// </summary>
        public void PauseMusic()
        {
            _musicSource.Pause();
        }

        /// <summary>
        ///     resumes paused music playback.
        /// </summary>
        public void ResumeMusic()
        {
            _musicSource.UnPause();
        }

        /// <summary>
        ///     plays a one-shot effect on the effects channel.
        /// </summary>
        /// <param name="clip">effect clip to play</param>
        /// <param name="volumeScale">extra per-playback volume multiplier</param>
        public void PlayEffect(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                Logkat.Dev("AudioManager: no effect clip assigned, skipping");
                return;
            }

            _effectsSource.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
        }

        /// <summary>
        ///     plays a positional one-shot effect.
        /// </summary>
        /// <param name="clip">effect clip to play</param>
        /// <param name="position">world position to play the effect at</param>
        /// <param name="volumeScale">extra per-playback volume multiplier</param>
        public void PlayEffectAt(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null)
            {
                Logkat.Dev("AudioManager: no positional effect clip assigned, skipping");
                return;
            }

            var volume = LinearToLogarithmic(MasterVolume * EffectsVolume) * Mathf.Max(0f, volumeScale);
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        /// <summary>
        ///     plays ambience on the ambience channel.
        /// </summary>
        /// <param name="clip">ambience clip to play</param>
        /// <param name="loop">true to loop the clip</param>
        public void PlayAmbience(AudioClip clip, bool loop = true)
        {
            if (clip == null)
            {
                Logkat.Dev("AudioManager: no ambience clip assigned, skipping");
                return;
            }

            _ambienceSource.clip = clip;
            _ambienceSource.loop = loop;
            _ambienceSource.Play();
        }

        /// <summary>
        ///     stops ambience playback.
        /// </summary>
        public void StopAmbience()
        {
            _ambienceSource.Stop();
        }

        /// <summary>
        ///     plays a one-shot user interface sound on the user interface channel.
        /// </summary>
        /// <param name="clip">user interface clip to play</param>
        /// <param name="volumeScale">extra per-playback volume multiplier</param>
        public void PlayUi(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                Logkat.Dev("AudioManager: no ui clip assigned, skipping");
                return;
            }

            _uiSource.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
        }

        /// <summary>
        ///     sets and persists the master volume.
        /// </summary>
        /// <param name="value">volume value in the linear 0..1 range</param>
        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            SaveVolume(DatastoreKeys.MasterVolume, MasterVolume);
            ApplyVolumes();
        }

        /// <summary>
        ///     sets and persists the music volume.
        /// </summary>
        /// <param name="value">volume value in the linear 0..1 range</param>
        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            SaveVolume(DatastoreKeys.MusicVolume, MusicVolume);
            ApplyVolumes();
        }

        /// <summary>
        ///     sets and persists the effects volume.
        /// </summary>
        /// <param name="value">volume value in the linear 0..1 range</param>
        public void SetEffectsVolume(float value)
        {
            EffectsVolume = Mathf.Clamp01(value);
            SaveVolume(DatastoreKeys.EffectsVolume, EffectsVolume);
            ApplyVolumes();
        }

        /// <summary>
        ///     sets and persists the ambience volume.
        /// </summary>
        /// <param name="value">volume value in the linear 0..1 range</param>
        public void SetAmbienceVolume(float value)
        {
            AmbienceVolume = Mathf.Clamp01(value);
            SaveVolume(DatastoreKeys.AmbienceVolume, AmbienceVolume);
            ApplyVolumes();
        }

        /// <summary>
        ///     sets and persists the user interface volume.
        /// </summary>
        /// <param name="value">volume value in the linear 0..1 range</param>
        public void SetUiVolume(float value)
        {
            UiVolume = Mathf.Clamp01(value);
            SaveVolume(DatastoreKeys.UiVolume, UiVolume);
            ApplyVolumes();
        }

        internal void Initialise(Datastore datastore)
        {
            InitialiseAudioSources();
            _datastore = datastore;
            LoadVolumes();
            ApplyVolumes();
            _isInitialised = true;
        }

        private void InitialiseAudioSources()
        {
            _musicSource = GetOrCreateAudioSource(_musicSource, "MusicSource");
            _effectsSource = GetOrCreateAudioSource(_effectsSource, "EffectsSource");
            _ambienceSource = GetOrCreateAudioSource(_ambienceSource, "AmbienceSource");
            _uiSource = GetOrCreateAudioSource(_uiSource, "UiSource");
        }

        private AudioSource GetOrCreateAudioSource(AudioSource source, string sourceName)
        {
            if (source != null) return ConfigureAudioSource(source);

            var child = transform.Find(sourceName);
            if (child == null)
            {
                var sourceObject = new GameObject(sourceName);
                sourceObject.transform.SetParent(transform, false);
                child = sourceObject.transform;
            }

            if (!child.TryGetComponent(out source)) source = child.gameObject.AddComponent<AudioSource>();

            return ConfigureAudioSource(source);
        }

        private static AudioSource ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void LoadVolumes()
        {
            if (_datastore == null) return;

            MasterVolume = Mathf.Clamp01(_datastore.GetFloat(DatastoreKeys.MasterVolume, 1f));
            MusicVolume = Mathf.Clamp01(_datastore.GetFloat(DatastoreKeys.MusicVolume, 1f));
            EffectsVolume = Mathf.Clamp01(_datastore.GetFloat(DatastoreKeys.EffectsVolume, 1f));
            AmbienceVolume = Mathf.Clamp01(_datastore.GetFloat(DatastoreKeys.AmbienceVolume, 1f));
            UiVolume = Mathf.Clamp01(_datastore.GetFloat(DatastoreKeys.UiVolume, 1f));
        }

        private void SaveVolume(string key, float value)
        {
            if (_datastore == null) return;

            _datastore.SetFloat(key, value);
        }

        private void ApplyVolumes()
        {
            _musicSource.volume = LinearToLogarithmic(MasterVolume * MusicVolume);
            _effectsSource.volume = LinearToLogarithmic(MasterVolume * EffectsVolume);
            _ambienceSource.volume = LinearToLogarithmic(MasterVolume * AmbienceVolume);
            _uiSource.volume = LinearToLogarithmic(MasterVolume * UiVolume);
        }

        private static float LinearToLogarithmic(float value)
        {
            value = Mathf.Clamp01(value);
            if (value <= 0f) return 0f;

            var decibels = Mathf.Lerp(-80f, 0f, value);
            return Mathf.Pow(10f, decibels / 20f);
        }
    }
}