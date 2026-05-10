using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class SoundEntry
    {
        public string    name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float     volume = 1f;
        [Range(0.5f, 2f)]
        public float     pitch  = 1f;
        public bool      randomPitch = false;
    }

    [Header("Sound Library")]
// ── Spawn ─────────────────────────────────────
public SoundEntry redSpawn;
public SoundEntry chuckSpawn;
public SoundEntry bombSpawn;

// ── Launch ────────────────────────────────────
public SoundEntry redLaunch;
public SoundEntry chuckLaunch;
public SoundEntry bombLaunch;

// ── Hit ───────────────────────────────────────
public SoundEntry redHit;
public SoundEntry chuckHit;
public SoundEntry bombHit;

// ── Death ─────────────────────────────────────
public SoundEntry redDeath;
public SoundEntry chuckDeath;
public SoundEntry bombDeath;

// ── Abilities ─────────────────────────────────
public SoundEntry chuckBoost;
public SoundEntry bombExplosion;

// ── Pig ───────────────────────────────────────
public SoundEntry pigIdle;
public SoundEntry pigDeath;

// ── Slingshot ─────────────────────────────────
public SoundEntry slingshotDrag;
public SoundEntry slingshotLaunch;

// ── Structures ────────────────────────────────
public SoundEntry glassBreak;
public SoundEntry woodBreak;

    [Header("Music")]
    public AudioClip  backgroundMusic;
    [Range(0f, 1f)]
    public float      musicVolume = 0.4f;

    private AudioSource _musicSource;
    private Dictionary<string, SoundEntry> _map;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build dictionary from named fields
        _map = new Dictionary<string, SoundEntry>();
        // Spawn
Register("RedSpawn",        redSpawn);
Register("ChuckSpawn",      chuckSpawn);
Register("BombSpawn",       bombSpawn);

// Launch
Register("RedLaunch",       redLaunch);
Register("ChuckLaunch",     chuckLaunch);
Register("BombLaunch",      bombLaunch);

// Hit
Register("RedHit",          redHit);
Register("ChuckHit",        chuckHit);
Register("BombHit",         bombHit);

// Death
Register("RedDeath",        redDeath);
Register("ChuckDeath",      chuckDeath);
Register("BombDeath",       bombDeath);

// Abilities
Register("ChuckBoost",      chuckBoost);
Register("BombExplosion",   bombExplosion);

// Pig
Register("PigIdle",         pigIdle);
Register("PigDeath",        pigDeath);

// Slingshot
Register("SlingshotDrag",   slingshotDrag);
Register("SlingshotLaunch", slingshotLaunch);

// Structures
Register("GlassBreak",      glassBreak);
Register("WoodBreak",       woodBreak);

        // Setup music
        _musicSource             = gameObject.AddComponent<AudioSource>();
        _musicSource.clip        = backgroundMusic;
        _musicSource.loop        = true;
        _musicSource.volume      = PlayerPrefs.GetFloat("MusicVolume", 0.7f)
                                   * PlayerPrefs.GetFloat("MasterVolume", 1f);
        _musicSource.playOnAwake = false;

        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        if (backgroundMusic != null && musicOn)
            _musicSource.Play();
    }

    void Register(string key, SoundEntry entry)
    {
        if (entry != null && !string.IsNullOrEmpty(key))
            _map[key] = entry;
    }

    // ── Play by name ──────────────────────────────
    public void Play(string soundName)
    {
        if (!_map.TryGetValue(soundName, out SoundEntry s)) return;
        if (s == null || s.clip == null) return;

        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float sfxVol    = PlayerPrefs.GetFloat("SFXVolume",    1f);

        GameObject  go  = new GameObject($"SFX_{soundName}");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip   = s.clip;
        src.volume = s.volume * sfxVol * masterVol;
        src.pitch  = s.randomPitch
                   ? s.pitch * Random.Range(0.9f, 1.15f)
                   : s.pitch;
        src.Play();
        Destroy(go, s.clip.length + 0.1f);
    }

    // ── Music controls ────────────────────────────
    public void SetMusicVolume(float v)
    {
        if (_musicSource != null)
            _musicSource.volume = v * PlayerPrefs.GetFloat("MasterVolume", 1f);
        PlayerPrefs.SetFloat("MusicVolume", v);
    }

    public void SetSFXVolume(float v) =>
        PlayerPrefs.SetFloat("SFXVolume", v);

    public void ToggleMusic(bool on)
    {
        if (_musicSource == null) return;
        if (on) _musicSource.Play();
        else    _musicSource.Stop();
    }
}