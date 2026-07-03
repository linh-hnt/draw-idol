using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AFramework;
using AFramework.Analytics;
using System.Linq;
using System;

namespace AFramework.Analytics
{
    public class TrackingManager : AFramework.SingletonMono<TrackingManager>
    {
        [SerializeField] protected FrameworkGlobalConfig _Config;
        [SerializeField] protected bool TrackCostCenter = false;
        public FrameworkGlobalConfig Config { get { return _Config; } }

        public const string FIRST_LAUNCH = "FIRST_LAUNCH";
        public const string FIRST_VERSION = "FIRST_VERSION";

        public const string EVENT_IAP_CLICK = "IAP_CLICKED";
        public const string EVENT_IAP_BOUGHT = "IAP_BOUGHT";
        public const string EVENT_ADS_UNKNOWN = "ADS_UNKNOWN";
        public const string EVENT_ADS_BANNER = "ADS_BANNER";
        public const string EVENT_ADS_INTERSTITIAL = "ADS_INTERSTITIAL";
        public const string EVENT_ADS_REWARD = "ADS_REWARD";

        public const string OFFLINE_EVENT_SAVE = "afofflineevent";
        public const string IAP_TOTAL_VALUE = "total_iap";
        public const string IAP_PURCHASE_TIME = "purchase_time";
        public const string FIREBASE_LEVEL_PLAYERPREFS = "firebase_currentlevel";
        public const string FIREBASE_MAXLEVEL_PLAYERPREFS = "firebase_maxlevel";
        public const string FIREBASE_MODE_PLAYERPREFS = "firebase_currentmode";

#if USE_APPSFLYER_ANALYTICS
        public static AppsFlyerAnalytics appsflyerObject { get; protected set; }
#endif
        public virtual string AppflyerKey { get { return "J3etMkpLvkqZMcCKAMD3V"; } }
        public virtual string AppflyerAppId
        {
            get
            {
#if UNITY_ANDROID
                return Application.identifier;
#endif
                if (_Config == null) return "";
                return _Config.GetiOSAppId(Application.identifier);
            }
        }

        protected List<IAnalytic> Analytics = new List<IAnalytic>();
        protected List<IAnalytic> LimitedAnalytics = new List<IAnalytic>();
        protected Dictionary<string, List<CustomRuleEvent>> appsflyerCustomEventList = new Dictionary<string, List<CustomRuleEvent>>();

        public string AppsflyerID { get; protected set; }
        public string FirebaseID { get; protected set; }
        public bool IsInited { get; protected set; }
        public bool IsTrackingReady { get; protected set; }

        bool hasNewOfflineEvent = false;
        OfflineEventHandler offlineEventData = new OfflineEventHandler();
        void SaveOfflineEvent()
        {
            if (!hasNewOfflineEvent) return;
            hasNewOfflineEvent = false;
            PlayerPrefs.SetString(OFFLINE_EVENT_SAVE, offlineEventData.events.Count == 0 ? string.Empty : JsonUtility.ToJson(offlineEventData));
            PlayerPrefs.Save();
        }
        void LoadOfflineEvent()
        {
            var offlineStr = PlayerPrefs.GetString(OFFLINE_EVENT_SAVE, string.Empty);
            if (string.IsNullOrEmpty(offlineStr)) return;
            offlineEventData = JsonUtility.FromJson<OfflineEventHandler>(offlineStr);
        }

        protected void Start()
        {
#if UNITY_ANDROID
            _Config?.CheckTester();
#endif
            AppsflyerID = null;
            FirebaseID = null;
            InitTracking();
        }

