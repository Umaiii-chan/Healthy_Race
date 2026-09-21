using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealthyRace.Audio
{
    /// <summary>
    /// Centralized, lightweight audio manager providing music and sound effect playback,
    /// volume management with PlayerPrefs persistence, and robust procedural clip fallbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        private const string PrefMaster = "HealthyRace_MasterVolume";
        private const string PrefMusic = "HealthyRace_MusicVolume";
        private const string PrefSfx = "HealthyRace_SfxVolume";

        private static AudioService _instance;

        [Header("Sources")]
        [SerializeField]
        private AudioSource _musicSource;

        [SerializeField]
        private AudioSource _sfxSource;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)]
        private float _masterVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _musicVolume = 0.7f;

        [SerializeField, Range(0f, 1f)]
        private float _sfxVolume = 0.8f;

        private readonly Dictionary<AudioClipId, AudioClip> _clipCache = new();

        public static AudioService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioService>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[AudioService]");
                        _instance = go.AddComponent<AudioService>();
                    }
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        public static void ResetInstance()
        {
            if (_instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_instance.gameObject);
                }
                else
                {
                    DestroyImmediate(_instance.gameObject);
                }
                _instance = null;
            }
        }

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public AudioSource MusicSource => _musicSource;
        public AudioSource SfxSource => _sfxSource;
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;
        public AudioClip CurrentMusicClip => _musicSource != null ? _musicSource.clip : null;

        public event Action<float, float, float> VolumeChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            LoadSettings();
            EnsureSources();
            LoadClips();
        }

        public void InitializeSources()
        {
            EnsureSources();
            ApplyVolumes();
        }

        private void EnsureSources()
        {
            if (_musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform, false);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }

            if (_sfxSource == null)
            {
                var sfxObj = new GameObject("SfxSource");
                sfxObj.transform.SetParent(transform, false);
                _sfxSource = sfxObj.AddComponent<AudioSource>();
                _sfxSource.loop = false;
                _sfxSource.playOnAwake = false;
            }

            ApplyVolumes();
        }

        private void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PrefMaster, 1f);
            _musicVolume = PlayerPrefs.GetFloat(PrefMusic, 0.7f);
            _sfxVolume = PlayerPrefs.GetFloat(PrefSfx, 0.8f);
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefMaster, _masterVolume);
            PlayerPrefs.SetFloat(PrefMusic, _musicVolume);
            PlayerPrefs.SetFloat(PrefSfx, _sfxVolume);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveSettings();
            VolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveSettings();
            VolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveSettings();
            VolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        public void SetVolumes(float musicVolume, float sfxVolume)
        {
            _musicVolume = Mathf.Clamp01(musicVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
            ApplyVolumes();
            SaveSettings();
            VolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _masterVolume * _musicVolume;
            }
            if (_sfxSource != null)
            {
                _sfxSource.volume = _masterVolume * _sfxVolume;
            }
        }

        public void PlayMusic(AudioClipId musicId)
        {
            var clip = GetClip(musicId);
            PlayMusic(clip);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            EnsureSources();

            if (_musicSource.clip == clip && _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
            }
        }

        public void PlaySfx(AudioClipId sfxId, float volumeMultiplier = 1f)
        {
            var clip = GetClip(sfxId);
            PlaySfx(clip, volumeMultiplier);
        }

        public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureSources();
            _sfxSource.PlayOneShot(
                clip,
                Mathf.Clamp01(_masterVolume * _sfxVolume * volumeMultiplier)
            );
        }

        public AudioClip GetClip(AudioClipId id)
        {
            if (_clipCache.TryGetValue(id, out var cached) && cached != null)
            {
                return cached;
            }

            var clip = LoadClip(id);
            if (clip == null)
            {
                clip = CreateSyntheticFallback(id);
            }

            _clipCache[id] = clip;
            return clip;
        }

        private void LoadClips()
        {
            foreach (AudioClipId id in Enum.GetValues(typeof(AudioClipId)))
            {
                if (!_clipCache.ContainsKey(id))
                {
                    _clipCache[id] = GetClip(id);
                }
            }
        }

        private AudioClip LoadClip(AudioClipId id)
        {
#if UNITY_EDITOR
            string relativePath = id switch
            {
                AudioClipId.Music_Menu => "Assets/_Game/Audio/Music/mus_menu.wav",
                AudioClipId.Music_Level01 => "Assets/_Game/Audio/Music/mus_level01.wav",
                AudioClipId.Sfx_Jump => "Assets/_Game/Audio/SFX/sfx_jump.wav",
                AudioClipId.Sfx_Step => "Assets/_Game/Audio/SFX/sfx_step.wav",
                AudioClipId.Sfx_PickupHealthy => "Assets/_Game/Audio/SFX/sfx_pickup_healthy.wav",
                AudioClipId.Sfx_PickupUltra => "Assets/_Game/Audio/SFX/sfx_pickup_ultra.wav",
                AudioClipId.Sfx_WellbeingUp => "Assets/_Game/Audio/SFX/sfx_wellbeing_up.wav",
                AudioClipId.Sfx_WellbeingDown => "Assets/_Game/Audio/SFX/sfx_wellbeing_down.wav",
                AudioClipId.Sfx_Fatigue => "Assets/_Game/Audio/SFX/sfx_fatigue.wav",
                AudioClipId.Sfx_Button => "Assets/_Game/Audio/SFX/sfx_button.wav",
                AudioClipId.Sfx_Lever => "Assets/_Game/Audio/SFX/sfx_lever.wav",
                AudioClipId.Sfx_Door => "Assets/_Game/Audio/SFX/sfx_door.wav",
                AudioClipId.Sfx_PuzzleSolved => "Assets/_Game/Audio/SFX/sfx_puzzle_solved.wav",
                AudioClipId.Sfx_Checkpoint => "Assets/_Game/Audio/SFX/sfx_checkpoint.wav",
                AudioClipId.Sfx_Finish => "Assets/_Game/Audio/SFX/sfx_finish.wav",
                AudioClipId.Sfx_Victory => "Assets/_Game/Audio/SFX/sfx_victory.wav",
                AudioClipId.Sfx_Defeat => "Assets/_Game/Audio/SFX/sfx_defeat.wav",
                AudioClipId.Sfx_Tie => "Assets/_Game/Audio/SFX/sfx_tie.wav",
                _ => null,
            };

            if (!string.IsNullOrEmpty(relativePath))
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                if (clip != null)
                {
                    return clip;
                }
            }
