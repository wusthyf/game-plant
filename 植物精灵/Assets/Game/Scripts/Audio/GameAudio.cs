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

        public static GameAudio Instance { get; private set; }
        public static bool Ready => Instance != null && Instance.sfx.Count > 0;
        public bool MixerSettingsApplied { get; private set; }

        private readonly Dictionary<AudioCue, AudioClip> sfx = new Dictionary<AudioCue, AudioClip>();
        private AudioMixer mixer;
        private AudioSource musicSource;
        private AudioSource sfxSource;

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
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadResources();
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
            mixer = Resources.Load<AudioMixer>(MixerResource);
            musicSource = CreateSource(true);
            sfxSource = CreateSource(false);

            if (mixer != null)
            {
                AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");
                AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
                if (musicGroups.Length > 0) musicSource.outputAudioMixerGroup = musicGroups[0];
                if (sfxGroups.Length > 0) sfxSource.outputAudioMixerGroup = sfxGroups[0];
            }

            AudioClip[] clips = Resources.LoadAll<AudioClip>(SfxResource);
            var clipsByName = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (AudioClip clip in clips) clipsByName[clip.name] = clip;
            foreach (KeyValuePair<AudioCue, string> cue in CueNames)
                if (clipsByName.TryGetValue(cue.Value, out AudioClip clip)) sfx[cue.Key] = clip;
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplySettings()
        {
            float master = GameAudioSettings.Get(AudioChannel.Master);
            float music = GameAudioSettings.Get(AudioChannel.Music);
            float effects = GameAudioSettings.Get(AudioChannel.Sfx);
            if (mixer != null)
            {
                AudioListener.volume = 1f;
                bool masterApplied = mixer.SetFloat("MasterVolume", GameAudioSettings.ToDecibels(master));
                bool musicApplied = mixer.SetFloat("MusicVolume", GameAudioSettings.ToDecibels(music));
                bool sfxApplied = mixer.SetFloat("SfxVolume", GameAudioSettings.ToDecibels(effects));
                MixerSettingsApplied = masterApplied && musicApplied && sfxApplied;
                musicSource.volume = 1f;
                sfxSource.volume = 1f;
                return;
            }

            MixerSettingsApplied = false;
            AudioListener.volume = master;
            musicSource.volume = music;
            sfxSource.volume = effects;
        }

        private void PlayInternal(AudioCue cue)
        {
            if (sfxSource != null && sfx.TryGetValue(cue, out AudioClip clip)) sfxSource.PlayOneShot(clip);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusicForScene(scene.name);

        private void PlayMusicForScene(string sceneName)
        {
            if (musicSource == null) return;
            string resource = sceneName == "MainMenu" ? MenuMusicResource : sceneName == "Level01" ? LevelMusicResource : string.Empty;
            AudioClip next = string.IsNullOrEmpty(resource) ? null : Resources.Load<AudioClip>(resource);
            if (musicSource.clip == next) return;
            musicSource.Stop();
            musicSource.clip = next;
            if (next != null) musicSource.Play();
        }
    }
}
