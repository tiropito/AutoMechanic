using System.Collections.Generic;
using UnityEngine;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Менеджер звуков: SFX через пул источников, музыка через один источник.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Источники")]
        [SerializeField] private AudioSource sfxSourcePrefab;
        [SerializeField] private AudioSource musicSource;

        [Header("Звуки")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip fixSound;
        [SerializeField] private AudioClip cashSound;
        [SerializeField] private AudioClip bonusSound;
        [SerializeField] private AudioClip errorSound;

        [Header("Музыка")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Настройки")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.4f;
        private const int MaxPoolSize = 6;

        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        // ==================== ПУБЛИЧНОЕ API ====================

        public void PlayClick() => PlaySFX(clickSound);
        public void PlayFix() => PlaySFX(fixSound);
        public void PlayCash() => PlaySFX(cashSound);
        public void PlayBonus() => PlaySFX(bonusSound);
        public void PlayError() => PlaySFX(errorSound);

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetFreeSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.Play();
        }

        private AudioSource GetFreeSource()
        {
            foreach (var s in _sfxPool)
                if (s != null && !s.isPlaying) return s;

            if (_sfxPool.Count < MaxPoolSize && sfxSourcePrefab != null)
            {
                var newSource = Instantiate(sfxSourcePrefab, transform);
                _sfxPool.Add(newSource);
                return newSource;
            }

            return _sfxPool.Count > 0 ? _sfxPool[0] : null;
        }
    }
}