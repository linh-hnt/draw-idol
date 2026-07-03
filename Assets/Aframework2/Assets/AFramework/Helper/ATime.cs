using System;
using UnityEngine;

namespace AFramework
{
    public static class ATime
    {
        const string CheatSecondKey = "cSeckey";
        const string TimeZoneSecondKey = "tzSeckey";
        public const int SecondsInAnHour = 60 * 60;
        public const int SecondsInADay = 24 * 60 * 60;

        public static System.Action EventOnUTCDayEnd;
        public static System.Action EventOnLocalDayEnd;
        public static System.Action EventOnTimeZoneDayEnd;

        static long _serverUtcTime = 0;
        static float _beginServerOffsetTime = 0;
        static int _timezoneSecondOffet = 0;
        static int _localSecondOffet = 0;
        static int _cheatAddSecond = 0;

        /// <summary>
        /// Init some basic info to calculate time as soon as possible
        /// </summary>
        public static void Init()
        {
            _cheatAddSecond = PlayerPrefs.GetInt(CheatSecondKey, 0);
            if (!PlayerPrefs.HasKey(TimeZoneSecondKey))
            {
                PlayerPrefs.SetInt(TimeZoneSecondKey, (int)Math.Round(TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds));
            }
            _timezoneSecondOffet = PlayerPrefs.GetInt(TimeZoneSecondKey);
            _localSecondOffet = (int)Math.Round(TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds);

            UpdateEndDate();
        }

        /// <summary>
        /// Set time from server, if not then local will be used
        /// </summary>
        /// <param name="time"></param>
        public static void UpdateServerTime(long time)
        {
            _serverUtcTime = time;
            _beginServerOffsetTime = Time.realtimeSinceStartup;
        }

        public static void UpdateTimeZone(int secondOffset)
        {
            _timezoneSecondOffet = secondOffset;
            PlayerPrefs.SetInt(TimeZoneSecondKey, _timezoneSecondOffet);
            currentTimeZoneDate = DateTimeZoneNow.DayOfYear;
        }

        public static long UtcNow => GetUTCLong(_cheatAddSecond);

        public static DateTimeOffset DateUtcNow => GetUTCDateTimeOffset(_cheatAddSecond);
        public static DateTimeOffset DateLocalNow => GetUTCDateTimeOffset(_cheatAddSecond).ToLocalTime();
        public static DateTimeOffset DateTimeZoneNow
        {
            get
            {
                if (_timezoneSecondOffet != 0)
                    return GetUTCDateTimeOffset(_cheatAddSecond).ToOffset(new TimeSpan(0, 0, _timezoneSecondOffet));
                return GetUTCDateTimeOffset(_cheatAddSecond);
            }
        }

        public static long LocalSecondOffset => _localSecondOffet;
        public static long TimeZoneSecondOffset => _timezoneSecondOffet;

        static long GetUTCLong(long offset)
        {
            if (_serverUtcTime > 0)
            {
                return _serverUtcTime + (long)(Time.realtimeSinceStartup - _beginServerOffsetTime) + offset;
            }
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + offset;
        }

        static DateTimeOffset GetUTCDateTimeOffset(double offset)
        {
            if (_serverUtcTime > 0)
            {
                return DateTimeOffset.UnixEpoch.AddSeconds((double)_serverUtcTime + (double)(Time.realtimeSinceStartup - _beginServerOffsetTime) + offset);
            }
            return DateTimeOffset.UtcNow.AddSeconds(offset);
        }

        public static DateTime NextDateStartTime(DateTime time) => time.Date.AddDays(1);
        public static DateTimeOffset NextDateStartTime(DateTimeOffset time) => new DateTimeOffset(time.Year, time.Month, time.Day, 0, 0, 0, time.Offset).AddDays(1);
        /// <summary>
        /// Calculates seconds remaining until the end of the current day
        /// </summary>
        /// <returns>Seconds until midnight in the current timezone</returns>
        public static long GetSecondsUntilEndOfDay()
        {
            // Get current time as DateTimeOffset
            DateTimeOffset currentTime = Application.isPlaying
                ? DateLocalNow
                : DateTimeOffset.UtcNow.ToLocalTime();

            // Get next day start time (midnight) using ATime utility
            DateTimeOffset nextDayStartTime = NextDateStartTime(currentTime);

            // Calculate seconds until midnight
            long secondsUntilEndOfDay = (long)(nextDayStartTime - currentTime).TotalSeconds;

            // Ensure non-negative value
            return Math.Max(0, secondsUntilEndOfDay);
        }
        public static long ToEpochTime(DateTime time) => (long)time.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;
        public static long ToEpochTime(DateTimeOffset time) => time.ToUnixTimeSeconds();
        public static DateTimeOffset ToEpochTime(long seconds) => DateTimeOffset.UnixEpoch.AddSeconds(seconds);
        public static DateTimeOffset ToLocalTime(long seconds) => DateTimeOffset.UnixEpoch.AddSeconds(seconds).ToOffset(new TimeSpan(0, 0, _localSecondOffet));
        public static DateTimeOffset ToTimeZoneTime(long seconds) => DateTimeOffset.UnixEpoch.AddSeconds(seconds).ToOffset(new TimeSpan(0, 0, _timezoneSecondOffet));
        public static DateTime ToUTCBeginDate(this DateTimeOffset time) => time.UtcDateTime.Date;

        public static int CheatAddSeconds
        {
            get { return _cheatAddSecond; }
            set
            {
                _cheatAddSecond = value;
                PlayerPrefs.SetInt(CheatSecondKey, _cheatAddSecond);
            }
        }

        static int currentUTCDate = -1;
        static int currentLocalDate = -1;
        static int currentTimeZoneDate = -1;
        static async void UpdateEndDate()
        {
            currentUTCDate = DateUtcNow.DayOfYear;
            currentLocalDate = DateLocalNow.DayOfYear;
            currentTimeZoneDate = DateTimeZoneNow.DayOfYear;

#if UNITY_EDITOR
            while (UnityEditor.EditorApplication.isPlaying)
#else
            while (true)
#endif
            {
                await System.Threading.Tasks.Task.Delay(100);
                if (currentUTCDate != DateUtcNow.DayOfYear)
                {
                    EventOnUTCDayEnd?.Invoke();
                    currentUTCDate = DateUtcNow.DayOfYear;
                }
                if (currentLocalDate != DateLocalNow.DayOfYear)
                {
                    EventOnLocalDayEnd?.Invoke();
                    currentLocalDate = DateLocalNow.DayOfYear;
                }
                if (currentTimeZoneDate != DateTimeZoneNow.DayOfYear)
                {
                    EventOnTimeZoneDayEnd?.Invoke();
                    currentTimeZoneDate = DateTimeZoneNow.DayOfYear;
                }
            }
        }

#if UNITY_EDITOR
        public static void Reset()
        {
            EventOnUTCDayEnd = null;
            EventOnLocalDayEnd = null;
            EventOnTimeZoneDayEnd = null;

            _serverUtcTime = 0;
            _beginServerOffsetTime = 0;
            _timezoneSecondOffet = 0;
            _localSecondOffet = 0;
            _cheatAddSecond = 0;

            currentUTCDate = -1;
            currentLocalDate = -1;
            currentTimeZoneDate = -1;
        }
#endif
    }
}