using IngameDebugConsole;
using UnityEngine;

namespace AFramework
{
	public class CheatLogATime : MonoBehaviour
	{
        [ConsoleMethod("time.now", nameof(PrintTime))]
        public static void PrintTime()
        {
            ADebug.LogRelease($"Date Utc Now: {ATime.DateUtcNow}");
            ADebug.LogRelease($"Date Time Zone Now: {ATime.DateTimeZoneNow}");
            ADebug.LogRelease($"Date Local Now: {ATime.DateLocalNow}");
            ADebug.LogRelease($"Cheat Add Hour: {ATime.CheatAddSeconds / 3600}");
        }

        [ConsoleMethod("time.sethour", nameof(SetCheatHour))]
        public static void SetCheatHour(float addHour)
        {
            ATime.CheatAddSeconds = Mathf.RoundToInt(addHour) * 3600;
            PrintTime();
        }

        [ConsoleMethod("time.addhour", nameof(AddCheatHour))]
        public static void AddCheatHour(float addHour)
        {
            ATime.CheatAddSeconds += Mathf.RoundToInt(addHour) * 3600;
            PrintTime();
        }
    }
}
