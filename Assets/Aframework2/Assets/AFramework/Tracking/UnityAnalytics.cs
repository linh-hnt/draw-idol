using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_UNITY_ANALYTICS
using UnityEngine.Analytics;

namespace AFramework
{
    namespace Analytics
    {
        public class UnityAnalytics : IAnalytic
        {
            public bool InitSuccess { get; set; }

            public void ApplicationOnPause(bool Paused)
            {
                
            }

            public void Init(params string[] args)
            {
                InitSuccess = true;
            }

            public void TrackEvent(string eventName)
            {
                AnalyticsEvent.Custom(eventName, new Dictionary<string, object>());
            }

            public void TrackEvent(string eventName, Dictionary<string, object> parameters)
            {
                AnalyticsEvent.Custom(eventName, parameters);
            }
        }
    }
}
#endif