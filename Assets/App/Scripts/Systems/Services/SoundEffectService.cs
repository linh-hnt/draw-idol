using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace App.Services
{
    public class SoundEffectService : MonoBehaviour, ISoundEffectService
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";

        [Header("SFX Mapping")]
        [SerializeField] private SFXClipPair[] _sfxClips;

        [Header("Pool Settings")]
        [SerializeField] private int _poolSize = 8;
        [SerializeField] private float _maxVolume = 1f;

        private Queue<AudioSource> _pool;
        private List<AudioSource> _activeSources;
        private AudioMixerGroup _mixerGroup;

        private bool _isInitialized;
        private bool _isMuted;
        private float _currentVolume = 1f;

        private const string MUTE_KEY = "sfx_muted";

        public bool IsInitialized => _isInitialized;
        public bool IsMuted => _isMuted;

        public void Initialize()
        {
            if (_isInitialized) return;

            _pool = new Queue<AudioSource>(_poolSize);
            _activeSources = new List<AudioSource>(_poolSize);

            if (_audioMixer != null)
            {
                var groups = _audioMixer.FindMatchingGroups("SFX");
                _mixerGroup = groups.Length > 0 ? groups[0] : null;
            }

            for (int i = 0; i < _poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                if (_mixerGroup != null) src.outputAudioMixerGroup = _mixerGroup;
                src.volume = 0f;
                _pool.Enqueue(src);
            }

            _isMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
            _isInitialized = true;
        }

        public void Play(SoundFX fx)
        {
            if (!_isInitialized || _isMuted) return;

            AudioClip clip = GetClipForFX(fx);
            if (clip != null)
            {
                PlayClip(clip, _maxVolume);
            }
            else
            {
                Debug.LogWarning($"[SFXService] No clip assigned for SFX: {fx}");
            }
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (!_isInitialized || _isMuted) return;
            if (clip == null) return;

            PlayClip(clip, Mathf.Clamp01(volume) * _maxVolume);
        }

        public void SetVolume(float volume)
        {
            _currentVolume = Mathf.Clamp01(volume);
            ApplyMixerVolume();
        }

        public void SetMute(bool isMute)
        {
            _isMuted = isMute;
            PlayerPrefs.SetInt(MUTE_KEY, isMute ? 1 : 0);
            ApplyMixerVolume();
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            AudioSource src = GetFreeSource();
            if (src == null) return;

            src.clip = clip;
            src.volume = _isMuted ? 0f : volume;
            src.Play();

            _activeSources.Add(src);
            StartCoroutine(ReturnWhenFinished(src));
        }

        private AudioSource GetFreeSource()
        {
            if (_pool.Count > 0) return _pool.Dequeue();

            for (int i = _activeSources.Count - 1; i >= 0; i--)
            {
                if (!_activeSources[i].isPlaying)
                {
                    var src = _activeSources[i];
                    _activeSources.RemoveAt(i);
                    return src;
                }
            }

            if (_activeSources.Count + _pool.Count < _poolSize * 2)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                if (_mixerGroup != null) src.outputAudioMixerGroup = _mixerGroup;
                return src;
            }

            var stolen = _activeSources[0];
            _activeSources.RemoveAt(0);
            stolen.Stop();
            return stolen;
        }

        private IEnumerator ReturnWhenFinished(AudioSource src)
        {
            yield return new WaitWhile(() => src.isPlaying);
            _activeSources.Remove(src);
            _pool.Enqueue(src);
        }

        private void ApplyMixerVolume()
        {
            float vol = _isMuted ? 0f : _currentVolume;
            if (_audioMixer != null)
            {
                float dB = vol > 0.001f ? Mathf.Log10(vol) * 20f : -80f;
                _audioMixer.SetFloat(_sfxVolumeParam, dB);
            }
        }

        private AudioClip GetClipForFX(SoundFX fx)
        {
            if (_sfxClips == null) return null;
            foreach (var pair in _sfxClips)
            {
                if (pair.FX == fx) return pair.Clip;
            }
            return null;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        [Serializable]
        public struct SFXClipPair
        {
            public SoundFX FX;
            public AudioClip Clip;
        }
    }
}