        protected virtual void InitTracking()
        {
            IAnalytic analytic;

#if USE_APPSFLYER_ANALYTICS && !DISABLE_ONLINE_TRACKING
            {
                appsflyerObject = new GameObject("AppsFlyerObject").AddComponent<AppsFlyerAnalytics>();
                appsflyerObject.transform.SetParent(this.transform);
                analytic = appsflyerObject;
            }
            LimitedAnalytics.Add(analytic);
            analytic.Init(AppflyerKey, AppflyerAppId);
            AppsflyerID = analytic.InitSuccess ? (analytic as AppsFlyerAnalytics).UDID : null;
#endif

            bool enableOnlineAnalytics = true;
#if true//USE_LOCAL_ANALYTICS
            analytic = new AFramework.Analytics.LocalAnalytics();
            analytic.Init();
            if (analytic.InitSuccess)
            {
                Analytics.Add(analytic);
                enableOnlineAnalytics = false;//Tin: if is local then won't init online analytic
            }
#endif
#if DISABLE_ONLINE_TRACKING
            enableOnlineAnalytics = false;
#elif ENABLE_ONLINE_TRACKING
            enableOnlineAnalytics = true;
#endif

            if (enableOnlineAnalytics)
            {
#if USE_BYTE_BREW && !DISABLE_ONLINE_TRACKING
                analytic = new ByteBrewAnalytics();
                Analytics.Add(analytic);
                analytic.Init();
#endif

#if USE_FB_ANALYTICS && !DISABLE_ONLINE_TRACKING
                analytic = new FacebookAnalytics();
                Analytics.Add(analytic);
                analytic.Init();
#endif
#if USE_UNITY_ANALYTICS && !DISABLE_ONLINE_TRACKING
                analytic = new AFramework.Analytics.UnityAnalytics();
                Analytics.Add(analytic);
                analytic.Init();
#endif

#if USE_FIREBASE && USE_FIREBASE_ANALYTICS && !DISABLE_ONLINE_TRACKING
                analytic = new FirebaseAnalytics();
                Analytics.Add(analytic);
                analytic.Init();
                IsTrackingReady = false;
                LoadOfflineEvent();
#else
                IsTrackingReady = true;
#endif
            }
            else
            {
                IsTrackingReady = true;
            }
            IsInited = true;

#if USE_FIREBASE && !DISABLE_ONLINE_TRACKING
            StartCoroutine(CRWaitForFirebaseId());
#endif

            TrackFirstLaunch();
        }

#if USE_FIREBASE
        IEnumerator CRWaitForFirebaseId()
        {
            FirebaseID = PlayerPrefs.GetString("FirebaseID", null);
            while (!AFramework.FirebaseService.FirebaseInstance.HasInstance)
            {
                yield return null;
            }
            System.Threading.Tasks.Task<string> t = Firebase.Installations.FirebaseInstallations.DefaultInstance.GetIdAsync();
            while (!t.IsCompleted)
            {
                yield return null;
            }
            FirebaseID = t.Result;
            PlayerPrefs.SetString("FirebaseID", FirebaseID);
            if (!IsTrackingReady)
            {
                IsTrackingReady = true;

                int eventCount = offlineEventData.events.Count;
                for (int i = 0; i < eventCount; ++i)
                {
                    var eventData = offlineEventData.events[i];
                    if (eventData.normal)
                    {
                        TrackEvent(eventData.eName, eventData.GetParams());
                    }
                    else
                    {
                        TrackLimitedEvent(eventData.eName, eventData.GetParams());
                    }
                }
                offlineEventData.events.Clear();
                hasNewOfflineEvent = true;
            }

            var firebaseFirstEventData = PlayerPrefs.GetInt(FirebaseService.FirebaseInstance.FirebaseFirstEvent_Name, 0);
            if ((firebaseFirstEventData & (int)FirebaseService.eFirebaseFirstEvent.FirstLaunch) == 0)
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("userType", "Free");
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("newUserVersion", Application.version);
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("totalPaid", Mathf.RoundToInt(PlayerPrefs.GetFloat(IAP_TOTAL_VALUE, 0)).ToString());
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty("totalPurchase", Mathf.RoundToInt(PlayerPrefs.GetInt(IAP_PURCHASE_TIME, 0)).ToString());
                PlayerPrefs.SetInt(FirebaseService.FirebaseInstance.FirebaseFirstEvent_Name, firebaseFirstEventData | (int)FirebaseService.eFirebaseFirstEvent.FirstLaunch);
            }
        }
#endif

        public void TrackEvent(string eventName)
        {
            TrackEvent(eventName, new Dictionary<string, object>());
        }

        public void TrackEvent(string eventName, params string[] list)
        {
            Dictionary<string, object> Dic = new Dictionary<string, object>();
            for (int i = 0; i < list.Length; i += 2)
                Dic[list[i]] = list[i + 1];
            TrackEvent(eventName, Dic);
        }

        public void TrackEventLocal(string eventName, Dictionary<string, object> parameters)
        {
            foreach (IAnalytic a in Analytics)
            {
                if (a is LocalAnalytics)
                {
                    a.TrackEvent(eventName, parameters);
                }
            }
        }
        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!IsTrackingReady)
            {
                offlineEventData.AddEvent(true, eventName, parameters);
                hasNewOfflineEvent = true;
                return;
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (parameters == null) Debug.LogError("TrackEvent parameters is NULL");
#endif
            TrackCustomRuleLimitedEvent(eventName, parameters);

            if (!string.IsNullOrEmpty(AppsflyerID))
            {
                parameters["appsflyer_id"] = AppsflyerID;
            }

            if (!string.IsNullOrEmpty(FirebaseID))
            {
                parameters["af_firebase_id"] = FirebaseID;
            }

#if FAKE_TRACKING
            string paramLog = "";
            foreach(var pair in parameters)
            {
                paramLog += string.Format(" {0} {1}", pair.Key, pair.Value.ToString());
            }
            Debug.Log("Normal event: " + eventName + paramLog);
#else
            foreach (IAnalytic a in Analytics)
            {
                a.TrackEvent(eventName, parameters);
            }
#endif
        }

        public void TrackEvent(string eventName, string Key, string value)
        {
            Dictionary<string, object> Dic = new Dictionary<string, object>();
            Dic[Key] = value;
            TrackEvent(eventName, Dic);
        }

        public void TrackLimitedEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!IsTrackingReady)
            {
                offlineEventData.AddEvent(false, eventName, parameters);
                hasNewOfflineEvent = true;
                return;
            }

#if FAKE_TRACKING
            string paramLog = "";
            foreach (var pair in parameters)
            {
                paramLog += string.Format(" {0} {1}", pair.Key, pair.Value.ToString());
            }
            Debug.Log("Limit event: " + eventName + paramLog);
#else
            foreach (IAnalytic a in LimitedAnalytics)
            {
                a.TrackEvent(eventName, parameters);
            }
