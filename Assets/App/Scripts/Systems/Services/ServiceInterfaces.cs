namespace App.Services
{
    public interface IService
    {
        bool IsInitialized { get; }
        void Initialize();
    }

    public interface IBGMService : IService
    {
        void Play(BGMTrack track, float fadeDuration = 0.5f);
        void Stop(float fadeDuration = 0.5f);
        void Pause();
        void Resume();
        void SetVolume(float volume);
        void SetMute(bool isMute);
        bool IsMuted { get; }
        BGMTrack CurrentTrack { get; }
    }

    public interface ISoundEffectService : IService
    {
        void Play(SoundFX fx);
        void Play(UnityEngine.AudioClip clip, float volume = 1f);
        void SetVolume(float volume);
        void SetMute(bool isMute);
        bool IsMuted { get; }
    }

    public interface IVibrationService : IService
    {
        void Vibrate(VibrationPattern pattern);
        void Vibrate(long milliseconds);
        void VibrateLight();
        void VibrateMedium();
        void VibrateHeavy();
        void Cancel();
        void SetEnabled(bool enabled);
        bool IsEnabled { get; }
    }

    public enum BGMTrack
    {
        None = -1,
        MainMenu = 0,
        Gameplay = 1,
        Result = 2,
    }

    public enum SoundFX
    {
        ButtonClick = 0,
        PaintStart = 1,
        PaintFinish = 2,
        LevelComplete = 3,
        PopupOpen = 4,
        PopupClose = 5,
    }

    public enum VibrationPattern
    {
        Light = 0,
        Medium = 1,
        Heavy = 2,
        DoubleTap = 3,
        Success = 4,
        Failure = 5,
    }
}
