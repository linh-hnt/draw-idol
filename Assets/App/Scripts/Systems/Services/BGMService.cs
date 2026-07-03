using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace App.Services
{
    public class BGMService : MonoBehaviour, IBGMService
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _bgmVolumeParam = "BGMVolume";

        [Header("Track Mapping")]
        [SerializeField] private BGMTrackClipPair[] _trackClips;

        [Header("Settings")]
        [SerializeField] private float _defaultFadeDuration = 0.5f;
        [SerializeField] private float _maxVolume = 0.8f;
        [SerializeField] private bool _autoPlayOnInit = false;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool _useSourceA = true;
        private Coroutine _fadeCoroutine;

        private bool _isInitialized;
        private bool _isMuted;
        private float _currentVolume = 1f;
        private BGMTrack _currentTrack = BGMTrack.None;

        private const string MUTE_KEY = "bgm_muted";

        public bool IsInitialized => _isInitialized;
        public BGMTrack CurrentTrack => _currentTrack;
        public bool IsMuted => _isMuted;

        public void Initialize()
        {
            if (_isInitialized) return;

            CreateAudioSources();
            _isMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;

            _isInitialized = true;

            if (_autoPlayOnInit)
                Play(BGMTrack.MainMenu, 0f);
        }

        private void CreateAudioSources()
        {
            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceB = gameObject.AddComponent<AudioSource>();

            _sourceA.loop = true;
            _sourceB.loop = true;
            _sourceA.playOnAwake = false;
            _sourceB.playOnAwake = false;

            if (_audioMixer != null)
            {
                var groups = _audioMixer.FindMatchingGroups("BGM");
                if (groups.Length > 0)
                {
                    _sourceA.outputAudioMixerGroup = groups[0];
                    _sourceB.outputAudioMixerGroup = groups[0];
                }
            }
        }

        public void Play(BGMTrack track, float fadeDuration = -1f)
        {
            if (!_isInitialized) return;
            if (track == _currentTrack) return;

            if (fadeDuration < 0f) fadeDuration = _defaultFadeDuration;

            AudioClip clip = GetClipForTrack(track);
            if (clip == null)
            {
                Debug.LogWarning($"[BGMService] No clip assigned for track: {track}");
                return;
            }

            AudioSource targetSource = _useSourceA ? _sourceA : _sourceB;
            AudioSource currentSource = _useSourceA ? _sourceB : _sourceA;

            targetSource.clip = clip;
            targetSource.volume = 0f;
            targetSource.Play();

            _useSourceA = !_useSourceA;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossFade(currentSource, targetSource, fadeDuration));

            _currentTrack = track;
        }

        public void Stop(float fadeDuration = -1f)
        {
            if (fadeDuration < 0f) fadeDuration = _defaultFadeDuration;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutAll(fadeDuration));

            _currentTrack = BGMTrack.None;
        }

        public void Pause()
        {
            _sourceA.Pause();
            _sourceB.Pause();
        }

        public void Resume()
        {
            AudioSource active = _useSourceA ? _sourceA : _sourceB;
            if (active.clip != null)
                active.UnPause();
        }

        public void SetVolume(float volume)
        {
            _currentVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetMute(bool isMute)
        {
            _isMuted = isMute;
            PlayerPrefs.SetInt(MUTE_KEY, isMute ? 1 : 0);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            float vol = _isMuted ? 0f : _currentVolume * _maxVolume;
            _sourceA.volume = _sourceA.isPlaying ? vol : 0f;
            _sourceB.volume = _sourceB.isPlaying ? vol : 0f;

            if (_audioMixer != null)
            {
                float dB = vol > 0.001f ? Mathf.Log10(vol) * 20f : -80f;
                _audioMixer.SetFloat(_bgmVolumeParam, dB);
            }
        }

        private IEnumerator CrossFade(AudioSource fromSource, AudioSource toSource, float duration)
        {
            float elapsed = 0f;
            float vol = _isMuted ? 0f : _currentVolume * _maxVolume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                fromSource.volume = Mathf.Lerp(vol, 0f, t);
                toSource.volume = Mathf.Lerp(0f, vol, t);

                yield return null;
            }

            fromSource.volume = 0f;
            fromSource.Stop();
            toSource.volume = vol;

            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutAll(float duration)
        {
            float elapsed = 0f;
            float startVolA = _sourceA.volume;
            float startVolB = _sourceB.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                _sourceA.volume = Mathf.Lerp(startVolA, 0f, t);
                _sourceB.volume = Mathf.Lerp(startVolB, 0f, t);

                yield return null;
            }

            _sourceA.volume = 0f;
            _sourceA.Stop();
            _sourceB.volume = 0f;
            _sourceB.Stop();

            _fadeCoroutine = null;
        }

        private AudioClip GetClipForTrack(BGMTrack track)
        {
            if (_trackClips == null) return null;
            foreach (var pair in _trackClips)
            {
                if (pair.Track == track) return pair.Clip;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
        }

        [Serializable]
        public struct BGMTrackClipPair
        {
            public BGMTrack Track;
            public AudioClip Clip;
        }
    }
}
