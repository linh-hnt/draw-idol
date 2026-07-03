using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
#if USE_FB_ANALYTICS
using Facebook.Unity;

namespace AFramework
{
    namespace Analytics
    {
        public class FacebookAnalytics : IAnalytic
        {
            public bool InitSuccess { get; set; }

            static string fbExperiementId = null;
            public static void SetExperimentId(string id) { fbExperiementId = id; }

            public void ApplicationOnPause(bool Paused)
            {
                if (!Paused)
                {
                    //resume
                    if (FB.IsInitialized)
                    {
                        FB.ActivateApp();
                    }
                }
            }

            public void Init(params string[] args)
            {
                if (FB.IsInitialized)
                {
                    FB.ActivateApp();
                    InitSuccess = true;
                }
                else
                {
                    //Handle FB.Init
                    FB.Init(() => {
                        FB.ActivateApp();
                        InitSuccess = true;
                    });
                }
            }

            public void TrackEvent(string eventName)
            {
                if (fbExperiementId != null)
                {
                    var dic = new Dictionary<string, object>();
                    dic["experiment_id"] = fbExperiementId;
                    FB.LogAppEvent(eventName, null, dic);
                }
                else
                {
                    FB.LogAppEvent(eventName);
                }
            }

            public void TrackEvent(string eventName, Dictionary<string, object> parameters)
            {
                if (fbExperiementId != null)
                {
                    parameters["experiment_id"] = fbExperiementId;
                }
                FB.LogAppEvent(eventName, null, parameters);
            }
        }
    }
}
#endif