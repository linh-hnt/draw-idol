using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if USE_APPSFLYER_ANALYTICS
namespace AFramework.Analytics
{
    [System.Serializable]
    public class AFAttributeData
    {
        //https://support.appsflyer.com/hc/en-us/articles/360000726098-Conversion-data-payloads-and-scenarios
        //https://support.appsflyer.com/hc/en-us/articles/207447163#attribution-link-parameters
        //sample: https://app.appsflyer.com/com.merge.superhero?pid=appsflyer_sdk_test_int&clickid=ede32d87-e61a-4461-aec0-4ec7e6f6ec92&af_r=https%3A%2F%2Fsdktest.appsflyer.com%2Fsdk-integration-test%2Finstall%2Flaunch%3Fsid%3Dede32d87-e61a-4461-aec0-4ec7e6f6ec92%26store%3DandroidOther&ts=1669437437&advertising_id=6e56c1f0-d658-45bd-84ed-a01233cec844&af_ad=Video_Portrait_Long_3PPlayableEndcard_BCi_UAC2%20%2B%20BubbleClassic_VA_0529_EN_al&af_ad_type=INTER&af_channel=video&af_siteid=c252d48a16f0fee1c29e63e9206e675b&af_c_id=06766754d677036fef5643bc84e79bc1&af_adset=_DEFAULT&c=BCi_US_040521&af_ad_id=14507906&af_click_lookback=7d&idfa=&af_ip=172.222.236.7&af_lang=en_US&af_ua=Mozilla%252F5.0%2B%2528iPhone%253B%2BCPU%2BiPhone%2BOS%2B16_1_1%2Blike%2BMac%2BOS%2BX%2529%2BAppleWebKit%252F605.1.15%2B%2528KHTML%252C%2Blike%2BGecko%2529%2BMobile%252F15E148

        public string RawData { get; set; }

        [SerializeField] string adgroup_id;
        [SerializeField] string af_adgroup_id;
        public string AdGroupID
        {
            get
            {
                if (!string.IsNullOrEmpty(adgroup_id)) return adgroup_id;
                return af_adgroup_id;
            }
        }

        [SerializeField] string adset;
        [SerializeField] string af_adset;
        public string Adset
        {
            get
            {
                if (!string.IsNullOrEmpty(adset)) return adset;
                return af_adset;
            }
        }

        [SerializeField] string adset_id;
        [SerializeField] string af_adset_id;
        public string AdsetID
        {
            get
            {
                if (!string.IsNullOrEmpty(adset_id)) return adset_id;
                return af_adset_id;
            }
        }

        [SerializeField] string af_siteid;
        public string SiteID { get { return af_siteid; } }

        [SerializeField] string af_status;//"Non-organic"
        public string Status { get { return af_siteid; } }

        [SerializeField] string af_sub1;
        public string SubParam1 { get { return af_sub1; } }
        [SerializeField] string af_sub2;
        public string SubParam2 { get { return af_sub2; } }
        [SerializeField] string af_sub3;
        public string SubParam3 { get { return af_sub3; } }
        [SerializeField] string af_sub4;
        public string SubParam4 { get { return af_sub4; } }
        [SerializeField] string af_sub5;
        public string SubParam5 { get { return af_sub5; } }

        [SerializeField] string agency;
        public string Agency { get { return agency; } }

        [SerializeField] string c;
        [SerializeField] string campaign;
        public string Campaign
        {
            get
            {
                if (!string.IsNullOrEmpty(c)) return c;
                return campaign;
            }
        }

        [SerializeField] string campaign_id;
        [SerializeField] string af_c_id;
        public string CampaignID
        {
            get
            {
                if (!string.IsNullOrEmpty(campaign_id)) return campaign_id;
                return af_c_id;
            }
        }

        [SerializeField] string click_time;
        public string ClickTime { get { return click_time; } }

        [SerializeField] string http_referrer;
        public string HttpReferrer { get { return http_referrer; } }

        [SerializeField] string install_time;
        public string InstallTime { get { return install_time; } }

        [SerializeField] bool is_first_launch;
        public bool IsFirstLaunch { get { return is_first_launch; } }

        [SerializeField] string media_source;
        public string MediaSource { get { return media_source; } }

        [SerializeField] string retargeting_conversion_type;
        public string RetargetingConversionType { get { return retargeting_conversion_type; } }

        [SerializeField] string ad;
        [SerializeField] string af_ad;
        public string Ad
        {
            get
            {
                if (!string.IsNullOrEmpty(af_ad)) return af_ad;
                return ad;
            }
        }

