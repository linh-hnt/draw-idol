using AFramework.MiniJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework
{
    public class TimeServer : ManualSingletonMono<TimeServer>
    {
        class TimerServerFormat
        {
            public enum TimeTypeFormat
            {
                Second,
                Tick,
                FileTime,
            }

            public string ServerURL;
            public string TimeField;
            public TimeTypeFormat SecondTypeFormat = TimeTypeFormat.Second;

            public TimerServerFormat(string url, string field, TimeTypeFormat timeType)
            {
                ServerURL = url;
                TimeField = field;
                SecondTypeFormat = timeType;
            }
        }

        TimerServerFormat[] mServerInfo;
        int mCurrentServerIndex = 0;
        bool mIsTimeUpdated = false;
        double mOffsetTime = 0;
        bool mIsServerTime = false;
        public bool IsServerTime
        {
            //#if UNITY_IOS
            get { return true; }
            //#else
            //            get { return mIsServerTime; }
            //#endif
        }

        System.DateTime mBeginDefaultTime;
        System.DateTime _CurrentDateTime;
        public System.DateTime CurrentDateTime
        {
            get
            {
                //return _CurrentDateTime;
//#if UNITY_IOS
                return AFramework.Utility.GetCurrentTime();
//#else
//                return mBeginDefaultTime.AddSeconds(Time.realtimeSinceStartup + mOffsetTime);
//#endif
            }
            private set
            {
                _CurrentDateTime = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();

//#if UNITY_IOS
            return;
//#endif
            mBeginDefaultTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            mServerInfo = new TimerServerFormat[3];
            mServerInfo[0] = new TimerServerFormat("http://worldtimeapi.org/api/ip", "unixtime", TimerServerFormat.TimeTypeFormat.Second);
            mServerInfo[1] = new TimerServerFormat("http://worldclockapi.com/api/json/utc/now", "currentFileTime", TimerServerFormat.TimeTypeFormat.FileTime);
            mServerInfo[2] = new TimerServerFormat("http://api.timezonedb.com/v2.1/get-time-zone?key=GDFH3TZ0WY38&format=json&by=zone&zone=Atlantic%2FAzores", "timestamp", TimerServerFormat.TimeTypeFormat.Second);//personal server
        }

//        public void Start()
//        {
//#if !UNITY_IOS
//            StartCoroutine(CRRequestServerTime());
//#endif
//        }

        IEnumerator CRRequestServerTime()
        {
            float waitTime = 60.0f;
            var classPointerReference = this;
            while (true)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    yield return new WaitForSeconds(30.0f);
                }
                else
                {
                    //check destroy and stop
                    if (classPointerReference == null || mServerInfo == null || mCurrentServerIndex < 0 || mCurrentServerIndex >= mServerInfo.Length || mServerInfo[mCurrentServerIndex] == null)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || USE_CHEAT
                        Debug.LogError("mServerInfo is not valid");
#endif
                        yield break;
                    }

                    TimerServerFormat cacheCurrentServerInfo = mServerInfo[mCurrentServerIndex];
                    int serverInfoLength = mServerInfo.Length;
                    var www = UnityEngine.Networking.UnityWebRequest.Get(cacheCurrentServerInfo.ServerURL);
                    yield return www.SendWebRequest();
                    yield return new WaitUntil(() => www.isDone);

                    //check destroy and stop
                    if (classPointerReference == null || mServerInfo == null || mCurrentServerIndex < 0 || mCurrentServerIndex >= mServerInfo.Length || mServerInfo[mCurrentServerIndex] == null)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || USE_CHEAT
                        Debug.LogError("mServerInfo is not valid");
#endif
                        yield break;
                    }

                    if (www.isNetworkError)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || USE_CHEAT
                        Debug.LogWarning(string.Format("TimeServer {0}: request error: {1}", mCurrentServerIndex, www.error));
#endif
                        mIsTimeUpdated = false;
                        ++mCurrentServerIndex;
                        mCurrentServerIndex %= serverInfoLength;
                    }
                    else
                    {
                        object deserializeResult = null;
                        try
                        {
                            var downloadText = www.downloadHandler.text;
                            if (!string.IsNullOrEmpty(downloadText))
                            {
#if UNITY_PURCHASING || SIS_IAP
                                deserializeResult = UnityEngine.Purchasing.MiniJSON.Json.Deserialize(downloadText);
#else
                                deserializeResult = Json.Deserialize(downloadText);
#endif
                            }
                        }
                        catch (System.Exception e)
                        {
                            AFramework.Analytics.TrackingManager.I.TrackEvent("TIMESERVER_FAIL", "server", mCurrentServerIndex.ToString());
#if DEVELOPMENT_BUILD
                            Debug.LogWarning("Timeserver fail: " + e.Message);
#endif
                        }
                        Dictionary<string, object> result = null;
                        if (deserializeResult != null && deserializeResult is Dictionary<string, object>)
                        {
                            result = deserializeResult as Dictionary<string, object>;
                        }
                        bool success = false;
                        if (result != null && result.ContainsKey(cacheCurrentServerInfo.TimeField))
                        {
                            if (cacheCurrentServerInfo.SecondTypeFormat == TimerServerFormat.TimeTypeFormat.Second)
                            {
                                _CurrentDateTime = mBeginDefaultTime.AddSeconds((long)result[cacheCurrentServerInfo.TimeField]);
                            }
                            else if (cacheCurrentServerInfo.SecondTypeFormat == TimerServerFormat.TimeTypeFormat.Tick)
                            {
                                _CurrentDateTime = mBeginDefaultTime.AddTicks((long)result[cacheCurrentServerInfo.TimeField]);
                            }
                            else if (cacheCurrentServerInfo.SecondTypeFormat == TimerServerFormat.TimeTypeFormat.FileTime)
                            {
                                _CurrentDateTime = System.DateTime.FromFileTimeUtc((long)result[cacheCurrentServerInfo.TimeField]);
                            }

                            success = true;
                        }

                        if (success)
                        {
                            //var test_utc = System.DateTime.UtcNow;
                            mIsServerTime = true;
                            mIsTimeUpdated = true;
                            mOffsetTime = (_CurrentDateTime - mBeginDefaultTime).TotalSeconds - Time.realtimeSinceStartup;
                        }
                        else
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.LogWarning(string.Format("TimeServer {0}: parse error: {1}", mCurrentServerIndex, www.downloadHandler.text));
#endif
                            mIsTimeUpdated = false;
                            ++mCurrentServerIndex;
                            mCurrentServerIndex %= serverInfoLength;
                        }
                    }

                    waitTime = mIsTimeUpdated ? 300.0f : (mCurrentServerIndex == 0 ? 30f : 5.0f);

                    yield return new WaitForSeconds(waitTime);
                }
            }
        }

        void Update()
        {
            if (!mIsServerTime) return;
            CurrentDateTime = CurrentDateTime.AddSeconds(Time.unscaledDeltaTime);
        }
    }
}
