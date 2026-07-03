#if USE_APPLOVIN_ADS
using AFramework.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.Ads
{
    public class AppLovinAdapter : BaseAdsAdapter
    {
        protected BaseAdapterConfig Config { get; private set; }

        // Cache banner width để chỉ tính toán 1 lần
        private static float cachedBannerWidth = -1f;

#if AMAZON_APS
        AmazonAds.APSBannerAdRequest amazonBannerAdRequest;
        AmazonAds.APSVideoAdRequest amazonInterstitialAdRequest;
        AmazonAds.APSVideoAdRequest amazonRewardedAdRequest;
#endif

        public override void Init(object[] parameters)
        {
            mConfig = ((BaseAdapterConfig)parameters[0]);
            Config = mConfig;// (AppLovinAdapterConfig)mConfig;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += HandleOnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += HandleOnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += HandleOnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += HandleOnBannerAdRevenuePaidEvent;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += HandleOnInterstitialAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += HandleOnInterstitialAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += HandleOnInterstitialAdFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += HandleOnInterstitialAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += HandleOnInterstitialAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += HandleOnInterstitialAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += HandleOnInterstitialAdRevenuePaidEvent;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += HandleOnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += HandleOnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += HandleOnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += HandleOnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += HandleOnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += HandleOnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += HandleOnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += HandleOnRewardedAdRevenuePaidEvent;

            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += HandleOnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += HandleOnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += HandleOnAppOpenAdFailedToDisplayEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += HandleOnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += HandleOnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += HandleOnAppOpenAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += HandleOnAppOpenAdRevenuePaidEvent;

            var cacheParameters = parameters;
            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                if (AdsManager.Debugging) MaxSdk.ShowMediationDebugger();
#if UNITY_IOS
                var attStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                if (attStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
#if !ADMOB_CONSENT
                    MaxSdk.SetHasUserConsent(attStatus == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);
#endif
#if FACEBOOK_AUDIENCENETWORK
                    AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(attStatus == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);
#endif
                }
#endif
                MaxSdk.SetMuted(mIsMuted);
                base.Init(cacheParameters);
                StartCoroutine(AFramework.Utility.CRDelayFunction(0.1f, () =>
                {
                    SetBannerPosition(mBannerPosition);
                }));
            };

            //MaxSdk.SetSdkKey(Config.Platform.ApplovingSDKKey);
            //MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[] { "e553767b-09b7-4e75-800e-cb59add4309f" });

#if AMAZON_APS && !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (Config.Platform.AmazonConfig != null && !string.IsNullOrEmpty(Config.Platform.AmazonConfig.appId))
            {
                AmazonAds.Amazon.Initialize(Config.Platform.AmazonConfig.appId);
                AmazonAds.Amazon.SetAdNetworkInfo(new AmazonAds.AdNetworkInfo(AmazonAds.DTBAdNetwork.MAX));
                AmazonAds.Amazon.SetMRAIDPolicy(AmazonAds.Amazon.MRAIDPolicy.CUSTOM);
                AmazonAds.Amazon.SetMRAIDSupportedVersions(new string[] { "1.0", "2.0", "3.0" });
                if (AdsManager.Debugging)
                {
                    AmazonAds.Amazon.EnableTesting(true);
                    AmazonAds.Amazon.EnableLogging(true);
                }
            }
#endif
#if ADMOB_CONSENT
            if (
#if ADMOB_ATT
                true ||
#endif
                AdsManager.I.IsATTAsked)
            {
                var request = new GoogleMobileAds.Ump.Api.ConsentRequestParameters();
                GoogleMobileAds.Ump.Api.ConsentInformation.Update(request, OnConsentInfoUpdated);
            }
            else
#endif
            {
                MaxSdk.InitializeSdk();
            }
        }

#if ADMOB_CONSENT
        void OnConsentInfoUpdated(GoogleMobileAds.Ump.Api.FormError consentError)
        {
            if (consentError != null)
            {
                // Handle the error.
                if (consentError.ErrorCode == 2 || consentError.ErrorCode == 4)
                {
                    UnityEngine.Debug.LogWarning("OnConsentInfoUpdated " + consentError.ErrorCode + " - " + consentError.Message);
                    System.Action callback = () =>
                    {
                        var request = new GoogleMobileAds.Ump.Api.ConsentRequestParameters();
                        GoogleMobileAds.Ump.Api.ConsentInformation.Update(request, OnConsentInfoUpdated);
                    };
                    StartCoroutine(CRRetryConsent(3, callback));
                }
                else
                {
                    UnityEngine.Debug.LogError("OnConsentInfoUpdated " + consentError.ErrorCode + " - " + consentError.Message);
                }
                return;
            }

            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.

            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            LoadAndShowConsentFormIfRequired();
        }

        void LoadAndShowConsentFormIfRequired()
        {
            GoogleMobileAds.Ump.Api.ConsentForm.LoadAndShowConsentFormIfRequired((GoogleMobileAds.Ump.Api.FormError formError) =>
            {
                if (formError != null)
                {
                    // Consent gathering failed.
                    if (formError.ErrorCode == 2 || formError.ErrorCode == 4)
                    {
                        UnityEngine.Debug.LogWarning("LoadAndShowConsentFormIfRequired " + formError.ErrorCode + " - " + formError.Message);
                        StartCoroutine(CRRetryConsent(3, LoadAndShowConsentFormIfRequired));
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("LoadAndShowConsentFormIfRequired " + formError.ErrorCode + " - " + formError.Message);
                    }
                    return;
                }

                // Consent has been gathered.
                if (GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds())
                {
                    UnityMainThreadDispatcher.instance.Enqueue(() => { MaxSdk.InitializeSdk(); });
                }
            });
        }

        IEnumerator CRRetryConsent(float delay, System.Action callback)
        {
            var waitTime = new WaitForSeconds(delay);
            do
            {
                yield return waitTime;
            }
            while (!AFramework.Utility.DelayHasInternet());
            callback?.Invoke();
        }
#endif

        bool mIsMuted = false;
        public void SetMute(bool mute)
        {
            mIsMuted = mute;
            MaxSdk.SetMuted(mIsMuted);
        }

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            return new MaxAdStatusHandler(type, id);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            base.DownloadAd(ad);
            switch (ad._Type)
            {
                case AdsType.Banner:
                    var dpi = Screen.dpi;
                    ////Debug.Log("dpi " + dpi);


                    MaxSdk.AdViewConfiguration bannerConfig = null;

                    // Reference: 1920x1080 @ 160dpi -> banner = 320dp (ratio = 320/1920)
                    // Scale banner width proportionally to maintain same visual ratio on any screen/DPI.
                    const float REF_SCREEN_WIDTH_PX = 1920f;
                    const float REF_BANNER_WIDTH_DP = 320f;
                    // ref screen width in dp = REF_SCREEN_WIDTH_PX / (REF_DPI / 160)
                    const float REF_SCREEN_WIDTH_DP = REF_SCREEN_WIDTH_PX; // = 1920dp at 160dpi
                    float bannerRatio = REF_BANNER_WIDTH_DP / REF_SCREEN_WIDTH_DP; // = 320/1920

                    if (mDefaultBannerAdList.Count > 1 && ad._Id.Equals(mDefaultBannerAdList[1]._Id))
                    {
                        bannerConfig = new MaxSdkBase.AdViewConfiguration(AdapterBannerPosition(BannerPosition.Top));
                        bannerConfig.IsAdaptive = false;

                        if (Screen.width > Screen.height)
                        {
                            float banner_width = GetCachedBannerWidth(dpi, bannerRatio);
                            MaxSdk.SetBannerWidth(ad._Id, banner_width);
                        }
                    }
                    else
                    {
                        bannerConfig = new MaxSdkBase.AdViewConfiguration(AdapterBannerPosition(mBannerPosition));
                        bannerConfig.IsAdaptive = false;

                        if (Screen.width > Screen.height)
                        {
                            float banner_width = GetCachedBannerWidth(dpi, bannerRatio);
                            MaxSdk.SetBannerWidth(ad._Id, banner_width);
                        }
                    }

#if AMAZON_APS && !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                    if (amazonBannerAdRequest == null && Config.Platform.AmazonConfig != null && !string.IsNullOrEmpty(Config.Platform.AmazonConfig.banner))
                    {
                        int bannerWidth = 320;//Mathf.Min(Screen.width, Screen.height);
                        int bannerHeight = 50;//Mathf.FloorToInt(bannerWidth * 50f / 320);
                        string maxAdId = ad._Id;

                        amazonBannerAdRequest = new AmazonAds.APSBannerAdRequest(bannerWidth, bannerHeight, Config.Platform.AmazonConfig.banner);
                        amazonBannerAdRequest.onFailedWithError += (adError) => {
                            if (AdsManager.Debugging) Debug.Log("AmazonAds.APSBannerAdRequest onFailedWithError " + adError.GetCode());
                            MaxSdk.SetBannerLocalExtraParameter(ad._Id, "amazon_ad_error", adError.GetAdError());
                            MaxSdk.CreateBanner(maxAdId, bannerConfig);
                        };
                        amazonBannerAdRequest.onSuccess += (adResponse) => {
                            MaxSdk.SetBannerLocalExtraParameter(ad._Id, "amazon_ad_response", adResponse.GetResponse());
                            MaxSdk.CreateBanner(maxAdId, bannerConfig);
                        };

                        amazonBannerAdRequest.LoadAd();
                    }
                    else
#endif
                    {
                        MaxSdk.CreateBanner(ad._Id, bannerConfig);
                        MaxSdk.SetBannerExtraParameter(ad._Id, "adaptive_banner", "true");
                    }
#if UNITY_EDITOR
                    HandleOnBannerAdLoadedEvent(ad._Id, null);

                    //correct fake banner width
                    var allCanvas = Resources.FindObjectsOfTypeAll(typeof(Canvas)) as Canvas[];
                    var bannerName = mBannerPosition == BannerPosition.Bottom ? "BannerBottom(Clone)" : "BannerTop(Clone)";
                    for (int i = 0; i < allCanvas.Length; ++i)
                    {
                        var obj = allCanvas[i];
                        if (obj.name == bannerName)
                        {
                            try
                            {
                                var bannerRect = obj.transform.GetChild(0).GetComponent<RectTransform>();
                                bannerRect.anchorMin = new Vector2(0.5f, bannerRect.anchorMin.y);
                                bannerRect.anchorMax = new Vector2(0.5f, bannerRect.anchorMax.y);
                                var bannerSizeDelta = new Vector2(bannerRect.sizeDelta.y / 50f * 320, bannerRect.sizeDelta.y);
                                if (AFramework.UI.CanvasManager.UICanvas != null)
                                {
                                    var uiHeight = AFramework.UI.CanvasManager.UIRectTrans.rect.height;
                                    bannerRect.localScale /= (uiHeight / Screen.height);
                                }
                                bannerRect.sizeDelta = bannerSizeDelta;
                            }
                            catch (Exception e)
                            {

                            }
                            break;
                        }
                    }
#endif
                    break;
                case AdsType.Interstitial:
#if AMAZON_APS && !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                    if (amazonInterstitialAdRequest == null && Config.Platform.AmazonConfig != null && !string.IsNullOrEmpty(Config.Platform.AmazonConfig.interstitial))
                    {
                        string maxAdId = ad._Id;
                        if (Screen.width > Screen.height)
                            amazonInterstitialAdRequest = new AmazonAds.APSVideoAdRequest(480, 320, Config.Platform.AmazonConfig.interstitial);
                        else
                            amazonInterstitialAdRequest = new AmazonAds.APSVideoAdRequest(320, 480, Config.Platform.AmazonConfig.interstitial);

                        amazonInterstitialAdRequest.onSuccess += (adResponse) => {
                            MaxSdk.SetInterstitialLocalExtraParameter(maxAdId, "amazon_ad_response", adResponse.GetResponse());
                            MaxSdk.LoadInterstitial(maxAdId);
                        };
                        amazonInterstitialAdRequest.onFailedWithError += (adError) => {
                            if (AdsManager.Debugging) Debug.Log("AmazonAds InterVideo onFailedWithError " + adError.GetCode());
                            MaxSdk.SetInterstitialLocalExtraParameter(maxAdId, "amazon_ad_error", adError.GetAdError());
                            MaxSdk.LoadInterstitial(maxAdId);
                        };
                        amazonInterstitialAdRequest.LoadAd();
                    }
                    else
#endif
                    {
                        MaxSdk.LoadInterstitial(ad._Id);
                    }
                    break;
                case AdsType.RewardedVideo:
#if AMAZON_APS && !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                    if (amazonRewardedAdRequest == null && Config.Platform.AmazonConfig != null && !string.IsNullOrEmpty(Config.Platform.AmazonConfig.reward))
                    {
                        string maxAdId = ad._Id;
                        if (Screen.width > Screen.height)
                            amazonRewardedAdRequest = new AmazonAds.APSVideoAdRequest(480, 320, Config.Platform.AmazonConfig.reward);
                        else
                            amazonRewardedAdRequest = new AmazonAds.APSVideoAdRequest(320, 480, Config.Platform.AmazonConfig.reward);

                        amazonRewardedAdRequest.onSuccess += (adResponse) =>
                        {
                            MaxSdk.SetRewardedAdLocalExtraParameter(maxAdId, "amazon_ad_response", adResponse.GetResponse());
                            MaxSdk.LoadRewardedAd(maxAdId);
                        };
                        amazonRewardedAdRequest.onFailedWithError += (adError) =>
                        {
                            if (AdsManager.Debugging) Debug.Log("AmazonAds RewardVideo onFailedWithError " + adError.GetCode());
                            MaxSdk.SetRewardedAdLocalExtraParameter(maxAdId, "amazon_ad_error", adError.GetAdError());
                            MaxSdk.LoadRewardedAd(maxAdId);
                        };
                        amazonRewardedAdRequest.LoadAd();
                    }
                    else
#endif
                    {
                        MaxSdk.LoadRewardedAd(ad._Id);
                    }
                    break;
                case AdsType.AppOpen:
                    MaxSdk.LoadAppOpenAd(ad._Id);
                    break;
            }
        }

        #region Banner
        bool bannerPositionSet = false;
        Vector2 bannerLastPosition = Vector2.zero;
        const float REF_DPI = 160f;

        /// <summary>
        /// Convert physical pixels to logical units used by MAX SDK's SetBannerWidth:
        ///   Android → dp  (MaxSdkAndroid: "width in dp", baseline 160 ppi, float)
        ///   iOS     → pt  (MaxSdkiOS: "width in points", UIScreen.scale is integer 1/2/3)
        ///
        /// iOS UIScreen.scale thresholds (from Apple device specs):
        ///   @1x: non-retina iPad/iPhone  ~132–163 ppi  → scale 1
        ///   @2x: Retina iPad/iPhone      ~264–326 ppi  → scale 2
        ///   @3x: iPhone Plus/Pro/Max     ~401–460 ppi  → scale 3
        /// Using threshold-based instead of rounding because e.g. iPhone 6+/7+/8+
        /// reports 401 ppi but is @3x — RoundToInt(401/163)=2 would be wrong.
        /// </summary>
        static float CalcScreenDensity(float dpi)
        {
            if (dpi <= 0) return 1f;
#if UNITY_IOS
            // threshold boundaries: <200 = @1x, 200–380 = @2x, >380 = @3x
            // if (dpi < 200f) return 1f;
            // if (dpi < 380f) return 2f;
            // return 3f;

            // float rawScale = dpi / 163f;
            // if (rawScale < 1.5f) return 1f;
            // if (rawScale < 2.3f) return 2f;
            // return 3f;

            int maxDim = Mathf.Max(Screen.width, Screen.height);
            int minDim = Mathf.Min(Screen.width, Screen.height);
            float aspect = minDim > 0 ? (float)maxDim / minDim : 2f;
            bool isIPad = aspect < 1.6f; // iPad ≤ 1.43, iPhone ≥ 1.78

            if (isIPad)
            {
                return maxDim >= 2048 ? 2f : 1f; // iPad 3/Air/mini2+/Pro → 2x; iPad 1/2 → 1x
            }
            if (maxDim >= 2000) return 3f;   // iPhone 6+/X/11 Pro/12/13/14/15/16/17 series (3x)
            if (maxDim >= 1136) return 2f;   // iPhone 5/6/7/8/SE/11 series (2x)
            return 1f;
#else
            // Android: dp = px / (dpi / 160)
            return dpi / REF_DPI;
#endif
        }

        /// <summary>
        /// Lay cached banner width, chi tinh toan 1 lan roi dung mai.
        /// Tranh recalculate moi lan refresh.
        /// </summary>
        private float GetCachedBannerWidth(float dpi, float bannerRatio)
        {
            // Neu chua cache, tinh toan 1 lan
            if (cachedBannerWidth < 0)
            {
                float density = CalcScreenDensity(dpi);
                float screenWidthDp = Screen.width / density;
                cachedBannerWidth = Mathf.Floor(screenWidthDp * bannerRatio);
                cachedBannerWidth = Mathf.Min(cachedBannerWidth, 320f);  // Cap toi da 320dp
                if (AdsManager.Debugging) Debug.Log("[Banner] Calculated banner width: " + cachedBannerWidth + "dp");
            }

            //Debug.Log("[Banner] Cached banner width: " + cachedBannerWidth + "dp"+" (dpi: " + dpi + ", density: " + CalcScreenDensity(dpi) + ", screenWidthDp: " + (Screen.width / CalcScreenDensity(dpi)) + ", bannerRatio: " + bannerRatio + ")");

            return cachedBannerWidth;
        }

        MaxSdkBase.AdViewPosition AdapterBannerPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                    return MaxSdkBase.AdViewPosition.TopCenter;
                case BannerPosition.Bottom:
                    return MaxSdkBase.AdViewPosition.BottomCenter;
                case BannerPosition.TopLeft:
                    return MaxSdkBase.AdViewPosition.TopLeft;
                case BannerPosition.TopRight:
                    return MaxSdkBase.AdViewPosition.TopRight;
                case BannerPosition.BottomLeft:
                    return MaxSdkBase.AdViewPosition.BottomLeft;
                case BannerPosition.BottomRight:
                    return MaxSdkBase.AdViewPosition.BottomRight;
                case BannerPosition.Center:
                    return MaxSdkBase.AdViewPosition.Centered;
                case BannerPosition.CenterLeft:
                    return MaxSdkBase.AdViewPosition.CenterLeft;
                case BannerPosition.CenterRight:
                    return MaxSdkBase.AdViewPosition.CenterRight;
            }
            return MaxSdkBase.AdViewPosition.BottomCenter;
        }

        public override void SetBannerPosition(BannerPosition position)
        {
            if (//mBannerAdVisibility &&
                mDefaultBannerAdList.Count > 0)
            {
                //if (Screen.width > Screen.height && position == BannerPosition.Bottom)//landscape
                //{
                //    SetBannerPosition(new Vector2(Screen.width * 0.5f, Screen.height));
                //}
                //else
                {
                    MaxSdk.UpdateBannerPosition(mDefaultBannerAdList[0]._Id, AdapterBannerPosition(position));
                }
            }
            base.SetBannerPosition(position);
        }

        //public override void SetBannerPosition(Vector2 screenPos)
        //{
        //    bannerLastPosition = screenPos;
        //    var dpi = Screen.dpi;
        //    if (dpi <= 0) return;//could not detect DPI so could not convert to native resolution
        //    if (mDefaultBannerAdList.Count == 0) return;//no banner
        //    var adsId = mDefaultBannerAdList[0]._Id;
        //    var ratio = (dpi > DEFAULT_DPI) ? dpi / DEFAULT_DPI : DEFAULT_DPI / dpi;
        //    var safeArea = Screen.safeArea;

        //    var banner_width = Mathf.Floor(Mathf.Min(Screen.width, Screen.height) / ratio);
        //    var banner_height = Mathf.Floor(MaxSdkUtils.GetAdaptiveBannerHeight(banner_width));
        //    //Debug.Log("banner_width " + banner_width + "   banner_height " + banner_height);
        //    var rect_x_min = Mathf.Floor(safeArea.xMin / ratio);
        //    var rect_x_max = Mathf.Floor(safeArea.xMax / ratio);
        //    var rect_y_min = Mathf.Floor((Screen.height - safeArea.yMax) / ratio);
        //    var rect_y_max = Mathf.Floor((Screen.height - safeArea.yMin) / ratio);
        //    float maxOffsetHeight = 0;
        //    if (Screen.width > Screen.height && AFramework.UI.CanvasManager.UICanvas != null && screenPos.y >= Screen.height / 2)
        //    {
        //        var uiHeight = AFramework.UI.CanvasManager.UIRectTrans.rect.height;
        //        float uiScaleRatio = (uiHeight / Screen.height);
        //        banner_width /= uiScaleRatio;
        //        banner_height = Mathf.Floor(MaxSdkUtils.GetAdaptiveBannerHeight(banner_width));

        //        rect_x_min = Mathf.Floor(safeArea.xMin / uiScaleRatio / ratio);
        //        rect_x_max = Mathf.Floor((Screen.width - (Screen.width - safeArea.xMax) / uiScaleRatio) / ratio);
        //        rect_y_min = Mathf.Floor(((Screen.height - safeArea.yMax) / uiScaleRatio) / ratio);
        //        rect_y_max = Mathf.Floor((Screen.height - safeArea.yMin / uiScaleRatio) / ratio);

        //        maxOffsetHeight = (banner_width / banner_height) < 6.4f ? 18 : 0;
        //    }
        //    //Debug.Log("rect_x_max " + rect_x_max + "   rect_y_max " + rect_y_max + "  offHei " + maxOffsetHeight);

        //    var pos_x = Mathf.Floor(screenPos.x / ratio - banner_width / 2);
        //    if (pos_x + banner_width > rect_x_max) pos_x = rect_x_max - banner_width;
        //    else if (pos_x < rect_x_min) pos_x = rect_x_min;

        //    var pos_y = Mathf.Floor(screenPos.y / ratio - banner_height / 2);
        //    if (pos_y + banner_height > rect_y_max) pos_y = rect_y_max - banner_height;
        //    else if (pos_y < rect_y_min) pos_y = rect_y_min;
        //    //Debug.Log("pos_x " + pos_x + "   pos_y " + pos_y);
        //    MaxSdk.UpdateBannerPosition(adsId, pos_x, pos_y + maxOffsetHeight);
        //}

        public override void ShowAdsBanner()
        {
            base.ShowAdsBanner();
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            MaxSdk.ShowBanner(mDefaultBannerAdList[0]._Id);
            if (mDefaultBannerAdList.Count > 1)
            {
                MaxSdk.ShowBanner(mDefaultBannerAdList[1]._Id);
            }

            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(true);
            }
            if (AdsManager.Debugging) Debug.Log("ShowAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

        private void CheckBannerAdSize()
        {
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;

            // 1. Lấy Pixel thực tế (AppLovin trả về pixel trên Android)
            float heightFromSDK = MaxSdk.GetBannerLayout(mDefaultBannerAdList[0]._Id).height;

            // 2. Lấy Hệ số nhân
            float multiplier = Screen.dpi / 160f;

            // 3. Kết quả bạn muốn
            float finalValue = heightFromSDK * multiplier;

            Debug.Log($"[AppLovin] Size: {heightFromSDK}, {multiplier}, {finalValue}");
        }

        public string GetBannerId()
        {
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return string.Empty;

            return mDefaultBannerAdList[0]._Id;
        }

        public override void HideAdsBanner()
        {
            base.HideAdsBanner();
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(false);
            }
            if (mDefaultBannerAdList.Count <= 0) return;
            MaxSdk.HideBanner(mDefaultBannerAdList[0]._Id);
            if (mDefaultBannerAdList.Count > 1)
            {
                MaxSdk.HideBanner(mDefaultBannerAdList[1]._Id);
            }
            if (AdsManager.Debugging) Debug.Log("HideAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

        private void HandleOnBannerAdLoadedEvent(string adId, MaxSdk.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnBannerAdLoadedEvent");
            var currentDownload = GetCurrentDownload(AdsType.Banner);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.Banner, true, null, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(true);
                ForceFreeCurrentDownload(AdsType.Banner);
            }
            //Debug.Log("bPos " + MaxSdk.GetBannerLayout(adId));
            if (mBannerAdVisibility)
            {
                ShowAdsBanner();
            }
            else
            {
                HideAdsBanner();
            }
            if (!bannerPositionSet && bannerLastPosition != Vector2.zero)
            {
                bannerPositionSet = true;
                SetBannerPosition(bannerLastPosition);
            }
        }

        private void HandleOnBannerAdClickedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnBannerAdClickedEvent");
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Banner);
        }

        private void HandleOnBannerAdRevenuePaidEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnBannerAdRevenuePaidEvent");
            if (adInfo != null)
            {
                TrackAdRevenue(AdsType.Banner, adInfo.Revenue, adInfo.NetworkName, "USD", "AppLovin", adInfo.AdUnitIdentifier, adInfo.AdFormat, adInfo.Placement);
            }
        }

        private void HandleOnBannerAdLoadFailedEvent(string adId, MaxSdkBase.ErrorInfo error)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnBannerAdLoadFailedEvent " + adId + " code " + (error != null ? error.Code.ToString() : "-1"));
            var currentDownload = GetCurrentDownload(AdsType.Banner);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.Banner, false, error.Code.ToString(), error.ToString());
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                ForceFreeCurrentDownload(AdsType.Banner);
            }
        }
        #endregion

        #region InterstitialAd
        public override bool ShowAdsInterstitial(Action<bool> callback, string adId = null)
        {
            if (!IsInterstitialAdAvailable(adId)) return false;
            base.ShowAdsInterstitial(callback, adId);
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultInterstitialAdList.Count; ++i)
                {
                    if (mDefaultInterstitialAdList[i].IsAvailable())
                    {
                        mCurrentFullscreenAd = mDefaultInterstitialAdList[i];
                        break;
                    }
                }
            }
            else
            {
                mCurrentFullscreenAd = mAdDownloadHandler[adId];
            }

            if (mAdsInterstitialTimeoutThread != null)
            {
                StopCoroutine(mAdsInterstitialTimeoutThread);
                mAdsInterstitialTimeoutThread = null;
            }
            mAdsInterstitialTimeoutThread = CRAdsInterstitialTimeoutThread();
            StartCoroutine(mAdsInterstitialTimeoutThread);

            MaxSdk.ShowInterstitial(mCurrentFullscreenAd._Id);
            if (AdsManager.Debugging) Debug.Log("ShowAdsInterstitial " + mCurrentFullscreenAd._Id);
            return true;
        }

        private void HandleOnInterstitialAdHiddenEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialHiddenEvent");
            AudioListener.volume = 1;
            if (mInterstitialAdCallback != null)
            {
                mInterstitialAdCallback(true);
                mInterstitialAdCallback = null;
            }
            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }
            FullscreenAdShowing = false;
        }

        private void HandleOnInterstitialAdDisplayedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialDisplayedEvent");

            AudioListener.volume = 0;
            FullscreenAdShowing = true;

            if (mAdsInterstitialTimeoutThread != null)
            {
                StopCoroutine(mAdsInterstitialTimeoutThread);
                mAdsInterstitialTimeoutThread = null;
            }

            if (AdsManager.EventOnFullScreenAdsShown != null)
            {
                AdsManager.EventOnFullScreenAdsShown();
            }
            AdDisplayResultCallback(AdsType.Interstitial, true, null, null);
        }

        private void HandleOnInterstitialAdClickedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialClickedEvent");
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Interstitial);
        }

        private void HandleOnInterstitialAdRevenuePaidEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialRevenuePaidEvent");
            if (adInfo != null)
            {
                TrackAdRevenue(AdsType.Interstitial, adInfo.Revenue, adInfo.NetworkName, "USD", "AppLovin", adInfo.AdUnitIdentifier, adInfo.AdFormat, adInfo.Placement);
            }
        }

        private void HandleOnInterstitialAdFailedToDisplayEvent(string adId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo adInfo)
        {
            //Tin: Warning error + adInfo can be null
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialAdFailedToDisplayEvent " + adId + " code: " + (error != null ? error.Code.ToString() : "-1"));
            AudioListener.volume = 1;
            if (mInterstitialAdCallback != null)
            {
                mInterstitialAdCallback(false);
                mInterstitialAdCallback = null;
            }
            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }
            FullscreenAdShowing = false;
            if (error != null)
            {
                AdDisplayResultCallback(AdsType.Interstitial, false, error.Code.ToString(), error.ToString());
            }
            else
            {
                AdDisplayResultCallback(AdsType.Interstitial, false, "-1", adInfo != null ? adInfo.NetworkName : null);
            }
        }

        private void HandleOnInterstitialAdLoadFailedEvent(string adId, MaxSdkBase.ErrorInfo error)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialLoadFailedEvent " + adId + " code: " + (error != null ? error.Code.ToString() : "-1"));
            var currentDownload = GetCurrentDownload(AdsType.Interstitial);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.Interstitial, false, error.Code.ToString(), error.ToString());
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                ForceFreeCurrentDownload(AdsType.Interstitial);
            }
        }

        private void HandleOnInterstitialAdLoadedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialLoadedEvent");
            var currentDownload = GetCurrentDownload(AdsType.Interstitial);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.Interstitial, true, null, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(true);
                ForceFreeCurrentDownload(AdsType.Interstitial);
            }
        }

        IEnumerator mAdsInterstitialTimeoutThread;
        IEnumerator CRAdsInterstitialTimeoutThread()
        {
            int currentFrame = Time.frameCount;
            WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(0.5f);
            float totalWaitTime = 7;
            int totalWaitFrame = currentFrame + 90;
            while (totalWaitTime > 0)
            {
                totalWaitTime -= 0.5f;
                yield return waitTime;
            }
            yield return new WaitUntil(() => Time.frameCount > totalWaitFrame);
            HandleOnInterstitialAdFailedToDisplayEvent(mCurrentFullscreenAd != null ? mCurrentFullscreenAd._Id : "", null, null);
        }
        #endregion

        #region RewardAd
        bool mHaveRewarded = false;

        public override bool ShowAdsReward(Action<bool> callback, string adId = null)
        {
            if (!IsRewardAdAvailable(adId)) return false;
            base.ShowAdsReward(callback, adId);
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultRewardAdList.Count; ++i)
                {
                    if (mDefaultRewardAdList[i].IsAvailable())
                    {
                        mCurrentFullscreenAd = mDefaultRewardAdList[i];
                        break;
                    }
                }
            }
            else
            {
                mCurrentFullscreenAd = mAdDownloadHandler[adId];
            }
            mHaveRewarded = false;

            if (mAdsRewardTimeoutThread != null)
            {
                StopCoroutine(mAdsRewardTimeoutThread);
                mAdsRewardTimeoutThread = null;
            }
            mAdsRewardTimeoutThread = CRAdsRewardTimeoutThread(mCurrentFullscreenAd._Id);
            StartCoroutine(mAdsRewardTimeoutThread);

            MaxSdk.ShowRewardedAd(mCurrentFullscreenAd._Id);
            return true;
        }

        private void HandleOnRewardedAdReceivedRewardEvent(string adId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdReceivedRewardEvent " + adId);
            mHaveRewarded = true;
        }

        private void HandleOnRewardedAdHiddenEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdHiddenEvent " + adId);
            AudioListener.volume = 1;
            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }

            if (mHaveRewarded)
            {
                FullscreenAdShowing = false;
                if (mRewardAdCallback != null)
                {
                    mRewardAdCallback(mHaveRewarded);
                    mRewardAdCallback = null;
                }
                mHaveRewarded = false;
            }
            else
            {
                if (mDelayRewardCheckThread != null)
                {
                    StopCoroutine(mDelayRewardCheckThread);
                    mDelayRewardCheckThread = null;
                }

                mDelayRewardCheckThread = CRWaitForReward();
                StartCoroutine(mDelayRewardCheckThread);
            }
        }

        private void HandleOnRewardedAdClickedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdClickedEvent " + adId);
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.RewardedVideo);
        }

        private void HandleOnRewardedAdRevenuePaidEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdRevenuePaidEvent " + adId);
            if (adInfo != null)
            {
                TrackAdRevenue(AdsType.RewardedVideo, adInfo.Revenue, adInfo.NetworkName, "USD", "AppLovin", adInfo.AdUnitIdentifier, adInfo.AdFormat, adInfo.Placement);
            }
        }

        private void HandleOnRewardedAdDisplayedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdDisplayedEvent " + adId);

            AudioListener.volume = 0;
            FullscreenAdShowing = true;
            mHaveRewarded = false;

            if (mAdsRewardTimeoutThread != null)
            {
                StopCoroutine(mAdsRewardTimeoutThread);
                mAdsRewardTimeoutThread = null;
            }

            if (AdsManager.EventOnFullScreenAdsShown != null)
            {
                AdsManager.EventOnFullScreenAdsShown();
            }
            AdDisplayResultCallback(AdsType.RewardedVideo, true, null, null);
        }

        private void HandleOnRewardedAdFailedToDisplayEvent(string adId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo adInfo)
        {
            //Tin: Warning error + adInfo can be null
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdFailedToDisplayEvent " + adId + " code " + (error != null ? error.Code.ToString() : "-1"));
            AudioListener.volume = 1;
            if (mRewardAdCallback != null)
            {
                mRewardAdCallback(false);
                mRewardAdCallback = null;
            }
            if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
            mCurrentFullscreenAd = null;
            FullscreenAdShowing = false;
            if (error != null)
            {
                AdDisplayResultCallback(AdsType.RewardedVideo, false, error.Code.ToString(), error.ToString());
            }
            else
            {
                AdDisplayResultCallback(AdsType.RewardedVideo, false, "-1", adInfo != null ? adInfo.NetworkName : null);
            }
        }

        private void HandleOnRewardedAdLoadFailedEvent(string adId, MaxSdkBase.ErrorInfo error)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdLoadFailedEvent " + adId + " code " + (error != null ? error.Code.ToString() : "-1"));
            var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.RewardedVideo, false, error.Code.ToString(), error.ToString());
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                ForceFreeCurrentDownload(AdsType.RewardedVideo);
            }
        }

        private void HandleOnRewardedAdLoadedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdLoadedEvent " + adId);
            var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.RewardedVideo, true, null, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(true);
                ForceFreeCurrentDownload(AdsType.RewardedVideo);
            }
        }

        IEnumerator mDelayRewardCheckThread;
        IEnumerator CRWaitForReward()
        {
            float waitTime = 1.0f;
            var waitHandler = new WaitForSecondsRealtime(0.03f);
            while (waitTime > 0)
            {
                yield return waitHandler;
                waitTime -= 0.03f;

                if (mHaveRewarded)
                {
                    FullscreenAdShowing = false;
                    if (mRewardAdCallback != null)
                    {
                        mRewardAdCallback(true);
                        mRewardAdCallback = null;
                    }
                    mHaveRewarded = false;
                    mDelayRewardCheckThread = null;
                    yield break;
                }
            }
            FullscreenAdShowing = false;
            if (mRewardAdCallback != null)
            {
                mRewardAdCallback(mHaveRewarded);
                mRewardAdCallback = null;
            }
            mHaveRewarded = false;
            mDelayRewardCheckThread = null;
        }

        IEnumerator mAdsRewardTimeoutThread;
        IEnumerator CRAdsRewardTimeoutThread(string adUnitId)
        {
            var cacheId = adUnitId;
            int currentFrame = Time.frameCount;
            WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(0.5f);
            float totalWaitTime = 7;
            int totalWaitFrame = currentFrame + 90;
            while (totalWaitTime > 0)
            {
                totalWaitTime -= 0.5f;
                yield return waitTime;
            }
            yield return new WaitUntil(() => Time.frameCount > totalWaitFrame);
            HandleOnRewardedAdFailedToDisplayEvent(mCurrentFullscreenAd != null ? mCurrentFullscreenAd._Id : "", null, null);
        }
        #endregion

        #region AppOpenAd
        public override bool ShowAdAppOpen(Action<bool> callback, string adId = null)
        {
            if (!IsAppOpenAdAvailable(adId)) return false;
            base.ShowAdAppOpen(callback, adId);
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultAppOpenAdList.Count; ++i)
                {
                    if (mDefaultAppOpenAdList[i].IsAvailable())
                    {
                        mCurrentFullscreenAd = mDefaultAppOpenAdList[i];
                        break;
                    }
                }
            }
            else
            {
                mCurrentFullscreenAd = mAdDownloadHandler[adId];
            }

            MaxSdk.ShowAppOpenAd(mCurrentFullscreenAd._Id);
            if (AdsManager.Debugging) Debug.Log("ShowAdAppOpen " + mCurrentFullscreenAd._Id);
            return true;
        }

        private void HandleOnAppOpenAdLoadedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdLoadedEvent");
            var currentDownload = GetCurrentDownload(AdsType.AppOpen);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.AppOpen, true, null, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(true);
                ForceFreeCurrentDownload(AdsType.AppOpen);
            }
        }

        private void HandleOnAppOpenAdLoadFailedEvent(string adId, MaxSdkBase.ErrorInfo error)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdLoadFailedEvent " + adId + " code: " + (error != null ? error.Code.ToString() : "-1"));
            var currentDownload = GetCurrentDownload(AdsType.AppOpen);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                AdDownloadCallback(AdsType.AppOpen, false, error.Code.ToString(), error.ToString());
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                ForceFreeCurrentDownload(AdsType.AppOpen);
            }
        }

        private void HandleOnAppOpenAdFailedToDisplayEvent(string adId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo adInfo)
        {
            //Tin: Warning error + adInfo can be null
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdFailedToDisplayEvent " + adId + " code: " + (error != null ? error.Code.ToString() : "-1"));
            if (mAppOpenCallback != null)
            {
                mAppOpenCallback(false);
                mAppOpenCallback = null;
            }
            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }
            FullscreenAdShowing = false;
            if (error != null)
            {
                AdDisplayResultCallback(AdsType.AppOpen, false, error.Code.ToString(), error.ToString());
            }
            else
            {
                AdDisplayResultCallback(AdsType.AppOpen, false, "-1", adInfo != null ? adInfo.NetworkName : null);
            }
        }

        private void HandleOnAppOpenAdHiddenEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdHiddenEvent");
            if (mAppOpenCallback != null)
            {
                mAppOpenCallback(true);
                mAppOpenCallback = null;
            }
            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }
            FullscreenAdShowing = false;
        }

        private void HandleOnAppOpenAdDisplayedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdDisplayedEvent");

            FullscreenAdShowing = true;

            if (AdsManager.EventOnFullScreenAdsShown != null)
            {
                AdsManager.EventOnFullScreenAdsShown();
            }
            AdDisplayResultCallback(AdsType.AppOpen, true, null, null);
        }

        private void HandleOnAppOpenAdClickedEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdClickedEvent");
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.AppOpen);
        }

        private void HandleOnAppOpenAdRevenuePaidEvent(string adId, MaxSdkBase.AdInfo adInfo)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnAppOpenAdRevenuePaidEvent");
            if (adInfo != null)
            {
                TrackAdRevenue(AdsType.AppOpen, adInfo.Revenue, adInfo.NetworkName, "USD", "AppLovin", adInfo.AdUnitIdentifier, adInfo.AdFormat, adInfo.Placement);
            }
        }
        #endregion

        protected override void SetCustomUserId(string userId)
        {
            MaxSdk.SetUserId(userId);
        }
    }

    public class MaxAdStatusHandler : AdStatusHandler
    {
        public MaxAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return _Available;
        }

        protected override bool InterstitialAdAvailable()
        {
            return MaxSdk.IsInterstitialReady(_Id);
        }

        protected override bool RewardAdAvailable()
        {
            return MaxSdk.IsRewardedAdReady(_Id);
        }

        protected override bool AppOpenAdAvailable()
        {
            return MaxSdk.IsAppOpenAdReady(_Id);
        }
    }
}
#endif