#endif
            return null;
        }

        private static AudioClip CreateSyntheticFallback(AudioClipId id)
        {
            int sampleRate = 22050;
            float duration = id switch
            {
                AudioClipId.Music_Menu => 2.0f,
                AudioClipId.Music_Level01 => 2.0f,
                AudioClipId.Sfx_Jump => 0.15f,
                AudioClipId.Sfx_Step => 0.05f,
                AudioClipId.Sfx_PickupHealthy => 0.25f,
                AudioClipId.Sfx_PickupUltra => 0.22f,
                AudioClipId.Sfx_WellbeingUp => 0.3f,
                AudioClipId.Sfx_WellbeingDown => 0.35f,
                AudioClipId.Sfx_Fatigue => 0.4f,
                AudioClipId.Sfx_Button => 0.08f,
                AudioClipId.Sfx_Lever => 0.12f,
                AudioClipId.Sfx_Door => 0.5f,
                AudioClipId.Sfx_PuzzleSolved => 0.6f,
                AudioClipId.Sfx_Checkpoint => 0.4f,
                AudioClipId.Sfx_Finish => 0.7f,
                AudioClipId.Sfx_Victory => 1.0f,
                AudioClipId.Sfx_Defeat => 1.0f,
                AudioClipId.Sfx_Tie => 0.8f,
                _ => 0.2f,
            };

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float baseFreq = id switch
            {
                AudioClipId.Sfx_Jump => 300f,
                AudioClipId.Sfx_Step => 120f,
                AudioClipId.Sfx_PickupHealthy => 700f,
                AudioClipId.Sfx_PickupUltra => 150f,
                AudioClipId.Sfx_WellbeingUp => 500f,
                AudioClipId.Sfx_WellbeingDown => 300f,
                AudioClipId.Sfx_Fatigue => 110f,
                AudioClipId.Sfx_Button => 450f,
                AudioClipId.Sfx_Lever => 350f,
                AudioClipId.Sfx_Door => 90f,
                AudioClipId.Sfx_PuzzleSolved => 650f,
                AudioClipId.Sfx_Checkpoint => 800f,
                AudioClipId.Sfx_Finish => 600f,
                AudioClipId.Sfx_Victory => 650f,
                AudioClipId.Sfx_Defeat => 320f,
                AudioClipId.Sfx_Tie => 500f,
                _ => 440f,
            };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float envelope = 1f - progress;
                samples[i] = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * envelope * 0.25f;
            }

            var audioClip = AudioClip.Create($"Synthetic_{id}", sampleCount, 1, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
