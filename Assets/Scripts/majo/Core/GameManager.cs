/*
 * author: mark joshwel
 * date: 13/5/2026
 * description: persistent core game manager for lifecycle and services
 */

using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace majo.Core
{
    /// <summary>
    ///     persistent core entry point for scene loading, pause state, and reusable services.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>
        ///     currently active game manager singleton.
        /// </summary>
        public static GameManager Instance { get; private set; }

        /// <summary>
        ///     local-first datastore service owned by the game manager.
        /// </summary>
        public Datastore Datastore { get; private set; }

        /// <summary>
        ///     central audio service owned by the game manager.
        /// </summary>
        public AudioManager AudioManager { get; private set; }

        /// <summary>
        ///     true when gameplay time is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     true while an asynchronous scene load is running.
        /// </summary>
        public bool IsLoadingScene { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Datastore = new Datastore(new LocalDatastoreBackend());
            InitialiseAudioManager();

            Logkat.Out("GameManager: awake/setup ok");
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            Time.timeScale = 1f;
            Instance = null;
        }

        /// <summary>
        ///     fired after the pause state changes.
        /// </summary>
        public event Action<bool> OnPauseChanged;

        /// <summary>
        ///     fired before a requested scene starts loading.
        /// </summary>
        public event Action<string> OnSceneLoadStarted;

        /// <summary>
        ///     fired after a requested scene finishes loading.
        /// </summary>
        public event Action<string> OnSceneLoadCompleted;

        /// <summary>
        ///     loads a scene by name while preventing duplicate scene loads.
        /// </summary>
        /// <param name="sceneName">scene name registered in build settings</param>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Logkat.Warn("GameManager: scene name is empty, skipping load");
                return;
            }

            if (IsLoadingScene)
            {
                Logkat.Warn("GameManager: already loading scene");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        ///     reloads the currently active scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        ///     pauses or unpauses gameplay time.
        /// </summary>
        /// <param name="paused">true to pause gameplay time</param>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            OnPauseChanged?.Invoke(paused);
        }

        /// <summary>
        ///     quits the application.
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        private void InitialiseAudioManager()
        {
            AudioManager = GetComponentInChildren<AudioManager>(true);

            if (AudioManager == null)
            {
                var audioManagerObject = new GameObject("AudioManager");
                audioManagerObject.transform.SetParent(transform, false);
                AudioManager = audioManagerObject.AddComponent<AudioManager>();
            }

            AudioManager.Initialise(Datastore);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoadingScene = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            AsyncOperation loadOperation;

            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception exception)
            {
                Logkat.Err($"GameManager: failed to start scene load {sceneName}: {exception.Message}");
                IsLoadingScene = false;
                yield break;
            }

            if (loadOperation == null)
            {
                Logkat.Err($"GameManager: failed to start scene load {sceneName}");
                IsLoadingScene = false;
                yield break;
            }

            while (!loadOperation.isDone) yield return null;

            Datastore.SetString(DatastoreKeys.LastScene, sceneName);
            IsLoadingScene = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
        }
    }
}