#endif
        }

        public void TrackLimitedEvent(string eventName)
        {
            if (!IsTrackingReady)
            {
                offlineEventData.AddEvent(false, eventName, null);
                hasNewOfflineEvent = true;
                return;
            }

#if FAKE_TRACKING
            Debug.Log("Limit event: " + eventName);
#else
            foreach (IAnalytic a in LimitedAnalytics)
            {
                a.TrackEvent(eventName);
            }
#endif
        }

        #region ATTracking
        const string ATTRACKING_SAVE = "attracking";
        public bool CanRequestATTracking()
        {
#if UNITY_EDITOR || TEST_ATTRACKING
            return PlayerPrefs.GetInt(ATTRACKING_SAVE, 0) == 0;
#elif UNITY_IOS
            if (!iOSATTConfig.IsOSReady()) return false;
            if (Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                return true;
#endif
            return false;
        }

        public int GetATState()
        {
#if UNITY_EDITOR || TEST_ATTRACKING
            return PlayerPrefs.GetInt(ATTRACKING_SAVE, 0);
#elif UNITY_IOS
            var state = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            if (state == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                return 0;
            }
            else if (state == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
            {
                return 1;
            }
            else
            {
                return -1;
            }
#endif
            return 0;
        }

        bool hasRequestATT = false;
        public bool IsRequestingATT { get; protected set; }
        public float RequestATTTimeFromStart { get; protected set; }
        public void RequestATTracking(float delay, Action finishAction = null)
        {
            if (hasRequestATT) 
            {
                finishAction?.Invoke();
                return;
            }

            hasRequestATT = true;
            if (CanRequestATTracking())
            {
                StartCoroutine(CRRequestATTracking(delay, finishAction));
            }
            else
            {
                finishAction?.Invoke();
            }
        }

        IEnumerator CRRequestATTracking(float delay, Action finishAction)
        {
            IsRequestingATT = true;
            if (delay > 0) yield return new WaitForSeconds(delay);
#if UNITY_EDITOR || TEST_ATTRACKING
            AFramework.UI.CanvasManager.OpenSystemPopupConfirm("App Tracking Transparency", iOSATTConfig.ATTUsageDescription["en"], "Allow", "Deny", (result) =>
            {
                PlayerPrefs.SetInt(ATTRACKING_SAVE, result ? 1 : -1);
                Debug.LogWarning("App Tracking Transparency " + (result ? "ALLOW" : "DENY"));
            });
            yield return null;
#elif UNITY_IOS
            Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();
            while (Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                yield return null;
            }
#else
            yield return null;
#endif
            RequestATTTimeFromStart = Time.realtimeSinceStartup;
#if UNITY_IOS && !TEST_ATTRACKING
            if (AFramework.Ads.AdsManager.IsInstanceValid() && AFramework.Ads.AdsManager.I.IsAdapterInited())
            {
                var hasConsent = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
//#if USE_IRONSOURCE_ADS
//                IronSource.Agent.setConsent(hasConsent);
//#elif USE_APPLOVIN_ADS
//                MaxSdk.SetHasUserConsent(hasConsent);
//#endif

#if FACEBOOK_AUDIENCENETWORK
                AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(hasConsent);
#endif
            }
            TrackEvent("ATRESULT_" + Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus().ToString());
#endif
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            IsRequestingATT = false;
            finishAction?.Invoke();
        }
        #endregion

        //private void OnApplicationQuit()
        //{
        //    Debug.LogError("TrackingManager: Add tracking OnApplicationQuit");
        //}

        void OnApplicationPause(bool pauseStatus)
        {
            foreach (IAnalytic a in Analytics)
            {
                a.ApplicationOnPause(pauseStatus);
            }

            SaveOfflineEvent();
        }

        public virtual void AddIAPClickAdditionalParam(ref Dictionary<string, object> currentParams) { }

        public void TrackIAPClick(string packageId, string location)
        {
            var parameters = new Dictionary<string, object>();
            parameters["pack"] = packageId;
            parameters["screen"] = location;
            AddIAPClickAdditionalParam(ref parameters);
            TrackEvent(EVENT_IAP_CLICK, parameters);
        }

        public virtual void AddIAPPurchaseAdditionalParam(IAP.PackageInfo iapInfo, ref Dictionary<string, object> currentParams) { }

        public void TrackIAPPurchase(string packageId, string transactionId, string location)
        {
#if UNITY_ANDROID
            if (AFramework.IAP.IAPManager.TamperedStore)
            {
                return;
            }
#endif

            IAP.PackageInfo iapInfo = AFramework.IAP.IAPManager.instance.PackageIdentifierToPackageInfo(packageId, false);
            if (iapInfo == null)
            {
                iapInfo = AFramework.IAP.IAPManager.instance.PackageIdentifierToPackageInfo(packageId, true);
            }
            if (iapInfo == null) return;

            {
                var parameters = new Dictionary<string, object>();
                parameters["value"] = iapInfo.Price;
                parameters["currency"] = iapInfo.Currency;
                //parameters["type"] = iapInfo.Type.ToString();
                parameters["pack"] = iapInfo.PackageIdentifier.getString();
                parameters["product_id"] = iapInfo.PackageIdentifier.getString();
                parameters["screen"] = location;

                var default_iap_pack = AFramework.IAP.IAPManager.instance.PackageIdentifierToPackageInfo(packageId, true);
                var total_iap = PlayerPrefs.GetFloat(IAP_TOTAL_VALUE, 0);
                if (default_iap_pack != null)
                {
                    total_iap += (float)default_iap_pack.Price;
                    PlayerPrefs.SetFloat(IAP_TOTAL_VALUE, total_iap);
                }
                var purchase_time = PlayerPrefs.GetInt(IAP_PURCHASE_TIME, 0) + 1;
                PlayerPrefs.SetInt(IAP_PURCHASE_TIME, purchase_time);
                parameters["total_iap"] = total_iap;

                parameters["level"] = PlayerPrefs.GetInt(FIREBASE_LEVEL_PLAYERPREFS, 0);
                parameters["level_mode"] = PlayerPrefs.GetString(FIREBASE_MODE_PLAYERPREFS, "unknown");
                parameters["transaction_order"] = purchase_time;

                AddIAPPurchaseAdditionalParam(iapInfo, ref parameters);

                //TrackEvent(EVENT_IAP_BOUGHT, parameters);
                TrackEvent("iap_sdk", parameters);

                if (TrackCostCenter)
                {
                    var cc_parameters = new Dictionary<string, object>();
                    CCAddIAPPurchaseAdditionalParam(iapInfo, ref cc_parameters);
                    cc_parameters["price"] = iapInfo.Price;
                    cc_parameters["currency"] = iapInfo.Currency;
                    cc_parameters["transaction_id"] = transactionId ?? string.Empty;
                    TrackEvent("cc_iap", cc_parameters);
                }

#if USE_FIREBASE_ANALYTICS
                if (FirebaseService.FirebaseInstance.HasInstance)
                {
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty("totalPaid", Mathf.RoundToInt(total_iap).ToString());
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty("totalPurchase", purchase_time.ToString());
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty("userType", "Paid");
                }
#endif
            }

#if false//USE_APPSFLYER_ANALYTICS //Tin: should be track in AppsFlyerPurchaseConnector
            {
                var parameters = new Dictionary<string, object>();
                parameters[AFInAppEvents.REVENUE] = iapInfo.Price * 0.69;
                parameters[AFInAppEvents.CURRENCY] = iapInfo.Currency;
                parameters[AFInAppEvents.CONTENT_TYPE] = iapInfo.Type.ToString();
                parameters[AFInAppEvents.CONTENT_ID] = iapInfo.PackageIdentifier.getString();
                parameters[AFInAppEvents.PRICE] = iapInfo.Price;
                parameters[AFInAppEvents.QUANTITY] = 1;

                TrackLimitedEvent(AFInAppEvents.PURCHASE, parameters);
            }
#endif
        }

        #region Ads
        protected Dictionary<string, object> mRewardAdsTrackingCacheData;
        public virtual void TrackAdsView(AFramework.Ads.AdsType adsType, Dictionary<string, object> args)
        {
            mRewardAdsTrackingCacheData = null;
            switch (adsType)
            {
                case Ads.AdsType.Banner:
                    TrackEvent("ADS_BANNER_IMPRESSION", args);
                    break;
                case Ads.AdsType.Interstitial:
                    TrackEvent("ADS_INTERSTITIAL_IMPRESSION", args);
                    TrackLimitedEvent("af_ad_view_interstitial");
                    break;
                case Ads.AdsType.RewardedVideo:
                    mRewardAdsTrackingCacheData = args;
                    TrackEvent("ADS_REWARD_IMPRESSION", args);
                    TrackLimitedEvent("af_ad_view_rewarded");
                    break;
                case Ads.AdsType.InterstitialRewardedVideo:
                    mRewardAdsTrackingCacheData = args;
                    TrackEvent("ADS_REWARD_INTERSTITIAL_IMPRESSION", args);
                    TrackLimitedEvent("af_ad_view_rewarded");
                    break;
                case Ads.AdsType.OfferWall:
                    TrackEvent("ADS_OFFERWALL_IMPRESSION", args);
                    break;
                case Ads.AdsType.AppOpen:
                    TrackEvent("ADS_APPOPEN_IMPRESSION", args);
                    break;
                case Ads.AdsType.OverlayNative:
                    TrackEvent("ADS_OVERLAYNATIVE_IMPRESSION", args);
                    break;
                case Ads.AdsType.NativeInterstitial:
                    TrackEvent("ADS_NATIVEINTERSTITIAL_IMPRESSION", args);
                    break;
                default:
                    TrackEvent(string.Format("ADS_{0}_IMPRESSION", adsType.ToString().ToUpperInvariant()), args);
                    break;
            }
        }

        public virtual void TrackAdsClick(AFramework.Ads.AdsType adsType)
        {
            switch (adsType)
            {
                case Ads.AdsType.Banner:
                    TrackEvent("ADS_BANNER_CLICK");
                    break;
                case Ads.AdsType.Interstitial:
                    if (mRewardAdsTrackingCacheData != null)//should be Ads.AdsType.InterstitialRewardedVideo
                    {
                        TrackEvent("ADS_REWARD_INTERSTITIAL_CLICK", mRewardAdsTrackingCacheData);
                    }
                    else
                    {
                        TrackEvent("ADS_INTERSTITIAL_CLICK");
                    }
                    break;
                case Ads.AdsType.RewardedVideo:
                    TrackEvent("ADS_REWARD_CLICK", mRewardAdsTrackingCacheData);
                    break;
                case Ads.AdsType.InterstitialRewardedVideo:
                    TrackEvent("ADS_REWARD_INTERSTITIAL_CLICK", mRewardAdsTrackingCacheData);
                    break;
                case Ads.AdsType.AppOpen:
                    {
                        TrackEvent("ADS_APPOPEN_CLICK");
                    }
                    break;
                case Ads.AdsType.OverlayNative:
                    TrackEvent("ADS_OVERLAYNATIVE_CLICK");
                    break;
                case Ads.AdsType.NativeInterstitial:
                    TrackEvent("ADS_NATIVEINTERSTITIAL_CLICK");
                    break;
                default:
                    TrackEvent(string.Format("ADS_{0}_CLICK", adsType.ToString().ToUpperInvariant()));
                    break;
            }

            {
                var parameters = new Dictionary<string, object>();
                parameters["af_adrev_ad_type"] = adsType.ToString();
                TrackLimitedEvent("af_ad_click", parameters);
            }
        }

        public virtual void TrackAdsReady(AFramework.Ads.AdsType adsType, Dictionary<string, object> args)
        {
            switch (adsType)
            {
                case Ads.AdsType.Banner:
                    TrackEvent("ADS_BANNER_REQUEST", args);
                    break;
                case Ads.AdsType.Interstitial:
                    TrackEvent("ADS_INTERSTITIAL_REQUEST", args);
                    break;
                case Ads.AdsType.RewardedVideo:
                    mRewardAdsTrackingCacheData = args;
                    TrackEvent("ADS_REWARD_REQUEST", args);
                    break;
                case Ads.AdsType.InterstitialRewardedVideo:
                    TrackEvent("ADS_REWARD_INTERSTITIAL_REQUEST", args);
                    break;
                case Ads.AdsType.OfferWall:
                    TrackEvent("ADS_OFFERWALL_REQUEST", args);
                    break;
                case Ads.AdsType.AppOpen:
                    TrackEvent("ADS_APPOPEN_REQUEST", args);
                    break;
                case Ads.AdsType.OverlayNative:
                    TrackEvent("ADS_OVERLAYNATIVE_REQUEST", args);
                    break;
                case Ads.AdsType.NativeInterstitial:
                    TrackEvent("ADS_NATIVEINTERSTITIAL_REQUEST", args);
                    break;
                default:
                    TrackEvent(string.Format("ADS_{0}_REQUEST", adsType.ToString().ToUpperInvariant()), args);
                    break;
            }
        }

        public virtual void TrackAdsDownloadFail(AFramework.Ads.AdsType adsType, string errorCode, string errorMessage)
        {
            var args = new Dictionary<string, object>();
            args["error"] = string.IsNullOrEmpty(errorCode) ? "unknown" : errorCode;
            HandleErrorMessageParam(ref args, errorMessage);
            switch (adsType)
            {
                case Ads.AdsType.Banner:
                    TrackEvent("ADS_BANNER_LOADFAIL", args);
                    break;
                case Ads.AdsType.Interstitial:
                    TrackEvent("ADS_INTERSTITIAL_LOADFAIL", args);
                    break;
                case Ads.AdsType.RewardedVideo:
                    TrackEvent("ADS_REWARD_LOADFAIL", args);
                    break;
                case Ads.AdsType.InterstitialRewardedVideo:
                    TrackEvent("ADS_REWARD_INTERSTITIAL_LOADFAIL", args);
                    break;
                case Ads.AdsType.OfferWall:
                    TrackEvent("ADS_OFFERWALL_LOADFAIL", args);
                    break;
                case Ads.AdsType.AppOpen:
                    TrackEvent("ADS_APPOPEN_LOADFAIL", args);
                    break;
                case Ads.AdsType.OverlayNative:
                    TrackEvent("ADS_OVERLAYNATIVE_LOADFAIL", args);
                    break;
                case Ads.AdsType.NativeInterstitial:
                    TrackEvent("ADS_NATIVEINTERSTITIAL_LOADFAIL", args);
                    break;
                default:
                    TrackEvent(string.Format("ADS_{0}_LOADFAIL", adsType.ToString().ToUpperInvariant()), args);
                    break;
            }
        }

        public virtual void TrackAdsShowFail(AFramework.Ads.AdsType adsType, string errorCode, string errorMessage)
        {
            var args = new Dictionary<string, object>();
            args["error"] = string.IsNullOrEmpty(errorCode) ? "unknown" : errorCode;
            HandleErrorMessageParam(ref args, errorMessage);
            switch (adsType)
            {
                case Ads.AdsType.Banner:
                    TrackEvent("ADS_BANNER_SHOWFAIL", args);
                    break;
                case Ads.AdsType.Interstitial:
                    TrackEvent("ADS_INTERSTITIAL_SHOWFAIL", args);
                    break;
                case Ads.AdsType.RewardedVideo:
                    TrackEvent("ADS_REWARD_SHOWFAIL", args);
                    break;
                case Ads.AdsType.InterstitialRewardedVideo:
                    TrackEvent("ADS_REWARD_INTERSTITIAL_SHOWFAIL", args);
                    break;
                case Ads.AdsType.OfferWall:
                    TrackEvent("ADS_OFFERWALL_SHOWFAIL", args);
                    break;
                case Ads.AdsType.AppOpen:
                    TrackEvent("ADS_APPOPEN_SHOWFAIL", args);
                    break;
                case Ads.AdsType.OverlayNative:
                    TrackEvent("ADS_OVERLAYNATIVE_SHOWFAIL", args);
                    break;
                case Ads.AdsType.NativeInterstitial:
                    TrackEvent("ADS_NATIVEINTERSTITIAL_SHOWFAIL", args);
                    break;
                default:
                    TrackEvent(string.Format("ADS_{0}_SHOWFAIL", adsType.ToString().ToUpperInvariant()), args);
                    break;
            }
        }

        static void HandleErrorMessageParam(ref Dictionary<string, object> args, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                args["message"] = "unknown";
                return;
            }

            const int CHUNK_SIZE = 95;
            if (message.Length <= CHUNK_SIZE)
            {
                args["message"] = message;
                return;
            }

            var splitMessage = Enumerable.Range(0, message.Length / CHUNK_SIZE)
            .Select(i => message.Substring(i * CHUNK_SIZE, CHUNK_SIZE)).ToArray();

            if (splitMessage == null || splitMessage.Length == 0)
            {
                args["message"] = message.Substring(0, CHUNK_SIZE);
                return;
            }

            for (int i = 0; i < splitMessage.Length && i < 15; ++i)
            {
                if (i == 0) args["message"] = splitMessage[i];
                else args["message" + (i + 1)] = splitMessage[i];
            }
        }

        public virtual void TrackAdsRewardSkip(Dictionary<string, object> args)
        {
            if (args == null) args = new Dictionary<string, object>();
            if (mRewardAdsTrackingCacheData != null)
            {
                foreach (var pair in mRewardAdsTrackingCacheData)
                {
                    args[pair.Key] = pair.Value;
                }
            }
            else
            {
                args["location"] = "unknown";
                args["type"] = "unknown";
            }
            TrackEvent("ADS_REWARD_SKIP", args);
        }

        public virtual void TrackAdsRevenue(Ads.AdsType adType, double revenue, string network, string currency, string adPlatform, string adUnitName, string adFormat, string placement)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic["value"] = revenue;
            if (!string.IsNullOrEmpty(network)) dic["network"] = network;
            if (!string.IsNullOrEmpty(currency)) dic["currency"] = currency;
            //Debug.Log(string.Format("ADS_{0}_ADVALUE", adType.ToString().ToUpper()) + " " + revenue.ToString() + " - " + currency + " - " + network);
            TrackEvent(string.Format("ADS_{0}_ADVALUE", adType.ToString().ToUpperInvariant()), dic);

#if USE_FIREBASE_ANALYTICS
            {
                Dictionary<string, object> dictFirebase = new Dictionary<string, object>();
                dictFirebase["ad_format"] = adType.ToString().ToLowerInvariant();
                dictFirebase["revenue"] = revenue;//Firebase.Analytics.FirebaseAnalytics.ParameterValue
                dictFirebase["currency"] = string.IsNullOrEmpty(currency) ? "USD" : currency;//Firebase.Analytics.FirebaseAnalytics.ParameterCurrency
                dictFirebase["level"] = PlayerPrefs.GetInt(FIREBASE_LEVEL_PLAYERPREFS, 0);//Firebase.Analytics.FirebaseAnalytics.ParameterLevel
                dictFirebase["level_mode"] = PlayerPrefs.GetString(FIREBASE_MODE_PLAYERPREFS, "unknown");
                dictFirebase["revenue"] = revenue;
                dictFirebase["currency"] = string.IsNullOrEmpty(currency) ? "USD" : currency;
                dictFirebase["placement"] = placement;
                dictFirebase["ad_network"] = network;
                AddAdRevenueSDKAdditionalParam(ref dictFirebase);
                Debug.Log($"{dictFirebase["level"]}|{dictFirebase["level_mode"]}|{dictFirebase["ad_format"]}|{dictFirebase["revenue"]}|{dictFirebase["currency"]}" +
                    $"|{dictFirebase["placement"]}|{dictFirebase["ad_network"]}");
                TrackEvent("ad_revenue_sdk", dictFirebase);
            }
#endif

#if USE_FIREBASE_ANALYTICS || USE_APPSFLYER_ANALYTICS
            {
                Dictionary<string, object> dictFirebase = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(adPlatform)) dictFirebase["ad_platform"] = adPlatform;
                if (!string.IsNullOrEmpty(network)) dictFirebase["ad_source"] = network;
                if (!string.IsNullOrEmpty(adUnitName)) dictFirebase["ad_unit_name"] = adUnitName;
                if (!string.IsNullOrEmpty(adFormat)) dictFirebase["ad_format"] = adFormat;
                dictFirebase["revenue"] = revenue;
                if (!string.IsNullOrEmpty(currency)) dictFirebase["currency"] = currency;
#if USE_FIREBASE_ANALYTICS
                TrackEvent("ad_impression", dictFirebase);
#endif
#if USE_APPSFLYER_ANALYTICS
                TrackLimitedEvent("ad_impression", dictFirebase);
#endif
            }
