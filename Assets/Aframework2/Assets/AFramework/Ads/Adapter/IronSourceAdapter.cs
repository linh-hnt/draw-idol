#if USE_IRONSOURCE_ADS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace AFramework.Ads
{
    public class IronSourceAdapter : BaseAdsAdapter
    {
        protected BaseAdapterConfig Config { get { return mConfig; } }
        bool mHaveRewarded = false;
        object[] backupInitParams;

#if AMAZON_APS
        AmazonAds.APSBannerAdRequest amazonBannerAdRequest;
        AmazonAds.APSVideoAdRequest amazonInterstitialAdRequest;
        AmazonAds.APSVideoAdRequest amazonRewardedAdRequest;
#endif

        public override void Init(object[] parameters)
        {
            backupInitParams = parameters;

#if ADMOB_CONSENT
            if (
#if ADMOB_ATT
                true ||
#endif
                AdsManager.I.IsATTAsked
            )
            {
                var request = new GoogleMobileAds.Ump.Api.ConsentRequestParameters();
                GoogleMobileAds.Ump.Api.ConsentInformation.Update(request, OnConsentInfoUpdated);
            }
            else
#endif
            {
                IronsourceInit(backupInitParams);
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
                    UnityMainThreadDispatcher.instance.Enqueue(() => { IronsourceInit(backupInitParams); });
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
        void IronsourceInit(object[] parameters)
        {
#if DEVELOPMENT_BUILD
            LevelPlay.setAdaptersDebug(true);
            LevelPlay.SetMetaData("is_test_suite", "enable");
#endif

            mConfig = ((BaseAdapterConfig)parameters[0]);

#if AMAZON_APS && !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (Config.Platform.AmazonConfig != null && !string.IsNullOrEmpty(Config.Platform.AmazonConfig.appId))
            {
                AmazonAds.Amazon.Initialize(Config.Platform.AmazonConfig.appId);
                AmazonAds.Amazon.SetAdNetworkInfo(new AmazonAds.AdNetworkInfo(AmazonAds.DTBAdNetwork.IRON_SOURCE));
                AmazonAds.Amazon.SetMRAIDPolicy(AmazonAds.Amazon.MRAIDPolicy.CUSTOM);
                AmazonAds.Amazon.SetMRAIDSupportedVersions(new string[] { "1.0", "2.0", "3.0" });
                if (AdsManager.Debugging)
                {
                    AmazonAds.Amazon.EnableTesting(true);
                    AmazonAds.Amazon.EnableLogging(true);
                }
            }
#endif

#if UNITY_IOS && FACEBOOK_AUDIENCENETWORK
            {
                var attStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                if (attStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(attStatus == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);
                }
            }
#endif
            LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
            LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;

            LevelPlay.OnInitFailed -= OnLevelPlayInitFailed;
            LevelPlay.OnInitFailed += OnLevelPlayInitFailed;

            LevelPlay.Init(Config.Platform.IronsourceId);

#if false//UNITY_IOS
            {
                var attStatus = Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                if (attStatus != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    IronSource.Agent.setConsent(attStatus == Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);
                }
            }
#endif


#if DEVELOPMENT_BUILD
            IronSource.Agent.validateIntegration();
#endif
        }

        void OnLevelPlayInitSuccess(LevelPlayConfiguration levelplayConfig)
        {
            LevelPlay.OnImpressionDataReady += (x) => {
                if (AdsManager.Debugging) Debug.Log("onImpressionDataReadyEvent");
                var backupResult = x;
                UnityMainThreadDispatcher.instance.Enqueue(() => { ImpressionSuccessEvent(backupResult); });
            };
            base.Init(backupInitParams);

#if USE_IRONSOURCE_ADQUALITY
            IronSourceAdQuality.Initialize(Config.Platform.IronsourceId);
#endif
        }

        void OnLevelPlayInitFailed(LevelPlayInitError error)
        {
            ADebug.LogError(error.ToString());

            AdsManager.I.StartCoroutine(Utility.CRDelayFunction(30, () => {
                LevelPlay.Init(Config.Platform.IronsourceId);
            }));
        }

        //        void OnApplicationPause(bool isPaused)
        //        {
        //            IronSource.Agent.onApplicationPause(isPaused);

        //#if UNITY_ANDROID
        //            if (!isPaused && mFullScreenAdShowing)
        //            {
        //                StartCoroutine(CRInteruptWaitCheck());
        //            }
        //#endif
        //        }

        //IEnumerator CRInteruptWaitCheck()
        //{
        //    yield return new WaitForSecondsRealtime(1);

        //    if (FullscreenAdShowing && AdsManager.IsInstanceValid())//fix some adapter does not return Ads Close event on interrupt
        //    {
        //        if (mCurrentFullscreenAd != null)
        //        {
        //            mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
        //            mCurrentFullscreenAd = null;
        //        }
        //        if (mInterstitialAdCallback != null)
        //        {
        //            mInterstitialAdCallback(false);
        //            mInterstitialAdCallback = null;
        //        }
        //        if (mRewardAdCallback != null)
        //        {
        //            mRewardAdCallback(false);
        //            mRewardAdCallback = null;
        //        }
        //        FullscreenAdShowing = false;
        //        AudioListener.volume = 1;
        //    }
        //}

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            var adsHandler = new IronsourceAdStatusHandler(type, id);
            if (type == AdsType.Banner)
            {
                var configBuilder = new LevelPlayBannerAd.Config.Builder();
                configBuilder.SetDisplayOnLoad(false);
                configBuilder.SetPosition(AdapterBannerPosition(mBannerPosition));

                adsHandler.BannerAd = new LevelPlayBannerAd(id, configBuilder.Build());

                adsHandler.BannerAd.OnAdLoaded += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onBannerAdLoadedEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonBannerAdLoadedEvent(backupResult); });
                };
                adsHandler.BannerAd.OnAdLoadFailed += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onBannerAdLoadFailedEvent " + result.ToString());
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonBannerAdLoadFailedEvent(backupResult); });
                };
                adsHandler.BannerAd.OnAdClicked += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onBannerAdClickedEvent ");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonBannerAdClickedEvent(backupResult); });
                };
            }
            else if (type == AdsType.Interstitial)
            {
                adsHandler.InterstitialAd = new LevelPlayInterstitialAd(id);

                adsHandler.InterstitialAd.OnAdLoaded += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onInterstitialAdReadyEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonInterstitialAdReadyEvent(backupResult); });
                };
                adsHandler.InterstitialAd.OnAdLoadFailed += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onInterstitialAdLoadFailedEvent " + result.ToString());
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonInterstitialAdLoadFailedEvent(backupResult); });
                };
                adsHandler.InterstitialAd.OnAdClicked += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onInterstitialAdClickedEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonInterstitialAdClickedEvent(backupResult); });
                };
                adsHandler.InterstitialAd.OnAdDisplayed += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onInterstitialAdShowSucceededEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonInterstitialAdShowSucceededEvent(backupResult); });
                };
                adsHandler.InterstitialAd.OnAdDisplayFailed += (info, error) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onInterstitialAdShowFailedEvent " + error.ToString());
                    var backupResult = error;
                    var backupInfo = info;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonInterstitialAdShowFailedEvent(backupResult, backupInfo); });
                };
                adsHandler.InterstitialAd.OnAdClosed += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onInterstitialAdClosedEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonInterstitialAdClosedEvent(backupResult); });
                };

            }
            else if (type == AdsType.RewardedVideo)
            {
                adsHandler.RewardAd = new LevelPlayRewardedAd(id);
                
                adsHandler.RewardAd.OnAdLoaded += (info) =>
                {
                    if (AdsManager.Debugging) Debug.Log("HandleonRewardAdReadyEvent");
                    var backupInfo = info;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardAdReadyEvent(backupInfo); });
                };
                adsHandler.RewardAd.OnAdLoadFailed += (error) =>
                {
                    if (AdsManager.Debugging) Debug.Log("HandleonRewardAdLoadFailedEvent " + error.ToString());
                    var backupResult = error;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardAdLoadFailedEvent(backupResult); });
                };
                adsHandler.RewardAd.OnAdClicked += (info) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onRewardedVideoAdClickedEvent");
                    var backupInfo = info;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardedVideoAdClickedEvent(backupInfo); });
                };
                adsHandler.RewardAd.OnAdDisplayed += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onRewardedVideoAdOpenedEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardedVideoAdOpenedEvent(backupResult); });
                };
                adsHandler.RewardAd.OnAdDisplayFailed += (info, error) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onRewardedVideoAdShowFailedEvent " + error.ToString());
                    var backupResult = error;
                    var backupInfo = info;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardedVideoAdShowFailedEvent(backupResult, backupInfo); });
                };
                adsHandler.RewardAd.OnAdClosed += (result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onRewardedVideoAdClosedEvent");
                    var backupResult = result;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardedVideoAdClosedEvent(backupResult); });
                };
                adsHandler.RewardAd.OnAdRewarded += (info, result) =>
                {
                    if (AdsManager.Debugging) Debug.Log("onRewardedVideoAdRewardedEvent");
                    var backupInfo = info;
                    UnityMainThreadDispatcher.instance.Enqueue(() => { HandleonRewardedVideoAdRewardedEvent(backupInfo); });
                };
            }
            else
            {
                Debug.LogError("TODO");
            }
            return adsHandler;
        }

        //protected override void UpdateDownloadList()
        //{
        //    AdStatusHandler handler = null;
        //    if (AdsManager.I.IsAdEnabled(AdsType.RewardedVideo))
        //    {
        //        handler = CreateAdHandler(AdsType.RewardedVideo, "");
        //        mAdDownloadHandler.Add("rewardad", handler);
        //        mAdHighPriorityList.Add(handler);
        //        mDefaultRewardAdList.Add(handler);
        //    }
        //    //if (AdsManager.I.IsAdEnabled(AdsType.OfferWall))
        //    //{
        //    //    handler = CreateAdHandler(AdsType.OfferWall, "");
        //    //    mAdDownloadHandler.Add("offerwall", handler);
        //    //    mAdHighPriorityList.Add(handler);
        //    //    mDefaultOfferWallAdList.Add(handler);
        //    //}
        //    if (AdsManager.I.IsAdEnabled(AdsType.Interstitial))
        //    {
        //        handler = CreateAdHandler(AdsType.Interstitial, "");
        //        mAdDownloadHandler.Add("interstitialad" + 0, handler);
        //        mAdHighPriorityList.Add(handler);
        //        mDefaultInterstitialAdList.Add(handler);
        //    }
        //    if (AdsManager.I.IsAdEnabled(AdsType.Banner))
        //    {
        //        handler = CreateAdHandler(AdsType.Banner, "");
        //        mAdDownloadHandler.Add("bannerad" + 0, handler);
        //        mAdHighPriorityList.Add(handler);
        //        mDefaultBannerAdList.Add(handler);
        //    }

        //    //Tin: not support sdk 6.16.1, currently Ironsource Option/Demanded Ad is messup too much, callback is not consistency 
        //    //base.UpdateDownloadList();
        //}

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            base.DownloadAd(ad);
            var ironsourceAds = ad as IronsourceAdStatusHandler;
            switch (ad._Type)
            {
                case AdsType.Banner:
#if AMAZON_APS && !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                    if (amazonBannerAdRequest == null && Config.Platform.AmazonConfig != null && !string.IsNullOrEmpty(Config.Platform.AmazonConfig.banner))
                    {
                        int bannerWidth = 320;// Mathf.Min(Screen.width, Screen.height);
                        int bannerHeight = 50;// Mathf.FloorToInt(bannerWidth * 50f / 320);
                        string maxAdId = ad._Id;

                        amazonBannerAdRequest = new AmazonAds.APSBannerAdRequest(bannerWidth, bannerHeight, Config.Platform.AmazonConfig.banner);
                        amazonBannerAdRequest.onFailedWithError += (adError) =>
                        {
                            if (AdsManager.Debugging) Debug.Log("AmazonAds.APSBannerAdRequest onFailedWithError " + adError.GetCode());
                            ironsourceAds.BannerAd.LoadAd();
                        };
                        amazonBannerAdRequest.onSuccess += (adResponse) =>
                        {
                            LevelPlay.SetNetworkData(AmazonAds.APSMediationUtils.APS_IRON_SOURCE_NETWORK_KEY,
                                            AmazonAds.APSMediationUtils.GetBannerNetworkData(Config.Platform.AmazonConfig.banner, adResponse));
                            ironsourceAds.BannerAd.LoadAd();
                        };

                        amazonBannerAdRequest.LoadAd();
                    }
                    else
#endif
                    {
                        ironsourceAds.BannerAd.LoadAd();
                    }
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

                        amazonInterstitialAdRequest.onSuccess += (adResponse) =>
                        {
                            IronSource.Agent.setNetworkData(AmazonAds.APSMediationUtils.APS_IRON_SOURCE_NETWORK_KEY,
                                            AmazonAds.APSMediationUtils.GetInterstitialNetworkData(Config.Platform.AmazonConfig.interstitial, adResponse));
                            IronSource.Agent.loadInterstitial();
                        };
                        amazonInterstitialAdRequest.onFailedWithError += (adError) =>
                        {
                            if (AdsManager.Debugging) Debug.Log("AmazonAds InterVideo onFailedWithError " + adError.GetCode());
                            IronSource.Agent.loadInterstitial();
                        };
                        amazonInterstitialAdRequest.LoadAd();
                    }
                    else
