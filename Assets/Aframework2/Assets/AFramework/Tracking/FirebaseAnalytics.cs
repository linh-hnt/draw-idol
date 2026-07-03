using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if USE_FIREBASE && USE_FIREBASE_ANALYTICS
namespace AFramework
{
    namespace Analytics
    {
        public class FirebaseAnalytics : IAnalytic
        {
            public bool InitSuccess { get; set; }

            string mUDID;
            public void ApplicationOnPause(bool Paused)
            {
                if (!AFramework.FirebaseService.FirebaseInstance.HasInstance) return;
                if (!Paused)
                {

                }
            }

            public void Init(params string[] args)
            {
                mUDID = AFramework.Utility.GetUDID();
                AFramework.FirebaseService.FirebaseInstance.ChecAndTryInit(() => {
                    Firebase.Analytics.FirebaseAnalytics.SetUserId(mUDID);
                    Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Firebase.Analytics.FirebaseAnalytics.LogEvent(Firebase.Analytics.FirebaseAnalytics.EventLogin);
                    InitSuccess = true;
                });
            }

            public void TrackEvent(string eventName, Dictionary<string, object> parameters)
            {
                if (!AFramework.FirebaseService.FirebaseInstance.HasInstance) return;
                Firebase.Analytics.Parameter[] fireBaseParameters = new Firebase.Analytics.Parameter[parameters.Count];

                int index = 0;
                foreach(KeyValuePair<string, object> kv in parameters)
                {
                    fireBaseParameters[index++] = ParseParameter(kv.Key, kv.Value);
                }

                Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, fireBaseParameters);
            }

            public void TrackEvent(string eventName)
            {
                if (!AFramework.FirebaseService.FirebaseInstance.HasInstance) return;
                Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
            }

            Firebase.Analytics.Parameter ParseParameter(string paramName, object paramValue)
            {
                if (paramValue is string)
                {
                    return new Firebase.Analytics.Parameter(paramName, paramValue as string);
                }
                else if (paramValue is float)
                {
                    return new Firebase.Analytics.Parameter(paramName, (float)paramValue);
                }
                else if (paramValue is double)
                {
                    return new Firebase.Analytics.Parameter(paramName, (double)paramValue);
                }
                else if (paramValue is decimal)
                {
                    return new Firebase.Analytics.Parameter(paramName, (double)((decimal)paramValue));
                }
                else if (paramValue is int)
                {
                    return new Firebase.Analytics.Parameter(paramName, (int)paramValue);
                }
                else if (paramValue is long)
                {
                    return new Firebase.Analytics.Parameter(paramName, (long)paramValue);
                }
                else
                {
                    return new Firebase.Analytics.Parameter(paramName, paramValue != null ? paramValue.ToString() : null);
                }
            }
        }
    }
}
#endif