#endif

#if USE_APPSFLYER_ANALYTICS
            {
                Dictionary<string, string> dicApp = new Dictionary<string, string>();
                dicApp[AppsFlyerSDK.AdRevenueScheme.AD_TYPE] = adType.ToString();
                dicApp[AppsFlyerSDK.AdRevenueScheme.AD_UNIT] = adUnitName;
                dicApp["adType"] = adType.ToString();
                dicApp["ad_format"] = adFormat;
                
#if USE_APPLOVIN_ADS
                const AppsFlyerSDK.MediationNetwork MediationType = AppsFlyerSDK.MediationNetwork.ApplovinMax;
#elif USE_IRONSOURCE_ADS
                const AppsFlyerSDK.MediationNetwork MediationType = AppsFlyerSDK.MediationNetwork.IronSource;
#elif USE_ADMOB || USE_MOPUB_ADS || USE_FB_ADS || USE_TOPON_ADS || USE_UNITY_ADS
                TODO
#else
                const AppsFlyerSDK.MediationNetwork MediationType = AppsFlyerSDK.MediationNetwork.Custom;
#endif
                AppsFlyerSDK.AFAdRevenueData revData = new(network, MediationType, currency, revenue);
                AppsFlyerSDK.AppsFlyer.logAdRevenue(revData, dicApp);
            }
