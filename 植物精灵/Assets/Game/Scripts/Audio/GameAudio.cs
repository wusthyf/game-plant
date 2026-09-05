using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace PlantSpirit.GGJ
{
    public enum AudioChannel { Master, Music, Sfx }

    public enum AudioCue
    {
        UiClick,
        PlayerJump,
        PlayerAttackSwing,
        PlayerAttackHit,
        PlayerHurt,
        PlayerDeath,
        PlayerPickup,
        GraftConfirm,
        PoisonCast,
        VineSwing,
        EnemyVineTelegraph,
        EnemyMushroomShoot,
        EnemyBeetleCharge,
        EnemyHurt,
        PortalGrow
    }

    public static class GameAudioSettings
    {
        private const string MasterKey = "audio.master";
        private const string MusicKey = "audio.music";
        private const string SfxKey = "audio.sfx";

        public static event Action Changed;

        public static float Get(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master: return PlayerPrefs.GetFloat(MasterKey, .8f);
                case AudioChannel.Music: return PlayerPrefs.GetFloat(MusicKey, .7f);
                default: return PlayerPrefs.GetFloat(SfxKey, .9f);
            }
        }

        public static void Set(AudioChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case AudioChannel.Master: PlayerPrefs.SetFloat(MasterKey, value); break;
                case AudioChannel.Music: PlayerPrefs.SetFloat(MusicKey, value); break;
                default: PlayerPrefs.SetFloat(SfxKey, value); break;
            }
            Changed?.Invoke();
        }

        public static float ToDecibels(float normalized)
        {
            return normalized <= .0001f ? -80f : Mathf.Log10(Mathf.Clamp01(normalized)) * 20f;
        }

        public static void Save() => PlayerPrefs.Save();
    }

    public sealed class GameAudio : MonoBehaviour
    {
        private const string AudioRoot = "PlantSpirit/Audio";
        private const string MixerResource = AudioRoot + "/PlantSpiritAudioMixer";
        private const string SfxResource = AudioRoot + "/SFX";
        private const string MenuMusicResource = AudioRoot + "/Music/menu_music";
        private const string LevelMusicResource = AudioRoot + "/Music/level_music";

        private static readonly Dictionary<AudioCue, string> CueNames = new Dictionary<AudioCue, string>
        {
            { AudioCue.UiClick, "ui_click" },
            { AudioCue.PlayerJump, "player_jump" },
            { AudioCue.PlayerAttackSwing, "player_attack_swing" },
            { AudioCue.PlayerAttackHit, "player_attack_hit" },
            { AudioCue.PlayerHurt, "player_hurt" },
            { AudioCue.PlayerDeath, "player_death" },
            { AudioCue.PlayerPickup, "player_pickup" },
            { AudioCue.GraftConfirm, "graft_confirm" },
            { AudioCue.PoisonCast, "poison_cast" },
            { AudioCue.VineSwing, "vine_swing" },
            { AudioCue.EnemyVineTelegraph, "enemy_vine_telegraph" },
            { AudioCue.EnemyMushroomShoot, "enemy_mushroom_shoot" },
            { AudioCue.EnemyBeetleCharge, "enemy_beetle_charge" },
            { AudioCue.EnemyHurt, "enemy_hurt_shared" },
            { AudioCue.PortalGrow, "portal_grow" }
        };

        // The source clips have very different transient shapes. Per-cue gains keep
        // quiet cues audible without making the already-loud cues clip when layered.
        private static readonly Dictionary<AudioCue, float> CueGains = new Dictionary<AudioCue, float>
        {
            { AudioCue.UiClick, .8f },
            { AudioCue.PlayerJump, 1.1f },
            { AudioCue.PlayerAttackSwing, 1f },
            { AudioCue.PlayerAttackHit, 1.2f },
            { AudioCue.PlayerHurt, .72f },
            { AudioCue.PlayerDeath, .5f },
            { AudioCue.PlayerPickup, 1f },
            { AudioCue.GraftConfirm, .75f },
            { AudioCue.PoisonCast, 1.15f },
            { AudioCue.VineSwing, 1.1f },
            { AudioCue.EnemyVineTelegraph, .72f },
            { AudioCue.EnemyMushroomShoot, .9f },
            { AudioCue.EnemyBeetleCharge, 1f },
            { AudioCue.EnemyHurt, 1.15f },
            { AudioCue.PortalGrow, 3.2f }
        };

        public static GameAudio Instance { get; private set; }
        public static bool Ready => Instance != null && Instance.sfx.Count == CueNames.Count;
        public static bool MusicReady => Instance != null && Instance.menuMusic != null && Instance.levelMusic != null;
        public bool MixerSettingsApplied { get; private set; }
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
        public bool HasAudioListener => FindObjectOfType<AudioListener>() != null;
        public string CurrentMusicClipName => musicSource != null && musicSource.clip != null ? musicSource.clip.name : string.Empty;

        private readonly Dictionary<AudioCue, AudioClip> sfx = new Dictionary<AudioCue, AudioClip>();
        private readonly Dictionary<AudioCue, float> lastPlayedAt = new Dictionary<AudioCue, float>();
        private AudioMixer mixer;
        private AudioMixerGroup musicGroup;
        private AudioMixerGroup sfxGroup;
        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioClip menuMusic;
        private AudioClip levelMusic;

        public static GameAudio Ensure()
        {
            if (Instance != null) return Instance;
            GameAudio existing = FindObjectOfType<GameAudio>();
            if (existing != null) return existing;
            return new GameObject("GameAudio").AddComponent<GameAudio>();
        }

        public static void Play(AudioCue cue)
        {
            if (!Application.isPlaying) return;
            Ensure().PlayInternal(cue);
        }

        public static bool IsCueAvailable(AudioCue cue)
        {
            return Instance != null && Instance.sfx.ContainsKey(cue);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Debug.Log("[PlantSpiritAudio] Initializing audio service.");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioListener();
            LoadResources();
            Debug.Log("[PlantSpiritAudio] Audio resources loaded: " + sfx.Count + " SFX, menu=" + (menuMusic != null) + ", level=" + (levelMusic != null) + ".");
            GameAudioSettings.Changed += ApplySettings;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySettings();
        }

        private void Start() => PlayMusicForScene(SceneManager.GetActiveScene().name);

        private void OnDestroy()
        {
            if (Instance != this) return;
            GameAudioSettings.Changed -= ApplySettings;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameAudioSettings.Save();
            Instance = null;
        }

        private void LoadResources()
        {
            Debug.Log("[PlantSpiritAudio] Loading mixer.");
            mixer = Resources.Load<AudioMixer>(MixerResource);
            musicSource = CreateSource(true);
            sfxSource = CreateSource(false);

            if (mixer != null)
            {
                AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");
                AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
                if (musicGroups.Length > 0) musicGroup = musicGroups[0];
                if (sfxGroups.Length > 0) sfxGroup = sfxGroups[0];
            }

            Debug.Log("[PlantSpiritAudio] Loading music clips.");
            menuMusic = Resources.Load<AudioClip>(MenuMusicResource);
            levelMusic = Resources.Load<AudioClip>(LevelMusicResource);

            Debug.Log("[PlantSpiritAudio] Loading SFX clips.");
            AudioClip[] clips = Resources.LoadAll<AudioClip>(SfxResource);
            var clipsByName = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (AudioClip clip in clips) clipsByName[clip.name] = clip;
            foreach (KeyValuePair<AudioCue, string> cue in CueNames)
                if (clipsByName.TryGetValue(cue.Value, out AudioClip clip)) sfx[cue.Key] = clip;

            if (menuMusic == null) Debug.LogError("Missing menu music at Resources/" + MenuMusicResource + ".");
            if (levelMusic == null) Debug.LogError("Missing level music at Resources/" + LevelMusicResource + ".");
            foreach (KeyValuePair<AudioCue, string> cue in CueNames)
                if (!sfx.ContainsKey(cue.Key)) Debug.LogError("Missing audio cue at Resources/" + SfxResource + "/" + cue.Value + ".");
        }

        private void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>() != null) return;
            gameObject.AddComponent<AudioListener>();
            Debug.Log("[PlantSpiritAudio] No scene AudioListener was found; attached a persistent listener to GameAudio.");
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = loop ? 96 : 64;
            return source;
        }

        private void ApplySettings()
        {
            float master = GameAudioSettings.Get(AudioChannel.Master);
            float music = GameAudioSettings.Get(AudioChannel.Music);
            float effects = GameAudioSettings.Get(AudioChannel.Sfx);
            if (mixer != null)
            {
                bool masterApplied = mixer.SetFloat("MasterVolume", GameAudioSettings.ToDecibels(master));
                bool musicApplied = mixer.SetFloat("MusicVolume", GameAudioSettings.ToDecibels(music));
                bool sfxApplied = mixer.SetFloat("SfxVolume", GameAudioSettings.ToDecibels(effects));
                MixerSettingsApplied = masterApplied && musicApplied && sfxApplied;
                if (MixerSettingsApplied)
                {
                    musicSource.outputAudioMixerGroup = musicGroup;
                    sfxSource.outputAudioMixerGroup = sfxGroup;
                    AudioListener.volume = 1f;
                    musicSource.volume = 1f;
                    sfxSource.volume = 1f;
                    return;
                }
            }

            // A malformed or unavailable mixer must not make the sliders ineffective.
            musicSource.outputAudioMixerGroup = null;
            sfxSource.outputAudioMixerGroup = null;
            MixerSettingsApplied = false;
            AudioListener.volume = master;
            musicSource.volume = music;
            sfxSource.volume = effects;
        }

        private void PlayInternal(AudioCue cue)
        {
            if (sfxSource == null || !sfx.TryGetValue(cue, out AudioClip clip)) return;
            float now = Time.unscaledTime;
            if (lastPlayedAt.TryGetValue(cue, out float previous) && now - previous < .035f) return;
            lastPlayedAt[cue] = now;
            float gain = CueGains.TryGetValue(cue, out float configuredGain) ? configuredGain : 1f;
            sfxSource.PlayOneShot(clip, gain);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusicForScene(scene.name);

        private void PlayMusicForScene(string sceneName)
        {
            if (musicSource == null) return;
            AudioClip next = sceneName == "MainMenu" ? menuMusic : sceneName == "Level01" ? levelMusic : null;
            if (musicSource.clip == next) return;
            musicSource.Stop();
            musicSource.clip = next;
            if (next != null) musicSource.Play();
        }
    }
}