#endif
                    {
                        ironsourceAds.InterstitialAd.LoadAd();
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
                            LevelPlay.SetNetworkData(AmazonAds.APSMediationUtils.APS_IRON_SOURCE_NETWORK_KEY,
                                            AmazonAds.APSMediationUtils.GetRewardedNetworkData(Config.Platform.AmazonConfig.reward, adResponse));
                            ironsourceAds.RewardAd.LoadAd();
                        };
                        amazonRewardedAdRequest.onFailedWithError += (adError) =>
                        {
                            if (AdsManager.Debugging) Debug.Log("AmazonAds RewardVideo onFailedWithError " + adError.GetCode());
                            ironsourceAds.RewardAd.LoadAd();
                        };
                        amazonRewardedAdRequest.LoadAd();
                    }
                    else
#endif
                    {
                        ironsourceAds.RewardAd.LoadAd();
                    }
                    break;
            }
        }

        #region Banner
        LevelPlayBannerPosition AdapterBannerPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                case BannerPosition.TopLeft:
                case BannerPosition.TopRight:
                    return LevelPlayBannerPosition.TopCenter;
            }
            return LevelPlayBannerPosition.BottomCenter;
        }

        public override void SetBannerPosition(BannerPosition position)
        {
            if (mBannerPosition == position) return;
            base.SetBannerPosition(position);
            //TODO
        }

        public override void ShowAdsBanner()
        {
            base.ShowAdsBanner();

            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            (mDefaultBannerAdList[0] as IronsourceAdStatusHandler).BannerAd.ShowAd();
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(true);
            }
        }

        public override void HideAdsBanner()
        {
            base.HideAdsBanner();
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(false);
            }

            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            (mDefaultBannerAdList[0] as IronsourceAdStatusHandler).BannerAd.HideAd();

        }

        void HandleonBannerAdLoadedEvent(LevelPlayAdInfo info)
        {
            AdDownloadCallback(AdsType.Banner, true, null, null);
            if (mBannerAdVisibility)
            {
                ShowAdsBanner();
            }
            else
            {
                HideAdsBanner();
            }
        }

        void HandleonBannerAdLoadFailedEvent(LevelPlayAdError error)
        {
            AdDownloadCallback(AdsType.Banner, false, error.ErrorCode.ToString(), error.ErrorMessage);
        }

        void HandleonBannerAdClickedEvent(LevelPlayAdInfo info)
        {
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Banner);
        }

        void HandleonBannerAdScreenPresentedEvent(LevelPlayAdInfo info) { }
        void HandleonBannerAdScreenDismissedEvent(LevelPlayAdInfo info) { }
        void HandleonBannerAdLeftApplicationEvent(LevelPlayAdInfo info) { }
        #endregion

        #region Interstitial
        public override bool ShowAdsInterstitial(Action<bool> callback, string adId = null)
        {
            if (!IsInterstitialAdAvailable(adId)) return false;
            base.ShowAdsInterstitial(callback, adId);
            mCurrentFullscreenAd = mDefaultInterstitialAdList[0];

            if (mAdsInterstitialTimeoutThread != null)
            {
                StopCoroutine(mAdsInterstitialTimeoutThread);
                mAdsInterstitialTimeoutThread = null;
            }
            mAdsInterstitialTimeoutThread = CRAdsInterstitialTimeoutThread();
            StartCoroutine(mAdsInterstitialTimeoutThread);

            (mCurrentFullscreenAd as IronsourceAdStatusHandler).InterstitialAd.ShowAd();
            return true;
        }

        void HandleonInterstitialAdReadyEvent(LevelPlayAdInfo info)
        {
            AdDownloadCallback(AdsType.Interstitial, true, null, null);
        }

        void HandleonInterstitialAdLoadFailedEvent(LevelPlayAdError error)
        {
            AdDownloadCallback(AdsType.Interstitial, false, error.ErrorCode.ToString(), error.ErrorMessage);
        }

        void HandleonInterstitialAdOpenedEvent(LevelPlayAdInfo info)
        {
            AdDisplayResultCallback(AdsType.Interstitial, true, null, null);
        }

        void HandleonInterstitialAdClosedEvent(LevelPlayAdInfo info)
        {
            AudioListener.volume = 1;
            FullscreenAdShowing = false;
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
        }

        void HandleonInterstitialAdShowSucceededEvent(LevelPlayAdInfo info)
        {
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
        }

        void HandleonInterstitialAdShowFailedEvent(LevelPlayAdError error, LevelPlayAdInfo info)
        {
            if (mInterstitialAdCallback != null)
            {
                mInterstitialAdCallback(false);
                mInterstitialAdCallback = null;
            }
            if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
            mCurrentFullscreenAd = null;
            if (error != null)
            {
                AdDisplayResultCallback(AdsType.Interstitial, false, error.ErrorCode.ToString(), error.ErrorMessage);
            }
            else
            {
                AdDisplayResultCallback(AdsType.Interstitial, false, "-1", null);
            }
        }

        void HandleonInterstitialAdClickedEvent(LevelPlayAdInfo info)
        {
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Interstitial);
        }

        //void HandleonInterstitialAdReadyDemandOnlyEvent(string id) { HandleonInterstitialAdReadyEvent(); }
        //void HandleonInterstitialAdLoadFailedDemandOnlyEvent(string id, LevelPlayAdError error) { HandleonInterstitialAdLoadFailedEvent(error); }
        //void HandleonInterstitialAdOpenedDemandOnlyEvent(string id) { HandleonInterstitialAdOpenedEvent(); }
        //void HandleonInterstitialAdClosedDemandOnlyEvent(string id) { HandleonInterstitialAdClosedEvent(); }
        //void HandleonInterstitialAdShowFailedDemandOnlyEvent(string id, LevelPlayAdError error) { HandleonInterstitialAdShowFailedEvent(error); }
        //void HandleonInterstitialAdClickedDemandOnlyEvent(string id) { HandleonInterstitialAdClickedEvent(); }

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
            HandleonInterstitialAdShowFailedEvent(null, null);
        }
        #endregion

        #region Reward
        public override bool ShowAdsReward(System.Action<bool> callback, string adId = null)
        {
            if (!IsRewardAdAvailable(adId)) return false;
            base.ShowAdsReward(callback);
            mCurrentFullscreenAd = mDefaultRewardAdList[0];

            if (mAdsRewardTimeoutThread != null)
            {
                StopCoroutine(mAdsRewardTimeoutThread);
                mAdsRewardTimeoutThread = null;
            }
            mAdsRewardTimeoutThread = CRAdsRewardTimeoutThread();
            StartCoroutine(mAdsRewardTimeoutThread);

            (mCurrentFullscreenAd as IronsourceAdStatusHandler).RewardAd.ShowAd();
            return true;
        }

        //IEnumerator CRAdsRewardInsurance()
        //{
        //    float insuranceTime = 35f;
        //    while (insuranceTime > 0 && FullscreenAdShowing)
        //    {
        //        insuranceTime -= Time.fixedUnscaledDeltaTime;
        //        if (insuranceTime <= 0)
        //        {
        //            mHaveRewarded = true;
        //        }
        //        yield return null;
        //    }
        //}

        void HandleonRewardedVideoAdShowFailedEvent(LevelPlayAdError error, LevelPlayAdInfo info)
        {
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
                AdDisplayResultCallback(AdsType.RewardedVideo, false, error.ErrorCode.ToString(), error.ErrorMessage);
            }
            else
            {
                AdDisplayResultCallback(AdsType.RewardedVideo, false, "-1", null);
            }
        }

        void HandleonRewardedVideoAdOpenedEvent(LevelPlayAdInfo info)
        {
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
            //StartCoroutine(CRAdsRewardInsurance());
            AdDisplayResultCallback(AdsType.RewardedVideo, true, null, null);
        }

        IEnumerator mDelayRewardCheckThread;
        void HandleonRewardedVideoAdClosedEvent(LevelPlayAdInfo info)
        {
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

        void HandleonRewardedVideoAdRewardedEvent(LevelPlayAdInfo info)
        {
            mHaveRewarded = true;
        }
        void HandleonRewardedVideoAdClickedEvent(LevelPlayAdInfo info)
        {
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.RewardedVideo);
        }

        void HandleonRewardAdReadyEvent(LevelPlayAdInfo info)
        {
            AdDownloadCallback(AdsType.RewardedVideo, true, null, null);
            if ((object)InternalEventOnRewardAdsChanged != null)
            {
                InternalEventOnRewardAdsChanged();
            }
        }

        void HandleonRewardAdLoadFailedEvent(LevelPlayAdError error)
        {
            AdDownloadCallback(AdsType.RewardedVideo, false, error.ErrorCode.ToString(), error.ErrorMessage);
            if ((object)InternalEventOnRewardAdsChanged != null)
            {
                InternalEventOnRewardAdsChanged();
            }
        }

        //void HandleonRewardedVideoAdLoadedDemandOnlyEvent(string id) { }
        //void HandleonRewardedVideoAdLoadFailedDemandOnlyEvent(string id, LevelPlayAdError error) { /*TODO*/ }

        //void HandleonRewardedVideoAdOpenedDemandOnlyEvent(string id) { HandleonRewardedVideoAdOpenedEvent(); }
        //void HandleonRewardedVideoAdClosedDemandOnlyEvent(string id) { HandleonRewardedVideoAdClosedEvent(); }
        //void HandleonRewardedVideoAdRewardedDemandOnlyEvent(string id) { HandleonRewardedVideoAdRewardedEvent(); }
        //void HandleonRewardedVideoAdShowFailedDemandOnlyEvent(string id, LevelPlayAdError error) { HandleonRewardedVideoAdShowFailedEvent(error); }
        //void HandleonRewardedVideoAdClickedDemandOnlyEvent(string id) { HandleonRewardedVideoAdClickedEvent(); }

        IEnumerator mAdsRewardTimeoutThread;
        IEnumerator CRAdsRewardTimeoutThread()
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
            HandleonRewardedVideoAdShowFailedEvent(null, null);
        }
        #endregion
        //#region OfferWall

        //public override bool ShowOfferWall(string placementName)
        //{
        //    if (string.IsNullOrEmpty(placementName))
        //    {
        //        IronSource.Agent.showOfferwall();
        //    }

        //    IronSource.Agent.showOfferwall(placementName);
        //    return true;
        //}

        //public override void CheckOfferwallReward()
        //{
        //    IronSource.Agent.getOfferwallCredits();
        //}

        //void HandleonOfferwallClosedEvent()
        //{
        //    AudioListener.volume = 1;
        //}

        //void HandleonOfferwallOpenedEvent()
        //{
        //    AudioListener.volume = 0;
        //}

        //void HandleonOfferwallShowFailedEvent(LevelPlayAdError error)
        //{
        //    AudioListener.volume = 1;
        //}

        //void HandleonOfferwallAdCreditedEvent(Dictionary<string, object> dict)
        //{
        //    if (AdsManager.Debugging)
        //    {
        //        foreach (KeyValuePair<string, object> entry in dict)
        //        {
        //            Debug.Log(string.Format("OfferwallAdCreditedEvent: {0}: {1}", entry.Value, entry.Key));
        //        }
        //    }

        //    if (mOfferWallCallback != null)
        //    {
        //        mOfferWallCallback(dict);
        //    }
        //}

        //void HandleonGetOfferwallCreditsFailedEvent(LevelPlayAdError error)
        //{
        //    if (mOfferWallCallback != null)
        //    {
        //        mOfferWallCallback(null);
        //    }
        //}

        //IEnumerator mOfferWallDelayEventChanged = null;
        //void HandleonOfferwallAvailableEvent(bool isAvailable)
        //{
        //    var currentDownload = GetCurrentDownload(AdsType.OfferWall);
        //    if (isAvailable && currentDownload != null && currentDownload == mDefaultOfferWallAdList[0])
        //    {
        //        AdDownloadCallback(AdsType.OfferWall, true, null, null);
        //    }
        //    else
        //    {
        //        mDefaultOfferWallAdList[0].OnAdAvailabilityUpdate(isAvailable);
        //        if (isAvailable)
        //        {
        //            ForceFreeCurrentDownload(AdsType.OfferWall);
        //        }
        //    }

        //    if (BaseAdsAdapter.AdsAvailableSafeTime <= 0)
        //    {
        //        if ((object)InternalEventOnOfferWallChanged != null)
        //        {
        //            InternalEventOnOfferWallChanged();
        //        }
        //    }
        //    else if (isAvailable)
        //    {
        //        if (mOfferWallDelayEventChanged != null)
        //        {
        //            StopCoroutine(mOfferWallDelayEventChanged);
        //            mOfferWallDelayEventChanged = null;
        //        }
        //        mOfferWallDelayEventChanged = CRDelayInternalEventOnOfferWallChanged();
        //        StartCoroutine(mOfferWallDelayEventChanged);
        //    }
        //    else
        //    {
        //        if (mOfferWallDelayEventChanged != null)
        //        {
        //            StopCoroutine(mOfferWallDelayEventChanged);
        //            mOfferWallDelayEventChanged = null;
        //        }

        //        if ((object)InternalEventOnOfferWallChanged != null)
        //        {
        //            InternalEventOnOfferWallChanged();
        //        }
        //    }
        //}

        //IEnumerator CRDelayInternalEventOnOfferWallChanged()
        //{
        //    WaitForSeconds waitTime = new WaitForSeconds(BaseAdsAdapter.AdsAvailableSafeTime);
        //    yield return waitTime;
        //    yield return null;
        //    mOfferWallDelayEventChanged = null;
        //    if ((object)InternalEventOnOfferWallChanged != null)
        //    {
        //        InternalEventOnOfferWallChanged();
        //    }
        //}
        //#endregion

        private void ImpressionSuccessEvent(LevelPlayImpressionData impressionData)
        {
            if (impressionData == null || impressionData.AllData == null || impressionData.AllData.Length < 10) return;
            //Dictionary<string, object> dic = new Dictionary<string, object>();

            //dic["auctionId"] = string.IsNullOrEmpty(impressionData.auctionId) ? "" : impressionData.auctionId;
            //dic["adUnit"] = string.IsNullOrEmpty(impressionData.adUnit) ? "" : impressionData.adUnit;
            //dic["country"] = string.IsNullOrEmpty(impressionData.country) ? "" : impressionData.country;
            //dic["ab"] = string.IsNullOrEmpty(impressionData.ab) ? "" : impressionData.ab;
            //dic["segmentName"] = string.IsNullOrEmpty(impressionData.segmentName) ? "" : impressionData.segmentName;
            //dic["placement"] = string.IsNullOrEmpty(impressionData.placement) ? "" : impressionData.placement;
            //dic["adNetwork"] = string.IsNullOrEmpty(impressionData.adNetwork) ? "" : impressionData.adNetwork;
            //dic["instanceName"] = string.IsNullOrEmpty(impressionData.instanceName) ? "" : impressionData.instanceName;
            //dic["instanceId"] = string.IsNullOrEmpty(impressionData.instanceId) ? "" : impressionData.instanceId;
            //dic["revenue"] = impressionData.revenue;
            //dic["precision"] = string.IsNullOrEmpty(impressionData.precision) ? "" : impressionData.precision;
            //dic["lifetimeRevenue"] = impressionData.lifetimeRevenue;
            //dic["encryptedCPM"] = string.IsNullOrEmpty(impressionData.encryptedCPM) ? "" : impressionData.encryptedCPM;

            //Analytics.TrackingManager.I.TrackEvent("IRONSOURCE_IMPRESSION", dic);

            var adsType = AdsType.NUM;
            if (impressionData.AdFormat == "banner") adsType = AdsType.Banner;
            else if (impressionData.AdFormat == "interstitial") adsType = AdsType.Interstitial;
            else if (impressionData.AdFormat == "rewarded_video") adsType = AdsType.RewardedVideo;
            if (adsType != AdsType.NUM) TrackAdRevenue(adsType, impressionData.Revenue ?? 0, impressionData.AdNetwork, "USD", "ironSource", impressionData.MediationAdUnitId, impressionData.AdFormat, impressionData.Placement);
        }

        protected override void SetCustomUserId(string userId)
        {
            LevelPlay.SetDynamicUserId(userId);
        }
    }

    public class IronsourceAdStatusHandler : AdStatusHandler
    {
        internal LevelPlayBannerAd BannerAd;
        internal LevelPlayInterstitialAd InterstitialAd;
        internal LevelPlayRewardedAd RewardAd;

        public IronsourceAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return _Available;
        }

        protected override bool InterstitialAdAvailable()
        {
            return InterstitialAd.IsAdReady();
        }

        protected override bool RewardAdAvailable()
        {
            return RewardAd.IsAdReady();
        }

        //protected override bool OfferWallAvailable()
        //{
        //    return IronSource.Agent.isOfferwallAvailable();
        //}
    }
}
#endif
