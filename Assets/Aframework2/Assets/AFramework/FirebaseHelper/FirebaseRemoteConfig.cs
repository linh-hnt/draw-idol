using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if USE_FIREBASE && USE_FIREBASE_REMOTECONFIG
using Firebase.Extensions;

namespace AFramework.FirebaseService
{
    public class RemoteConfigDataHelper : IRemoteConfigData
    {
        Firebase.RemoteConfig.ConfigValue mValue;
        public RemoteConfigDataHelper(Firebase.RemoteConfig.ConfigValue input)
        {
            mValue = input;
        }

        public bool GetBooleanValue()
        {
            return mValue.BooleanValue;
        }

        public IEnumerable<byte> GetByteArrayValue()
        {
            return mValue.ByteArrayValue;
        }

        public double GetDoubleValue()
        {
            return mValue.DoubleValue;
        }

        public long GetLongValue()
        {
            return mValue.LongValue;
        }

        public string GetStringValue()
        {
            return mValue.StringValue;
        }
    }

    public class FirebaseRemoteConfig : MonoBehaviour
    {
        public const string AF_CUSTOM_EVENT = "af_custom_event_v2";
        const string ADS_INTERSTITIAL_FOR_REWARD = "ads_interstitial_for_reward";
        const string FIREBASE_EXPERIMENT_ID = "firebase_experiment_id";
        const string ADS_DOWNLOAD_TIMEOUT = "ads_download_timeout";
        const string ACROSS_PROMO = "across_promo";
        
        const int LoadSuccessCooldownTime = 12 * 60 * 60;

        public const string LOCAL_STATUS = "FirebaseRemoteJson_localstatus";
        public const string LOCAL_JSON = "FirebaseRemoteJson_localjson";

        public static bool IsLocalConfig
        {
            get
            {
                return PlayerPrefs.GetInt(LOCAL_STATUS, 0) > 0 && PlayerPrefs.HasKey(LOCAL_JSON);
            }
        }

        public static System.Action EventFectchData;

        public AFramework.RemoteConfigGenData GenConfig;

        protected bool mInited = false;
        protected bool mIsLoading = false;
        protected bool mLoadSuccess = false;
        protected double mLastLoadTime = -999999999;

        void Start()
        {
            FirebaseInstance.ChecAndTryInit(Init);
        }

        void Init()
        {
            System.Collections.Generic.Dictionary<string, object> defaults =
              new System.Collections.Generic.Dictionary<string, object>();
            SetupDefaultConfig(defaults);
        }

        protected virtual void SetupDefaultConfig(System.Collections.Generic.Dictionary<string, object> defaults)
        {
            //need to override this function to init default value

            Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task => {
                mInited = true;
                FetchDataAsync();
            });
        }

        protected virtual void UpdateGameParams()
        {
            var interstitial_for_reward = GetValue(ADS_INTERSTITIAL_FOR_REWARD);
            if (interstitial_for_reward != null && AFramework.Ads.AdsManager.IsInstanceValid())
            {
                AFramework.Ads.AdsManager.I.SetUseInterstitialForReward(interstitial_for_reward.GetBooleanValue());
            }

            
            if (AFramework.Analytics.TrackingManager.IsInstanceValid())
            {
                var af_events = GetValue(AF_CUSTOM_EVENT);
                if (af_events != null) AFramework.Analytics.TrackingManager.I.UpdateCustomRuleData(true, af_events.GetStringValue());

#if USE_FIREBASE_ANALYTICS
                var firebaseFirstEventData = PlayerPrefs.GetInt(FirebaseService.FirebaseInstance.FirebaseFirstEvent_Name, 0);
                if ((firebaseFirstEventData & (int)FirebaseService.eFirebaseFirstEvent.FirstRemote) == 0)
                {
                    AFramework.Analytics.TrackingManager.I.TrackEvent("FIRST_LAUNCH_FIREBASE");
                    PlayerPrefs.SetInt(FirebaseService.FirebaseInstance.FirebaseFirstEvent_Name, firebaseFirstEventData | (int)FirebaseService.eFirebaseFirstEvent.FirstRemote);
                }
#endif
            }

            var ads_download_timeout = GetValue(ADS_DOWNLOAD_TIMEOUT);
            if (ads_download_timeout != null && AFramework.Ads.AdsManager.IsInstanceValid())
            {
                AFramework.Ads.AdsManager.I.SetAdsDownloadTimeout((int)ads_download_timeout.GetLongValue());
            }

            var a_cross_promo = GetValue(ACROSS_PROMO);
            if (a_cross_promo != null && AFramework.Promo.APromoManager.IsInstanceValid())
            {
                AFramework.Promo.APromoManager.I.OnReceivePromoData(a_cross_promo.GetStringValue());
            }

#if USE_FB_ANALYTICS
            var fb_experiment_id = GetValue(FIREBASE_EXPERIMENT_ID);
            if (fb_experiment_id != null)
            {
                var stringData = fb_experiment_id.GetStringValue();
                if (!string.IsNullOrEmpty(stringData))
                {
                    Analytics.FacebookAnalytics.SetExperimentId(stringData);
                }
            }
#endif
        }