#if USE_APPSFLYER_ADSREVENUE
            if (adType != Ads.AdsType.Banner)//won't use banner
            {
                var afDict = new Dictionary<string, object>();
                afDict[AFInAppEvents.REVENUE] = revenue.ToString("F99", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0');
                if (!string.IsNullOrEmpty(currency)) afDict[AFInAppEvents.CURRENCY] = "USD";
                else afDict[AFInAppEvents.CURRENCY] = currency;

                TrackLimitedEvent("af_ad_value", afDict);
                if (adType == Ads.AdsType.Interstitial) TrackLimitedEvent("af_is_value", afDict);
                else if (adType == Ads.AdsType.RewardedVideo) TrackLimitedEvent("af_rv_value", afDict);
            }
#endif
#endif
        }
        #endregion

        public virtual void TrackMenuActiveTime(string menuName, float time, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["name"] = menuName;
            dic["time"] = time;
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            TrackEvent("MENU", dic);
        }

#if USE_FIREBASE_ANALYTICS
        public virtual void FirebaseTutorialBegin(Dictionary<string, object> additionalParams = null)
        {
            if (additionalParams != null)
            {
                TrackEvent("tutorial_begin", additionalParams);//Firebase.Analytics.FirebaseAnalytics.EventTutorialBegin
            }
            else
            {
                TrackEvent("tutorial_begin");//Firebase.Analytics.FirebaseAnalytics.EventTutorialBegin
            }
        }

        public virtual void FirebaseTutorialComplete(Dictionary<string, object> additionalParams = null)
        {
            if (additionalParams != null)
            {
                TrackEvent("tutorial_complete", additionalParams);//Firebase.Analytics.FirebaseAnalytics.EventTutorialComplete
            }
            else
            {
                TrackEvent("tutorial_complete");//Firebase.Analytics.FirebaseAnalytics.EventTutorialComplete
            }
        }

        public virtual void FirebaseLevelUp(string character, int level, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["character"] = character;//Firebase.Analytics.FirebaseAnalytics.ParameterCharacter
            dic["level"] = level;//Firebase.Analytics.FirebaseAnalytics.ParameterLevel
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            TrackEvent("level_up", dic);//Firebase.Analytics.FirebaseAnalytics.EventLevelUp
        }

        public virtual void FirebasePostScore(string character, int level, int score, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["character"] = character;//Firebase.Analytics.FirebaseAnalytics.ParameterCharacter
            dic["level"] = level;//Firebase.Analytics.FirebaseAnalytics.ParameterLevel
            dic["score"] = score;//Firebase.Analytics.FirebaseAnalytics.ParameterScore
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            TrackEvent("post_score", dic);//Firebase.Analytics.FirebaseAnalytics.EventPostScore
        }

        public virtual void FirebaseEarnVirtualCurrency(string _type, int _amount, string _reason, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["virtual_currency_name"] = _type;//Firebase.Analytics.FirebaseAnalytics.ParameterVirtualCurrencyName
            dic["value"] = _amount;//Firebase.Analytics.FirebaseAnalytics.ParameterValue
            dic["reason"] = _reason;
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            if (!dic.ContainsKey("max_level")) dic["max_level"] = PlayerPrefs.GetInt(FIREBASE_MAXLEVEL_PLAYERPREFS, 0);
            TrackEvent("earn_virtual_currency", dic);//Firebase.Analytics.FirebaseAnalytics.EventEarnVirtualCurrency
        }

        public virtual void FirebaseSpendVirtualCurrency(string _type, int _amount, string _reason, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["virtual_currency_name"] = _type;//Firebase.Analytics.FirebaseAnalytics.ParameterVirtualCurrencyName
            dic["value"] = _amount;//Firebase.Analytics.FirebaseAnalytics.ParameterValue
            dic["item_name"] = _reason;//Firebase.Analytics.FirebaseAnalytics.ParameterItemName
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            if (!dic.ContainsKey("max_level")) dic["max_level"] = PlayerPrefs.GetInt(FIREBASE_MAXLEVEL_PLAYERPREFS, 0);
            TrackEvent("spend_virtual_currency", dic);//Firebase.Analytics.FirebaseAnalytics.EventSpendVirtualCurrency
        }

        public virtual void FirebaseSelectContent(string content_type, string item_id, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["content_type"] = content_type;//Firebase.Analytics.FirebaseAnalytics.ParameterContentType
            dic["item_id"] = item_id;//Firebase.Analytics.FirebaseAnalytics.ParameterItemId
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            TrackEvent("select_content", dic);//Firebase.Analytics.FirebaseAnalytics.EventSelectContent
        }

        public virtual void FirebaseLevelStart(int currentLevel, string currentMode, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["level"] = currentLevel;//Firebase.Analytics.FirebaseAnalytics.ParameterLevel
            dic["level_mode"] = currentMode;
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            TrackEvent("level_start", dic);//Firebase.Analytics.FirebaseAnalytics.EventLevelStart
            PlayerPrefs.SetInt(FIREBASE_LEVEL_PLAYERPREFS, currentLevel);
            PlayerPrefs.SetInt(FIREBASE_MAXLEVEL_PLAYERPREFS, Mathf.Max(currentLevel, PlayerPrefs.GetInt(FIREBASE_MAXLEVEL_PLAYERPREFS, 0)));
            PlayerPrefs.SetString(FIREBASE_MODE_PLAYERPREFS, currentMode);
            if (AFramework.Ads.AdsManager.IsInstanceValid())
            {
                AFramework.Ads.AdsManager.I.SetSafeState(false);
            }
        }

        public virtual void FirebaseLevelEnd(int currentLevel, string currentMode, bool success, Dictionary<string, object> additionalParams = null)
        {
            var dic = new Dictionary<string, object>();
            dic["level"] = currentLevel;//Firebase.Analytics.FirebaseAnalytics.ParameterLevel
            dic["level_mode"] = currentMode;
            dic["success"] = success.ToString();//Firebase.Analytics.FirebaseAnalytics.ParameterSuccess
            if (additionalParams != null)
            {
                foreach (var pair in additionalParams)
                {
                    dic[pair.Key] = pair.Value;
                }
            }
            TrackEvent("level_end", dic);//Firebase.Analytics.FirebaseAnalytics.EventLevelEnd

            RequestATTracking(0);
            if (AFramework.Ads.AdsManager.IsInstanceValid())
            {
                AFramework.Ads.AdsManager.I.SetSafeState(true);
            }
        }
