using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class iOSATTConfig
{
    //public const string NSUserTrackingUsageDescription = "This identifier will be used to deliver personalized ads to you.";
    //public const string NSUserTrackingUsageDescription = "Pressing \"Allow\" uses device info for more relevant ad content";
    static Dictionary<string, string> _ATTUsageDescription;
    public static Dictionary<string, string> ATTUsageDescription {
        get { 
            if (_ATTUsageDescription == null)
            {
                _ATTUsageDescription = new Dictionary<string, string>();

                _ATTUsageDescription["de"] = "\\\"Erlauben\\\" drücken benutzt Gerätinformationen für relevantere Werbeinhalte";
                _ATTUsageDescription["en"] = "This allows us to optimise the game and provide better personalised experienced for you";
                _ATTUsageDescription["es"] = "Esto nos permite optimizar el juego y ofrecerte mejores experiencias personalizadas";
                _ATTUsageDescription["fr"] = "\\\"Autoriser\\\" permet d'utiliser les infos du téléphone pour afficher des contenus publicitaires plus pertinents";
                _ATTUsageDescription["ja"] = "これにより、ゲームを最適化し、あなたにより良いパーソナライズされた体験を提供することができます";
                _ATTUsageDescription["ko"] = "이것은 우리가 게임을 최적화하고 당신에게 더 나은 맞춤형 경험을 제공할 수 있게 합니다";
                _ATTUsageDescription["zh-Hans"] = "这使我们能够优化游戏并为您提供更好的个性化体验";
                _ATTUsageDescription["zh-Hant"] = "这使我们能够优化游戏并为您提供更好的个性化体验";
                _ATTUsageDescription["vi"] = "Điều này cho phép chúng tôi tối ưu hóa trò chơi và cung cấp trải nghiệm cá nhân hóa tốt hơn cho bạn";
                _ATTUsageDescription["ru"] = "Это позволяет нам оптимизировать игру и предоставить вам лучшие персонализированные впечатления";
                _ATTUsageDescription["tl"] = "Ito ay nagbibigay-daan sa amin na i-optimize ang laro at magbigay ng mas mahusay na mga naka-personalisadong karanasan para sa iyo";
                _ATTUsageDescription["el"] = "Αυτό μας επιτρέπει να βελτιστοποιήσουμε το παιχνίδι και να παρέχουμε καλύτερες εξατομικευμένες εμπειρίες για εσάς";
            }
            return _ATTUsageDescription;
        }
    }

    public short AppsflyerWaitTime = 3 * 60;

    static bool isChecked = false;
    static bool _isOSready = false;
    public static bool IsOSReady()
    {
        if (!isChecked)
        {
            isChecked = true;
#if UNITY_IOS && !UNITY_EDITOR
            _isOSready = new System.Version(UnityEngine.iOS.Device.systemVersion) >= new System.Version("14.5");
#endif
        }
        return _isOSready;
    }
}