        [SerializeField] string af_ad_id;
        public string AdId { get { return af_ad_id; } }

        [SerializeField] string af_ad_type;
        public string AdType { get { return af_ad_type; } }

        [SerializeField] string af_channel;
        [SerializeField] string channel;
        public string Channel
        {
            get
            {
                if (!string.IsNullOrEmpty(af_channel)) return af_channel;
                return channel;
            }
        }

        [SerializeField] string af_click_lookback;//Attribution lookback window
        public string ClickLookback { get { return af_click_lookback; } }

        [SerializeField] string af_cost_currency;
        [SerializeField] string cost_currency;
        public string CostCurrency
        {
            get
            {
                if (!string.IsNullOrEmpty(af_cost_currency)) return af_cost_currency;
                return cost_currency;
            }
        }
    }

    public class AppsFlyerAnalytics : MonoBehaviour,
        AppsFlyerSDK.IAppsFlyerConversionData,
        AppsFlyerSDK.IAppsFlyerUserInvite,
        AppsFlyerSDK.IAppsFlyerPurchaseValidation,
        IAnalytic
    {
        public static System.Action<AFAttributeData, string> EventOnAppResume;
        public bool InitSuccess { get; set; }
        public string UDID { get { return InitSuccess ? AppsFlyerSDK.AppsFlyer.getAppsFlyerId() : null; } }

        public void ApplicationOnPause(bool Paused)
        {

        }

        public void Init(params string[] args)
        {
            //Mandatory - set your AppsFlyer’s Developer key.
            string appflyerKey = args[0];
            string appId = args[1];
            if (appflyerKey == "" || appId == "") return;

#if DEVELOPMENT_BUILD
            AppsFlyerSDK.AppsFlyer.setIsDebug(true);
#endif

#if UNITY_ANDROID && !USE_FIREBASE && USE_APPSFLYER_CHINA
            if (!string.IsNullOrEmpty(TrackingManager.I.Config.androidAFStore))
            {
                AppsFlyerSDK.AppsFlyer.setOutOfStore(TrackingManager.I.Config.androidAFStore);
            }

            AppsFlyerSDK.AppsFlyer.setCollectOaid(true);
            AppsFlyerSDK.AppsFlyer.setCollectIMEI(true);
            AppsFlyerSDK.AppsFlyer.setCollectAndroidID(true);
#endif

            AppsFlyerSDK.AppsFlyer.initSDK(appflyerKey, appId, this);
#if UNITY_IOS
            if (iOSATTConfig.IsOSReady() && Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                AppsFlyerSDK.AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(TrackingManager.I.Config.iosATT.AppsflyerWaitTime);
            }
#endif
            AppsFlyerSDK.AppsFlyerPurchaseConnector.init(this, AppsFlyerSDK.Store.GOOGLE);
#if DEVELOPMENT_BUILD
            AppsFlyerSDK.AppsFlyerPurchaseConnector.setIsSandbox(true);
#endif
            AppsFlyerSDK.AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue
                (AppsFlyerSDK.AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions,
                AppsFlyerSDK.AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases);
            AppsFlyerSDK.AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
            AppsFlyerSDK.AppsFlyerPurchaseConnector.build();
            AppsFlyerSDK.AppsFlyerPurchaseConnector.startObservingTransactions();

            AppsFlyerSDK.AppsFlyer.startSDK();

            InitSuccess = true;

#if APPSFLYER_UNINSTALL_EVENT
#if USE_FIREBASE_MESSAGING && UNITY_ANDROID && !UNITY_EDITOR
            if (AFramework.FirebaseService.FirebaseMessaging.FirebaseMessagingToken == null || AFramework.FirebaseService.FirebaseMessaging.FirebaseMessagingToken.Length <= 0)
            {
                AFramework.FirebaseService.FirebaseMessaging.EventOnTokenReceived += OnFirebaseMessagingTokenReceived;
            }
            else
            {
                AppsFlyerSDK.AppsFlyer.updateServerUninstallToken(AFramework.FirebaseService.FirebaseMessaging.FirebaseMessagingToken);
            }
#elif UNITY_IOS
            UnityEngine.iOS.NotificationServices.RegisterForNotifications(UnityEngine.iOS.NotificationType.Alert | UnityEngine.iOS.NotificationType.Badge | UnityEngine.iOS.NotificationType.Sound, true);
            TrackingManager.I.StartCoroutine(CRWaitForNotificationToken());
#endif
#endif
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            Dictionary<string, string> temp = parameters.ToDictionary(x => x.Key.ToString(), x => System.Convert.ToString(x.Value, System.Globalization.CultureInfo.InvariantCulture));
            AppsFlyerSDK.AppsFlyer.sendEvent(eventName, temp);
        }

        public void TrackEvent(string eventName)
        {
            var tempDictionary = new Dictionary<string, string>();
            AppsFlyerSDK.AppsFlyer.sendEvent(eventName, tempDictionary);
        }

        public void generateAppsFlyerLink()
        {
            //Dictionary<string, string> parameters = new Dictionary<string, string>();
            //parameters.Add("channel", "some_channel");
            //parameters.Add("campaign", "some_campaign");
            //parameters.Add("additional_param1", "some_param1");
            //parameters.Add("additional_param2", "some_param2");

            //// other params
            ////parameters.Add("referrerName", "some_referrerName");
            ////parameters.Add("referrerImageUrl", "some_referrerImageUrl");
            ////parameters.Add("customerID", "some_customerID");
            ////parameters.Add("baseDeepLink", "some_baseDeepLink");
            ////parameters.Add("brandDomain", "some_brandDomain");


            //AppsFlyerSDK.AppsFlyer.generateUserInviteLink(parameters, this);
        }

        public void onInviteLinkGenerated(string link)
        {
            //AppsFlyerSDK.AppsFlyer.AFLog("onInviteLinkGenerated", link);
        }

        public void onInviteLinkGeneratedFailure(string error)
        {
            //AppsFlyerSDK.AppsFlyer.AFLog("onInviteLinkGeneratedFailure", error);
        }

        public void onOpenStoreLinkGenerated(string link)
        {
            //printCallback("onOpenStoreLinkGenerated:: generated store link " + link);
            Application.OpenURL(link);
        }

        public void onConversionDataSuccess(string conversionData)
        {
            string notificationData = null;
#if USE_UNITY_NOTIFICATION
#if UNITY_ANDROID
            var androidNotification = Unity.Notifications.Android.AndroidNotificationCenter.GetLastNotificationIntent();
            if (androidNotification != null)
            {
                notificationData = androidNotification.Notification.IntentData;
            }
#elif UNITY_IOS
            var iosNotification = Unity.Notifications.iOS.iOSNotificationCenter.GetLastRespondedNotification();
            if (iosNotification != null && !string.IsNullOrEmpty(iosNotification.Data))
            {
                notificationData = iosNotification.Data;
            }
#endif
#endif
            AFAttributeData conversion = null;
            if (string.IsNullOrEmpty(conversionData))
            {
                conversion = new AFAttributeData();
            }
            else
            {
                conversion = JsonUtility.FromJson<AFAttributeData>(conversionData);
            }
            EventOnAppResume?.Invoke(conversion, notificationData);
        }

        public void onConversionDataFail(string error)
        {
            //throw new System.NotImplementedException();
        }

        public void onAppOpenAttribution(string attributionData)
        {
            //throw new System.NotImplementedException();
        }

        public void onAppOpenAttributionFailure(string error)
        {
            //throw new System.NotImplementedException();
        }

#if APPSFLYER_UNINSTALL_EVENT
#if USE_FIREBASE_MESSAGING && UNITY_ANDROID && !UNITY_EDITOR
        public void OnFirebaseMessagingTokenReceived(Firebase.Messaging.TokenReceivedEventArgs token)
        {
            AppsFlyerSDK.AppsFlyer.updateServerUninstallToken(token.Token);
        }
#endif
#if UNITY_IOS
        IEnumerator CRWaitForNotificationToken()
        {
            WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(0.2f);
            byte[] token = null;
            do
            {
                yield return waitTime;
                token = UnityEngine.iOS.NotificationServices.deviceToken;
            } while (token == null);
            AppsFlyerSDK.AppsFlyer.registerUninstall(token);
        }

#endif
#endif

        public static string GetConversionData()
        {
            return UnityEngine.PlayerPrefs.GetString("AF_CONVERSION_DATA", string.Empty);
        }

        public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
        {
#if UNITY_ANDROID
            Debug.Log("didReceivePurchaseRevenueValidationInfo " + validationInfo);
#endif
        }

        public void didReceivePurchaseRevenueError(string error)
        {
#if UNITY_ANDROID
            Debug.Log("didReceivePurchaseRevenueError " + error);
#endif
        }
    }
}
#endif