        Task FetchDataAsync()
        {
            if (!mInited || mIsLoading || mLastLoadTime + LoadSuccessCooldownTime > AFramework.Utility.GetCurrentTimeSecond())
            {
                return null;
            }

            mIsLoading = true;
#if UNITY_EDITOR
            var cacheTime = new System.TimeSpan(12, 0, 0);
            var firebaseFirstEventData = PlayerPrefs.GetInt(FirebaseService.FirebaseInstance.FirebaseFirstEvent_Name, 0);
            if ((firebaseFirstEventData & (int)FirebaseService.eFirebaseFirstEvent.FirstRemote) == 0 || PlayerPrefs.GetInt("ForceUpdateFirebaseConfig", 0) == 1)
            {
                cacheTime = new System.TimeSpan(-30, 0, 0, 0);
                PlayerPrefs.SetInt("ForceUpdateFirebaseConfig", 0);
            }
            System.Threading.Tasks.Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
                cacheTime
            );
#else
            System.Threading.Tasks.Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync();
#endif
            return fetchTask.ContinueWithOnMainThread(FetchComplete);
        }

        void FetchComplete(Task fetchTask)
        {
#if UNITY_EDITOR || LOCAL_FIREBASEREMOTECONFIG
            if (IsLocalConfig)
            {
                localConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<FirebaseRemoteJson>(PlayerPrefs.GetString(LOCAL_JSON));
                localConfig.Init();
            }
#endif
            var info = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.Info;
            switch (info.LastFetchStatus)
            {
                case Firebase.RemoteConfig.LastFetchStatus.Success:
                    Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                        .ContinueWithOnMainThread(task => {
                            //DebugLog(String.Format("Remote data loaded and ready (last fetch time {0}).",
                            //                       info.FetchTime));
                            mIsLoading = false;
                            mLoadSuccess = true;
                            UnityMainThreadDispatcher.instance.Enqueue(UpdateGameParams);
                            mLastLoadTime = AFramework.Utility.GetCurrentTimeSecond();
                        });
                    break;
                case Firebase.RemoteConfig.LastFetchStatus.Failure:
                    {
                        var dic = new Dictionary<string, object>();
                        dic["reason"] = info.LastFetchFailureReason.ToString();
                        AFramework.Analytics.TrackingManager.instance.TrackEvent("REMOTE_CONFIG_FAIL", dic);
                        switch (info.LastFetchFailureReason)
                        {
                            case Firebase.RemoteConfig.FetchFailureReason.Error:
                                //DebugLog("Fetch failed for unknown reason");
                                mLastLoadTime = AFramework.Utility.GetCurrentTimeSecond() + 5 * 60 - LoadSuccessCooldownTime;
                                break;
                            case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                                //DebugLog("Fetch throttled until " + info.ThrottledEndTime);
                                mLastLoadTime = info.ThrottledEndTime.ToUniversalTime().Subtract(System.DateTime.MinValue).TotalSeconds - LoadSuccessCooldownTime;
                                break;
                        }
                        mIsLoading = false;
                    }
                    break;
                case Firebase.RemoteConfig.LastFetchStatus.Pending:
                    //DebugLog("Latest Fetch call still pending.");
                    mLastLoadTime = AFramework.Utility.GetCurrentTimeSecond() + 5 * 60;
                    mIsLoading = false;
                    break;
                default:
                    mIsLoading = false;
                    break;
            }
        }

        public IRemoteConfigData GetValue(string key)
        {
#if UNITY_EDITOR || LOCAL_FIREBASEREMOTECONFIG
            if (localConfig != null)
            {
                var val = localConfig.GetValue(key);
                if (val == null) return null;
                return new FirebaseLocalConfigDataHelper(val);
            }
#endif
            var keyData = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            if (keyData.Source == Firebase.RemoteConfig.ValueSource.RemoteValue)
            {
                return new RemoteConfigDataHelper(keyData);
            }
            return null;
        }

        protected virtual void OnApplicationPause(bool isPaused)
        {
            if (!isPaused)
            {
                FetchDataAsync();
            }
        }

#if UNITY_EDITOR || LOCAL_FIREBASEREMOTECONFIG
        FirebaseRemoteJson localConfig;
        public void UpdateLocalConfig(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                localConfig = null;
                PlayerPrefs.SetInt(LOCAL_STATUS, 0);
                PlayerPrefs.DeleteKey(LOCAL_JSON);
                UpdateGameParams();
                return;
            }

            localConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<FirebaseRemoteJson>(json);
            localConfig.Init();
            UpdateGameParams();
            PlayerPrefs.SetInt(LOCAL_STATUS, 1);
            PlayerPrefs.SetString(LOCAL_JSON, json);
        }

        public void RefreshLocalConfig()
        {
            UpdateGameParams();
        }
#endif
    }
}
#endif
