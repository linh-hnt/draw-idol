using UnityEngine;

namespace App.Services
{
    public class VibrationService : MonoBehaviour, IVibrationService
    {
        [Header("Settings")]
        [SerializeField] private bool _enabledByDefault = true;

        private bool _isInitialized;
        private bool _isEnabled;

        private const string VIBRATION_KEY = "vibration_enabled";

        public bool IsInitialized => _isInitialized;
        public bool IsEnabled => _isEnabled;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isEnabled = PlayerPrefs.GetInt(VIBRATION_KEY, _enabledByDefault ? 1 : 0) == 1;

            if (SaveManager.IsInstanceValid())
                _isEnabled = SaveManager.I.GetSettingValue(SettingType.Vibration);

            _isInitialized = true;
        }

        public void Vibrate(VibrationPattern pattern)
        {
            if (!_isInitialized || !_isEnabled) return;

            switch (pattern)
            {
                case VibrationPattern.Light:
                    VibratePlatform(15);
                    break;
                case VibrationPattern.Medium:
                    VibratePlatform(30);
                    break;
                case VibrationPattern.Heavy:
                    VibratePlatform(50);
                    break;
                case VibrationPattern.DoubleTap:
                    VibratePlatform(new long[] { 0, 15, 50, 15 });
                    break;
                case VibrationPattern.Success:
                    VibratePlatform(new long[] { 0, 30, 50, 30, 100, 50 });
                    break;
                case VibrationPattern.Failure:
                    VibratePlatform(new long[] { 0, 100, 80, 50 });
                    break;
            }
        }

        public void Vibrate(long milliseconds)
        {
            if (!_isInitialized || !_isEnabled) return;
            VibratePlatform(milliseconds);
        }

        public void VibrateLight() => Vibrate(VibrationPattern.Light);
        public void VibrateMedium() => Vibrate(VibrationPattern.Medium);
        public void VibrateHeavy() => Vibrate(VibrationPattern.Heavy);

        public void Cancel()
        {
            if (!_isInitialized) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            RDG.Vibration.Cancel();
#endif
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            PlayerPrefs.SetInt(VIBRATION_KEY, enabled ? 1 : 0);

            if (!enabled) Cancel();
        }

        private void VibratePlatform(long milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            RDG.Vibration.Vibrate(milliseconds);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void VibratePlatform(long[] pattern)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            RDG.Vibration.Vibrate(pattern, repeat: -1);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