#endif
        public virtual bool UpdateCustomRuleData(bool remote, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                appsflyerCustomEventList = new Dictionary<string, List<CustomRuleEvent>>();
                return true;
            }
            CustomRuleEvent[] eventData = JsonHelper.getJsonArray<CustomRuleEvent>(data);
            var newEventList = new Dictionary<string, List<CustomRuleEvent>>();
            for (int i = 0; i < eventData.Length; i++)
            {
                if (eventData[i].Build())
                {
                    if (!newEventList.ContainsKey(eventData[i].eventName))
                    {
                        newEventList[eventData[i].eventName] = new List<CustomRuleEvent>();
                    }
                    newEventList[eventData[i].eventName].Add(eventData[i]);
                }
            }
            appsflyerCustomEventList = newEventList;
            return true;
        }

        void TrackCustomRuleLimitedEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!appsflyerCustomEventList.ContainsKey(eventName)) return;
            var listEvent = appsflyerCustomEventList[eventName];
            for (int i = 0; i < listEvent.Count; ++i)
            {
                var result = listEvent[i].ProcessEvent(parameters);
                if (string.IsNullOrEmpty(result)) continue;
                TrackLimitedEvent(result);
            }

        }

        public virtual bool TrackFirstLaunch()
        {
            if (PlayerPrefs.GetInt(FIRST_LAUNCH, 0) == 0)
            {
                TrackEvent(FIRST_LAUNCH);
#if UNITY_ANDROID
                TrackHardwareInfo();
#endif
                PlayerPrefs.SetInt(FIRST_LAUNCH, 1);

                if (string.IsNullOrEmpty(PlayerPrefs.GetString(FIRST_VERSION, null)))
                {
                    PlayerPrefs.SetString(FIRST_VERSION, Application.version);
                }
                return true;
            }
            return false;
        }

        protected virtual void TrackHardwareInfo()
        {
            var info = Utility.GetHardwareInfo();
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["soc"] = info.soc;
            dict["gpu"] = info.gpu;
            dict["ram"] = info.ram;
            TrackEvent("HARDWAREINFO", dict);
        }

        protected virtual void CCAddIAPPurchaseAdditionalParam(IAP.PackageInfo iapInfo, ref Dictionary<string, object> currentParams)
        {

        }

        protected virtual void AddAdRevenueSDKAdditionalParam(ref Dictionary<string, object> currentParams)
        {

        }

#if UNITY_EDITOR
        void Reset()
        {
            _Config = FrameworkGlobalConfig.Instance;
        }
#endif
    }
}
