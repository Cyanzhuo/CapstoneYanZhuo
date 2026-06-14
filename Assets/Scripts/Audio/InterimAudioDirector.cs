/*
 * InterimAudioDirector: scene-placeable routing point for interim sound design clips
 */

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Game.Audio
{
    public enum InterimAudioCue
    {
        None,
        Walk,
        CrouchWalk,
        Run,
        Slide,
        Jump,
        DoubleJump,
        Dash,
        PlayerCrouch,
        Land,
        HeavyLand,
        BasicAttack,
        BasicAttackHit,
        Charge,
        ChargedAttack,
        ChargedAttackHit,
        ChargedCrouchAttack,
        LauncherJump,
        LauncherHit,
        GroundSlamJump,
        GroundSlamHit,
        SpikeSecondJump,
        JumpBack,
        AerialPush,
        GoldPickup,
        GoldPurchase,
        Interact,
        InteractDenied,
        UiHover,
        UiClick,
        Bgm
    }

    public sealed class InterimAudioDirector : MonoBehaviour
    {
        public static InterimAudioDirector Instance { get; private set; }

        [Header("Master Toggle")]
        [SerializeField] private bool audioEnabled = false;
        [SerializeField] private bool keepAliveAcrossScenes = true;
        [SerializeField] private bool autoPlayBgm = false;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float movementVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.55f;
        [SerializeField] private bool playWorldSoundsAtPosition = true;
        [SerializeField] private float duplicateCueWindow = 0.03f;

        [Header("Sources")]
        [SerializeField] private AudioSource movementSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource bgmSource;

        [Header("Movement")]
        [SerializeField] private AudioClip walkLoop;
        [SerializeField] private AudioClip crouchWalkLoop;
        [SerializeField] private AudioClip runLoop;
        [SerializeField] private AudioClip slideLoop;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip doubleJumpClip;
        [SerializeField] private AudioClip dashClip;
        [SerializeField] private AudioClip playerCrouchClip;
        [SerializeField] private AudioClip landClip;
        [SerializeField] private AudioClip heavyLandClip;

        [Header("Moveset")]
        [SerializeField] private AudioClip basicAttackClip;
        [SerializeField] private AudioClip basicAttackHitClip;
        [SerializeField] private AudioClip chargeClip;
        [SerializeField] private AudioClip chargedAttackClip;
        [SerializeField] private AudioClip chargedAttackHitClip;
        [SerializeField] private AudioClip chargedCrouchAttackClip;
        [SerializeField] private AudioClip launcherJumpClip;
        [SerializeField] private AudioClip launcherHitClip;
        [SerializeField] private AudioClip groundSlamJumpClip;
        [SerializeField] private AudioClip groundSlamHitClip;
        [SerializeField] private AudioClip spikeSecondJumpClip;
        [SerializeField] private AudioClip jumpBackClip;
        [SerializeField] private AudioClip aerialPushClip;

        [Header("Interactions")]
        [SerializeField] private AudioClip goldPickupClip;
        [SerializeField] private AudioClip goldPurchaseClip;
        [SerializeField] private AudioClip interactClip;
        [SerializeField] private AudioClip interactDeniedClip;

        [Header("UI")]
        [SerializeField] private AudioClip uiHoverClip;
        [SerializeField] private AudioClip uiClickClip;

        [Header("BGM")]
        [SerializeField] private AudioClip bgmClip;

        private readonly Dictionary<InterimAudioCue, float> lastCueTimes = new Dictionary<InterimAudioCue, float>();
        private float lastMovementReportTime = -99f;
        private const float MovementReportTimeout = 0.18f;

        public bool AudioEnabled => audioEnabled && isActiveAndEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (keepAliveAcrossScenes) DontDestroyOnLoad(gameObject);

            EnsureSources();
            ApplyVolumes();

            if (audioEnabled && autoPlayBgm) PlayBgm();
        }

        private void LateUpdate()
        {
            if (movementSource == null || !movementSource.isPlaying) return;

            if (!AudioEnabled || Time.unscaledTime - lastMovementReportTime > MovementReportTimeout)
            {
                movementSource.Stop();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetAudioEnabled(bool enabled)
        {
            audioEnabled = enabled;

            if (!audioEnabled)
            {
                StopMovementLoop();
                StopBgm();
                return;
            }

            if (autoPlayBgm) PlayBgm();
        }

        public static void ReportPlayerMovement(
            Vector3 position,
            bool isMoving,
            bool isCrouching,
            bool isRunning,
            float speedRatio = 1f
        )
        {
            if (Instance == null) return;

            Instance.PlayMovementLoop(position, isMoving, isCrouching, isRunning, speedRatio);
        }

        public static bool TryPlayPlayerJump(Vector3 position, bool isAirJump)
        {
            return TryPlayWorld(isAirJump ? InterimAudioCue.DoubleJump : InterimAudioCue.Jump, position);
        }

        public static bool TryPlayPlayerDash(Vector3 position)
        {
            return TryPlayWorld(InterimAudioCue.Dash, position);
        }

        public static bool TryPlayPlayerCrouch(Vector3 position)
        {
            return TryPlayWorld(InterimAudioCue.PlayerCrouch, position);
        }

        public static bool TryPlayPlayerLand(Vector3 position, bool heavy = false)
        {
            return TryPlayWorld(heavy ? InterimAudioCue.HeavyLand : InterimAudioCue.Land, position);
        }

        public static bool TryPlayMove(InterimAudioCue cue, Vector3 position, float volumeScale = 1f)
        {
            return TryPlayWorld(cue, position, volumeScale);
        }

        public static bool TryPlayInteraction(InterimAudioCue cue, Vector3 position, float volumeScale = 1f)
        {
            return TryPlayWorld(cue, position, volumeScale);
        }

        public static bool TryPlayGoldPickup(Vector3 position, float volumeScale = 1f)
        {
            return TryPlayInteraction(InterimAudioCue.GoldPickup, position, volumeScale);
        }

        public static bool TryPlayGoldPurchase(Vector3 position, float volumeScale = 1f)
        {
            return TryPlayInteraction(InterimAudioCue.GoldPurchase, position, volumeScale);
        }

        public static bool TryPlayUiHover(float volumeScale = 1f)
        {
            return Instance != null && Instance.PlayOneShot(InterimAudioCue.UiHover, Vector3.zero, volumeScale, false, true);
        }

        public static bool TryPlayUiClick(float volumeScale = 1f)
        {
            return Instance != null && Instance.PlayOneShot(InterimAudioCue.UiClick, Vector3.zero, volumeScale, false, true);
        }

        public static bool TryPlayWorld(InterimAudioCue cue, Vector3 position, float volumeScale = 1f)
        {
            return Instance != null && Instance.PlayOneShot(cue, position, volumeScale, true, false);
        }

        public bool PlayBgm(AudioClip overrideClip = null, bool loop = true)
        {
            if (!AudioEnabled) return false;

            EnsureSources();

            AudioClip clip = overrideClip != null ? overrideClip : bgmClip;
            if (clip == null) return false;

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
            return true;
        }

        public void StopBgm()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        private bool PlayOneShot(
            InterimAudioCue cue,
            Vector3 position,
            float volumeScale,
            bool worldSound,
            bool uiSound
        )
        {
            if (!AudioEnabled || cue == InterimAudioCue.None) return false;

            AudioClip clip = GetClip(cue);
            if (clip == null) return false;

            if (ShouldSuppressDuplicate(cue)) return true;

            EnsureSources();

            float safeVolumeScale = Mathf.Max(0f, volumeScale);

            if (uiSound)
            {
                uiSource.PlayOneShot(clip, safeVolumeScale);
                return true;
            }

            if (worldSound && playWorldSoundsAtPosition)
            {
                AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(masterVolume * sfxVolume) * safeVolumeScale);
                return true;
            }

            sfxSource.PlayOneShot(clip, safeVolumeScale);
            return true;
        }

        private void PlayMovementLoop(
            Vector3 position,
            bool isMoving,
            bool isCrouching,
            bool isRunning,
            float speedRatio
        )
        {
            if (!AudioEnabled || !isMoving)
            {
                StopMovementLoop();
                return;
            }

            EnsureSources();

            InterimAudioCue cue = isCrouching ? InterimAudioCue.CrouchWalk : isRunning ? InterimAudioCue.Run : InterimAudioCue.Walk;
            AudioClip clip = GetClip(cue);

            if (clip == null)
            {
                StopMovementLoop();
                return;
            }

            lastMovementReportTime = Time.unscaledTime;
            movementSource.transform.position = position;
            movementSource.loop = true;
            movementSource.pitch = Mathf.Clamp(speedRatio, 0.75f, 1.35f);

            if (movementSource.clip == clip && movementSource.isPlaying) return;

            movementSource.clip = clip;
            movementSource.Play();
        }

        private void StopMovementLoop()
        {
            if (movementSource != null && movementSource.isPlaying) movementSource.Stop();
        }

        private bool ShouldSuppressDuplicate(InterimAudioCue cue)
        {
            if (duplicateCueWindow <= 0f) return false;

            float now = Time.unscaledTime;

            if (lastCueTimes.TryGetValue(cue, out float lastTime) && now - lastTime < duplicateCueWindow)
            {
                return true;
            }

            lastCueTimes[cue] = now;
            return false;
        }

        private AudioClip GetClip(InterimAudioCue cue)
        {
            switch (cue)
            {
                case InterimAudioCue.Walk:
                    return walkLoop;
                case InterimAudioCue.CrouchWalk:
                    return crouchWalkLoop;
                case InterimAudioCue.Run:
                    return runLoop;
                case InterimAudioCue.Slide:
                    return slideLoop;
                case InterimAudioCue.Jump:
                    return jumpClip;
                case InterimAudioCue.DoubleJump:
                    return doubleJumpClip != null ? doubleJumpClip : jumpClip;
                case InterimAudioCue.Dash:
                    return dashClip;
                case InterimAudioCue.PlayerCrouch:
                    return playerCrouchClip;
                case InterimAudioCue.Land:
                    return landClip != null ? landClip : heavyLandClip;
                case InterimAudioCue.HeavyLand:
                    return heavyLandClip != null ? heavyLandClip : landClip;
                case InterimAudioCue.BasicAttack:
                    return basicAttackClip;
                case InterimAudioCue.BasicAttackHit:
                    return basicAttackHitClip;
                case InterimAudioCue.Charge:
                    return chargeClip;
                case InterimAudioCue.ChargedAttack:
                    return chargedAttackClip != null ? chargedAttackClip : basicAttackClip;
                case InterimAudioCue.ChargedAttackHit:
                    return chargedAttackHitClip != null ? chargedAttackHitClip : basicAttackHitClip;
                case InterimAudioCue.ChargedCrouchAttack:
                    return chargedCrouchAttackClip != null ? chargedCrouchAttackClip : chargedAttackClip;
                case InterimAudioCue.LauncherJump:
                    return launcherJumpClip;
                case InterimAudioCue.LauncherHit:
                    return launcherHitClip != null ? launcherHitClip : basicAttackHitClip;
                case InterimAudioCue.GroundSlamJump:
                    return groundSlamJumpClip != null ? groundSlamJumpClip : launcherJumpClip;
                case InterimAudioCue.GroundSlamHit:
                    return groundSlamHitClip != null ? groundSlamHitClip : launcherHitClip;
                case InterimAudioCue.SpikeSecondJump:
                    return spikeSecondJumpClip != null ? spikeSecondJumpClip : doubleJumpClip;
                case InterimAudioCue.JumpBack:
                    return jumpBackClip;
                case InterimAudioCue.AerialPush:
                    return aerialPushClip;
                case InterimAudioCue.GoldPickup:
                    return goldPickupClip;
                case InterimAudioCue.GoldPurchase:
                    return goldPurchaseClip != null ? goldPurchaseClip : goldPickupClip;
                case InterimAudioCue.Interact:
                    return interactClip;
                case InterimAudioCue.InteractDenied:
                    return interactDeniedClip;
                case InterimAudioCue.UiHover:
                    return uiHoverClip;
                case InterimAudioCue.UiClick:
                    return uiClickClip;
                case InterimAudioCue.Bgm:
                    return bgmClip;
                default:
                    return null;
            }
        }

        private void EnsureSources()
        {
            movementSource = GetOrCreateSource(movementSource, "Movement Source", true, 0.7f);
            sfxSource = GetOrCreateSource(sfxSource, "SFX Source", false, 0f);
            uiSource = GetOrCreateSource(uiSource, "UI Source", false, 0f);
            bgmSource = GetOrCreateSource(bgmSource, "BGM Source", true, 0f);

            ApplyVolumes();
        }

        private AudioSource GetOrCreateSource(AudioSource source, string childName, bool loop, float spatialBlend)
        {
            if (source == null)
            {
                Transform child = transform.Find(childName);

                if (child == null)
                {
                    GameObject sourceObject = new GameObject(childName);
                    sourceObject.transform.SetParent(transform, false);
                    child = sourceObject.transform;
                }

                source = child.GetComponent<AudioSource>();
                if (source == null) source = child.gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = spatialBlend;
            return source;
        }

        private void ApplyVolumes()
        {
            if (movementSource != null) movementSource.volume = Mathf.Clamp01(masterVolume * movementVolume);
            if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(masterVolume * sfxVolume);
            if (uiSource != null) uiSource.volume = Mathf.Clamp01(masterVolume * uiVolume);
            if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(masterVolume * bgmVolume);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            audioEnabled = false;
            GuessInterimClips();
        }

        private void OnValidate()
        {
            GuessInterimClips();
            ApplyVolumes();
        }

        private void GuessInterimClips()
        {
            AssignIfEmpty(ref walkLoop, "Slide");
            AssignIfEmpty(ref crouchWalkLoop, "Player Crouch");
            AssignIfEmpty(ref runLoop, "Slide");
            AssignIfEmpty(ref slideLoop, "Slide");
            AssignIfEmpty(ref jumpClip, "Jump");
            AssignIfEmpty(ref doubleJumpClip, "Spike (Launcher, Jump, Late 2nd Jump) - 2nd Jump");
            AssignIfEmpty(ref dashClip, "Dash");
            AssignIfEmpty(ref playerCrouchClip, "Player Crouch");
            AssignIfEmpty(ref landClip, "Heavy Stone Land");
            AssignIfEmpty(ref heavyLandClip, "Heavy Stone Land");
            AssignIfEmpty(ref basicAttackClip, "Basic Attack");
            AssignIfEmpty(ref basicAttackHitClip, "Hit of Basic Attack");
            AssignIfEmpty(ref chargeClip, "Charge");
            AssignIfEmpty(ref chargedAttackClip, "Charged Crouch Attack");
            AssignIfEmpty(ref chargedAttackHitClip, "Charged Attack Hit");
            AssignIfEmpty(ref chargedCrouchAttackClip, "Charged Crouch Attack");
            AssignIfEmpty(ref launcherJumpClip, "Ground Slam (Launcher, Jump, Early 2nd Jump) - Jump");
            AssignIfEmpty(ref launcherHitClip, "Ground Slam (Launcher, Jump, Early 2nd Jump) - Hit");
            AssignIfEmpty(ref groundSlamJumpClip, "Ground Slam (Launcher, Jump, Early 2nd Jump) - Jump");
            AssignIfEmpty(ref groundSlamHitClip, "Ground Slam (Launcher, Jump, Early 2nd Jump) - Hit");
            AssignIfEmpty(ref spikeSecondJumpClip, "Spike (Launcher, Jump, Late 2nd Jump) - 2nd Jump");
            AssignIfEmpty(ref jumpBackClip, "Jump Back");
            AssignIfEmpty(ref aerialPushClip, "Dash and Jump - Jump and Dash");
            AssignIfEmpty(ref goldPickupClip, "Coin_Wood_Table_Singles_Drop_Spin_Takes_5");
            AssignIfEmpty(ref goldPurchaseClip, "264604 - Stack Coin Bag 04");
            AssignIfEmpty(ref interactClip, "Piano_Ui (1)");
            AssignIfEmpty(ref interactDeniedClip, "Piano_Ui (7)");
            AssignIfEmpty(ref uiHoverClip, "Piano_Ui (1)");
            AssignIfEmpty(ref uiClickClip, "Piano_Ui (7)");
        }

        private static void AssignIfEmpty(ref AudioClip clip, string exactFileName)
        {
            if (clip != null) return;

            clip = FindInterimClip(exactFileName);
        }

        private static AudioClip FindInterimClip(string exactFileName)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Interim" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileNameWithoutExtension(path) == exactFileName)
                {
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            return null;
        }
#endif
    }
}
