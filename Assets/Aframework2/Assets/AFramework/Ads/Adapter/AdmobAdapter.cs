#if USE_ADMOB
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Common;
using GoogleMobileAds.Api;

namespace AFramework.Ads
{
    public class AdmobAdapter : BaseAdsAdapter
    {
        const double ADS_BALUE_UNIT = 1000000;
        protected BaseAdapterConfig Config { get; private set; }
        GoogleMobileAds.GoogleMobileAdsClientFactory clientFactory;
        IMobileAdsClient mobileAdsClient;

        public override void Init(object[] parameters)
        {
            mConfig = ((BaseAdapterConfig)parameters[0]);
            Config = (BaseAdapterConfig)mConfig;
            //MobileAds.Initialize(AdModInitCallback);
            if (clientFactory == null)
            { 
                clientFactory = new GoogleMobileAds.GoogleMobileAdsClientFactory();
            }
            mobileAdsClient = clientFactory.MobileAdsInstance();
            mobileAdsClient.Initialize(AdModInitCallback);
            //base.Init(parameters);
        }

        void AdModInitCallback(IInitializationStatusClient result)
        {
//#if DEVELOPMENT_BUILD || UNITY_EDITOR
//            ReflectionHelper._InvokeNamespaceClassStaticMethod("GoogleMobileAds.Api.Mediation.AppLovin", "Initialize", false);
//#endif
#if !UNITY_EDITOR
            if (AdsManager.Debugging)
            {
                foreach (var pair in result.getAdapterStatusMap())
                {
                    Debug.Log(pair.Key + " " + pair.Value.InitializationState);
                }
            }
#endif
            base.Init(null);
        }

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            return new AdmodAdStatusHandler(type, id);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            base.DownloadAd(ad);
            switch (ad._Type)
            {
                case AdsType.Banner:
                    {
                        var bannerClient = clientFactory.BuildBannerClient();
                        bannerClient.CreateBannerView(ad._Id, GetBannerSize(), AdapterBannerPosition(mBannerPosition));
                        var currentAdhandler = GetCurrentDownload(AdsType.Banner) as AdmodAdStatusHandler;
                        currentAdhandler.AssignData(bannerClient);

                        bannerClient.OnAdOpening += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { BannerAdOpenedEvent(currentAdhandler); });
                        };
                        bannerClient.OnAdLoaded += (sender, args) =>
                        {
                            var backupSender = sender;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                BannerAdLoadedEvent(currentAdhandler, sender, true, null, null);
                            });
                        };
                        bannerClient.OnAdFailedToLoad += (sender, args) =>
                        {
                            var backupSender = sender;
                            var backupError = args.LoadAdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                BannerAdLoadedEvent(currentAdhandler, sender, false, backupError.GetCode().ToString(), backupError.GetMessage());
                            });
                        };
                        bannerClient.OnPaidEvent += (sender, args) =>
                        {
                            var backupArgs = args;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                TrackAdRevenue(AdsType.Banner, backupArgs.AdValue.Value / ADS_BALUE_UNIT, null, backupArgs.AdValue.CurrencyCode, "Admob", null, null);
                            });
                        };

                        // Create an empty ad request.
                        var request = new AdRequest.Builder().Build();
                        // Load the banner with the request.
                        bannerClient.LoadAd(request);
                    }
                    break;
                case AdsType.Interstitial:
                    {
                        var interstitialClient = clientFactory.BuildInterstitialClient();
                        interstitialClient.CreateInterstitialAd();
                        var currentAdhandler = GetCurrentDownload(AdsType.Interstitial) as AdmodAdStatusHandler;
                        currentAdhandler.AssignData(interstitialClient);

                        interstitialClient.OnAdDidPresentFullScreenContent += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { InterstitialAdOpenedEvent(currentAdhandler); });
                        };
                        interstitialClient.OnAdFailedToPresentFullScreenContent += (sender, args) =>
                        {
                            var backupError = args.AdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => { InterstitialAdOpenFailedEvent(currentAdhandler, backupError); });
                        };
                        interstitialClient.OnAdLoaded += (sender, args) =>
                        {
                            var backupSender = sender;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                InterstitialAdLoadedEvent(currentAdhandler, sender, true, null, null);
                            });
                        };
                        interstitialClient.OnAdFailedToLoad += (sender, args) =>
                        {
                            var backupSender = sender;
                            var backupError = args.LoadAdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                InterstitialAdLoadedEvent(currentAdhandler, sender, false, backupError.GetCode().ToString(), backupError.GetMessage());
                            });
                        };
                        interstitialClient.OnAdDidDismissFullScreenContent += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { InterstitialAdClosedEvent(currentAdhandler); });
                        };
                        interstitialClient.OnPaidEvent += (sender, args) =>
                        {
                            var backupArgs = args;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                TrackAdRevenue(AdsType.Interstitial, backupArgs.AdValue.Value / ADS_BALUE_UNIT, null, backupArgs.AdValue.CurrencyCode, "Admob", null, null);
                            });
                        };

                        // Create an empty ad request.
                        var request = new AdRequest.Builder().Build();
                        // Load the interstitial with the request.
                        interstitialClient.LoadAd(ad._Id, request);
                    }
                    break;
                case AdsType.RewardedVideo:
                    {
                        var rewardedAdClient = clientFactory.BuildRewardedAdClient();
                        rewardedAdClient.CreateRewardedAd();
                        var currentAdhandler = GetCurrentDownload(AdsType.RewardedVideo) as AdmodAdStatusHandler;
                        currentAdhandler.AssignData(rewardedAdClient);
                        rewardedAdClient.OnAdDidPresentFullScreenContent += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { RewardedVideoAdOpenedEvent(currentAdhandler); });
                        };
                        rewardedAdClient.OnAdLoaded += (sender, args) =>
                        {
                            var backupSender = sender;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                RewardedVideoAdLoadedEvent(currentAdhandler, sender, true, null, null);
                            });
                        };
                        rewardedAdClient.OnAdFailedToLoad += (sender, args) =>
                        {
                            var backupSender = sender;
                            var backupError = args.LoadAdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                RewardedVideoAdLoadedEvent(currentAdhandler, sender, false, backupError.GetCode().ToString(), backupError.GetMessage());
                            });
                        };
                        rewardedAdClient.OnAdFailedToPresentFullScreenContent += (sender, args) =>
                        {
                            var backupSender = sender;
                            var backupError = args.AdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                RewardedVideoAdFailedToDisplayEvent(currentAdhandler, sender, backupError);
                            });
                        };
                        rewardedAdClient.OnAdDidDismissFullScreenContent += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { RewardedVideoAdClosedEvent(currentAdhandler); });
                        };
                        rewardedAdClient.OnUserEarnedReward += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { RewardedVideoAdRewardedEvent(currentAdhandler); });
                        };

                        rewardedAdClient.OnPaidEvent += (sender, args) =>
                        {
                            var backupArgs = args;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                TrackAdRevenue(AdsType.RewardedVideo, backupArgs.AdValue.Value / ADS_BALUE_UNIT, null, backupArgs.AdValue.CurrencyCode, "Admob", null, null);
                            });
                        };

                        var request = new AdRequest.Builder().Build();
                        // Load the rewarded ad with the request.
                        rewardedAdClient.LoadAd(ad._Id, request);
                    }
                    break;
                case AdsType.InterstitialRewardedVideo:
                    {
                        var rewardedInterAdClient = clientFactory.BuildRewardedInterstitialAdClient();
                        rewardedInterAdClient.CreateRewardedInterstitialAd();
                        var currentAdhandler = GetCurrentDownload(AdsType.InterstitialRewardedVideo) as AdmodAdStatusHandler;
                        currentAdhandler.AssignData(rewardedInterAdClient);
                        rewardedInterAdClient.OnPaidEvent += (sender, args) =>
                        {
                            var backupArgs = args;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                TrackAdRevenue(AdsType.InterstitialRewardedVideo, backupArgs.AdValue.Value / ADS_BALUE_UNIT, null, backupArgs.AdValue.CurrencyCode, "Admob", null, null);
                            });
                        };
                        rewardedInterAdClient.OnAdFailedToPresentFullScreenContent += (sender, args) =>
                        {
                            var backupSender = sender;
                            var backupError = args.AdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                InterstitialRewardAdFailedToDisplayEvent(sender, backupError);
                            });
                        };
                        rewardedInterAdClient.OnAdDidPresentFullScreenContent += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { InterstitialRewardAdOpenedEvent(); });
                        };
                        rewardedInterAdClient.OnAdLoaded += (sender, args) =>
                        {
                            var backupSender = sender;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                InterstitialRewardAdsLoadCallback(currentAdhandler, sender, true, null, null);
                            });
                        };
                        rewardedInterAdClient.OnAdFailedToLoad += (sender, args) =>
                        {
                            var backupSender = sender;
                            var backupError = args.LoadAdErrorClient;
                            UnityMainThreadDispatcher.instance.Enqueue(() => {
                                InterstitialRewardAdsLoadCallback(currentAdhandler, sender, false, backupError.GetCode().ToString(), backupError.GetMessage());
                            });
                        };
                        rewardedInterAdClient.OnAdDidDismissFullScreenContent += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { InterstitialRewardAdClosedEvent(); });
                        };
                        rewardedInterAdClient.OnUserEarnedReward += (sender, args) =>
                        {
                            UnityMainThreadDispatcher.instance.Enqueue(() => { InterstitialRewardAdRewardedEvent(args); });
                        };

                        var request = new AdRequest.Builder().Build();
                        // Load the rewarded ad with the request.
                        rewardedInterAdClient.LoadAd(ad._Id, request);
                    }
                    break;
            }
        }

#region BannerAds
        public override void SetBannerPosition(BannerPosition position)
        {
            base.SetBannerPosition(position);
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            (mDefaultBannerAdList[0] as AdmodAdStatusHandler).bannerData.SetPosition(AdapterBannerPosition(mBannerPosition));
        }

        public override void HideAdsBanner()
        {
            if (AdsManager.EventOnBannerAdsChanged != null) AdsManager.EventOnBannerAdsChanged(false);
            base.HideAdsBanner();
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            (mDefaultBannerAdList[0] as AdmodAdStatusHandler).bannerData.HideBannerView();
        }

        public override void ShowAdsBanner()
        {
            base.ShowAdsBanner();
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            (mDefaultBannerAdList[0] as AdmodAdStatusHandler).bannerData.ShowBannerView();
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(true);
            }
        }

        AdPosition AdapterBannerPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                    return AdPosition.Top;
                case BannerPosition.Bottom:
                    return AdPosition.Bottom;
                case BannerPosition.TopLeft:
                    return AdPosition.TopLeft;
                case BannerPosition.TopRight:
                    return AdPosition.TopRight;
                case BannerPosition.BottomLeft:
                    return AdPosition.BottomRight;
                case BannerPosition.Center:
                    return AdPosition.Center;
            }
            return AdPosition.Bottom;
        }

        AdSize GetBannerSize()
        {
#if UNITY_EDITOR
            return AdSize.Banner;
#else
            int width = Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height) / (Screen.dpi / 160));
            int height = Mathf.RoundToInt(width / BANNER_RATIO);
            return new AdSize(width, height);
#endif
        }

        void BannerAdLoadedEvent(AdmodAdStatusHandler adHandler, object sender, bool success, string errorCode, string errorMessage)
        {
            var currentDownload = GetCurrentDownload(AdsType.Banner);
            if (AdsManager.Debugging && currentDownload != adHandler) Debug.LogWarning("BannerAds - Return download handler is not same as current download status " + success + " current " + currentDownload + "  other " + adHandler);

            if (currentDownload == adHandler)
            {
                AdDownloadCallback(AdsType.Banner, success, errorCode, errorMessage);
            }
            else
            {
                adHandler.OnAdAvailabilityUpdate(success);
                ForceFreeCurrentDownload(AdsType.Banner);
            }

            //if ((object)InternalEventOnBannerAdsChanged != null)
            //{
            //    InternalEventOnBannerAdsChanged();
            //}

            if (mBannerAdVisibility)
            {
                ShowAdsBanner();
            }
            else
            {
                if (AdsManager.EventOnBannerAdsChanged != null) AdsManager.EventOnBannerAdsChanged(false);
                HideAdsBanner();
            }
            if (AdsManager.Debugging) Debug.Log(success ? "BannerAds - Request Success" : ("BannerAds - Request Fail - " + errorMessage));
        }

        void BannerAdOpenedEvent(AdmodAdStatusHandler adHandler)
        {
            if (!mBannerAdVisibility)
            {
                HideAdsBanner();
                return;
            }
            if (AdsManager.Debugging) Debug.Log("BannerAds - Ads Opened");
        }

#endregion

#region InterstitialAds
        public override bool ShowAdsInterstitial(System.Action<bool> callback, string adId = null)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultInterstitialAdList.Count; ++i)
                {
                    if (mDefaultInterstitialAdList[i]._Available)
                    {
                        adId = mDefaultInterstitialAdList[i]._Id;
                        break;
                    }
                }
            }
            if (!IsInterstitialAdAvailable(adId)) return false;
            base.ShowAdsInterstitial(callback, adId);
            mCurrentFullscreenAd = mAdDownloadHandler[adId];

            //if (mAdsInterstitialTimeoutThread != null)
            //{
            //    StopCoroutine(mAdsInterstitialTimeoutThread);
            //    mAdsInterstitialTimeoutThread = null;
            //}
            //mAdsInterstitialTimeoutThread = CRAdsInterstitialTimeoutThread();
            //StartCoroutine(mAdsInterstitialTimeoutThread);

            (mCurrentFullscreenAd as AdmodAdStatusHandler).interstitialData.Show();
#if UNITY_EDITOR
            var allAdsCanvas = FindObjectsOfType<Canvas>();
            if (allAdsCanvas != null && allAdsCanvas.Length > 0)
            {
                for (int i = 0; i < allAdsCanvas.Length; ++i)
                {
                    if (allAdsCanvas[i].name.StartsWith("768x1024"))
                    {
                        allAdsCanvas[i].gameObject.GetComponent<Canvas>().sortingOrder = 9999;
                    }
                }
            }
#endif
            return true;
        }

        void InterstitialAdLoadedEvent(AdmodAdStatusHandler adHandler, object sender, bool success, string errorCode, string errorMessage)
        {
            var currentDownload = GetCurrentDownload(AdsType.Interstitial);
            if (AdsManager.Debugging && currentDownload != adHandler) Debug.LogWarning("InterstitialAds - Return download handler is not same as current download");

            if (currentDownload == adHandler)
            {
                AdDownloadCallback(AdsType.Interstitial, success, errorCode, errorMessage);
            }
            else
            {
                adHandler.OnAdAvailabilityUpdate(success);
                ForceFreeCurrentDownload(AdsType.Interstitial);
            }

            //TODO
            //if ((object)InternalEventOnInterstitialAdsChanged != null)
            //{
            //    InternalEventOnInterstitialAdsChanged();
            //}
            if (AdsManager.Debugging) Debug.Log(success ? "InterstitialAds - Request Success" : ("InterstitialAds - Request Fail - " + errorMessage));
        }

        void InterstitialAdOpenedEvent(AdmodAdStatusHandler adHandler)
        {
            AudioListener.volume = 0;
            FullscreenAdShowing = true;

            if (AdsManager.Debugging) Debug.Log("InterstitialAds - Ads Opened");
            AdDisplayResultCallback(AdsType.Interstitial, true, null, null);
        }

        void InterstitialAdOpenFailedEvent(AdmodAdStatusHandler adHandler, IAdErrorClient error)
        {
            AudioListener.volume = 1;
            FullscreenAdShowing = false;

            if (AdsManager.Debugging) Debug.Log("InterstitialAds - Ads Open FAILED");
            AdDisplayResultCallback(AdsType.Interstitial, false, error.GetCode().ToString(), error.GetMessage());
        }

        void InterstitialAdClosedEvent(AdmodAdStatusHandler adHandler)
        {
            AudioListener.volume = 1;
            if (mInterstitialAdCallback != null)
            {
                mInterstitialAdCallback(true);
                mInterstitialAdCallback = null;
            }
            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                if (AdsManager.Debugging && mCurrentFullscreenAd != adHandler) Debug.LogWarning("RewardAds - Current Fullscreen Ad is not same as param adHandler");
                mCurrentFullscreenAd = null;
            }
            FullscreenAdShowing = false;
            if (AdsManager.Debugging) Debug.Log("InterstitialAds - Ads Closed");
        }

#endregion

#region RewardAds
        bool mHaveRewarded = false;
        IEnumerator mRewardedVideoDelayEventChanged = null;
        IEnumerator mDelayRewardCheckThread;

        public override bool ShowAdsReward(System.Action<bool> callback, string adId = null)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultRewardAdList.Count; ++i)
                {
                    if (mDefaultRewardAdList[i]._Available)
                    {
                        adId = mDefaultRewardAdList[i]._Id;
                        break;
                    }
                }
            }
            if (!IsRewardAdAvailable(adId)) return false;
            base.ShowAdsReward(callback);
            mCurrentFullscreenAd = mAdDownloadHandler[adId];

            //if (mAdsRewardTimeoutThread != null)
            //{
            //    StopCoroutine(mAdsRewardTimeoutThread);
            //    mAdsRewardTimeoutThread = null;
            //}
            //mAdsRewardTimeoutThread = CRAdsRewardTimeoutThread();
            //StartCoroutine(mAdsRewardTimeoutThread);

            (mCurrentFullscreenAd as AdmodAdStatusHandler).rewardedVideoData.Show();
#if UNITY_EDITOR
            var allAdsCanvas = FindObjectsOfType<Canvas>();
            if (allAdsCanvas != null && allAdsCanvas.Length > 0)
            {
                for (int i = 0; i < allAdsCanvas.Length; ++i)
                {
                    if (allAdsCanvas[i].name.StartsWith("768x1024"))
                    {
                        allAdsCanvas[i].gameObject.GetComponent<Canvas>().sortingOrder = 9999;
                    }
                }
            }
#endif
            return true;
        }

        void RewardedVideoAdLoadedEvent(AdmodAdStatusHandler adHandler, object sender, bool success, string errorCode, string errorMessage)
        {
            var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
            if (AdsManager.Debugging && currentDownload != adHandler) Debug.LogWarning("RewardAds - Return download handler is not same as current download");

            if (currentDownload == adHandler)
            {
                AdDownloadCallback(AdsType.RewardedVideo, success, errorCode, errorMessage);
            }
            else
            {
                adHandler.OnAdAvailabilityUpdate(success);
                ForceFreeCurrentDownload(AdsType.RewardedVideo);
            }

            if (BaseAdsAdapter.AdsAvailableSafeTime <= 0)
            {
                if ((object)InternalEventOnRewardAdsChanged != null)
                {
                    InternalEventOnRewardAdsChanged();
                }
            }
            else if (success)
            {
                if (mRewardedVideoDelayEventChanged != null)
                {
                    StopCoroutine(mRewardedVideoDelayEventChanged);
                    mRewardedVideoDelayEventChanged = null;
                }

                mRewardedVideoDelayEventChanged = CRDelayInternalEventOnRewardAdsChanged();
                StartCoroutine(mRewardedVideoDelayEventChanged);
            }
            else
            {
                if (mRewardedVideoDelayEventChanged != null)
                {
                    StopCoroutine(mRewardedVideoDelayEventChanged);
                    mRewardedVideoDelayEventChanged = null;
                }

                if ((object)InternalEventOnRewardAdsChanged != null)
                {
                    InternalEventOnRewardAdsChanged();
                }
            }

            if (AdsManager.Debugging) Debug.Log(success ? "RewardAds - Request Success" : ("RewardAds - Request Fail - " + errorMessage));
        }

        void RewardedVideoAdOpenedEvent(AdmodAdStatusHandler adHandler)
        {
            AudioListener.volume = 0;
            FullscreenAdShowing = true;
            mHaveRewarded = false;

            if (AdsManager.Debugging) Debug.Log("RewardAds - Ads Opened");
            AdDisplayResultCallback(AdsType.RewardedVideo, true, null, null);
        }

        void RewardedVideoAdFailedToDisplayEvent(AdmodAdStatusHandler adHandler, object sender, IAdErrorClient error)
        {
            if (AdsManager.Debugging) Debug.Log("RewardAds - Display Fail - " + error.GetMessage());
            AudioListener.volume = 1;
            if (mRewardAdCallback != null)
            {
                mRewardAdCallback(false);
                mRewardAdCallback = null;
            }
            if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
            mCurrentFullscreenAd = null;
            FullscreenAdShowing = false;
            AdDisplayResultCallback(AdsType.RewardedVideo, false, error.GetCode().ToString(), error.GetMessage());
        }

        void RewardedVideoAdClosedEvent(AdmodAdStatusHandler adHandler)
        {
            AudioListener.volume = 1;

            if (mCurrentFullscreenAd != null)
            {
                if (AdsManager.Debugging && mCurrentFullscreenAd != adHandler) Debug.LogError("RewardAds - Current Fullscreen Ad is not same as param adHandler");
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }

            if (mHaveRewarded)
            {
                if (mRewardAdCallback != null)
                {
                    mRewardAdCallback(mHaveRewarded);
                    mRewardAdCallback = null;
                }
                FullscreenAdShowing = false;
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

            if (AdsManager.Debugging) Debug.Log("RewardAds - Ads Closed");
        }

        void RewardedVideoAdRewardedEvent(AdmodAdStatusHandler adHandler)
        {
            mHaveRewarded = true;
            if (AdsManager.Debugging) Debug.Log("RewardAds - Handle Reward");
        }

        IEnumerator CRDelayInternalEventOnRewardAdsChanged()
        {
            WaitForSeconds waitTime = new WaitForSeconds(BaseAdsAdapter.AdsAvailableSafeTime);
            yield return waitTime;
            yield return null;
            mRewardedVideoDelayEventChanged = null;

            if ((object)InternalEventOnRewardAdsChanged != null)
            {
                InternalEventOnRewardAdsChanged();
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
                    if (mRewardAdCallback != null)
                    {
                        mRewardAdCallback(true);
                        mRewardAdCallback = null;
                    }
                    mHaveRewarded = false;
                    FullscreenAdShowing = false;
                    mDelayRewardCheckThread = null;
                    yield break;
                }
            }
            if (mRewardAdCallback != null)
            {
                mRewardAdCallback(mHaveRewarded);
                mRewardAdCallback = null;
            }
            mHaveRewarded = false;
            FullscreenAdShowing = false;
            mDelayRewardCheckThread = null;
        }
#endregion

#region InterstitialRewardAds
        public override bool ShowAdsInterstitialReward(System.Action<bool> callback, string adId = null)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultInterstitialRewardAdList.Count; ++i)
                {
                    if (mDefaultInterstitialRewardAdList[i]._Available)
                    {
                        adId = mDefaultInterstitialRewardAdList[i]._Id;
                        break;
                    }
                }
            }
            if (!IsInterstitialRewardAdAvailable(adId)) return false;
            base.ShowAdsInterstitialReward(callback, adId);
            mCurrentFullscreenAd = mAdDownloadHandler[adId];

            mHaveRewarded = false;
            (mCurrentFullscreenAd as AdmodAdStatusHandler).interstitiaRewardedVideoData.Show();
#if UNITY_EDITOR
            var allAdsCanvas = FindObjectsOfType<Canvas>();
            if (allAdsCanvas != null && allAdsCanvas.Length > 0)
            {
                for (int i = 0; i < allAdsCanvas.Length; ++i)
                {
                    if (allAdsCanvas[i].name.StartsWith("768x1024"))
                    {
                        allAdsCanvas[i].gameObject.GetComponent<Canvas>().sortingOrder = 9999;
                    }
                }
            }
#endif
            return true;
        }

        private void InterstitialRewardAdsLoadCallback(AdmodAdStatusHandler adHandler, object sender, bool success, string errorCode, string errorMessage)
        {
            var currentDownload = GetCurrentDownload(AdsType.InterstitialRewardedVideo);
            if (AdsManager.Debugging && currentDownload != adHandler) Debug.LogWarning("InterstitialRewardAds - Return download handler is not same as current download");

            if (currentDownload == adHandler)
            {
                AdDownloadCallback(AdsType.InterstitialRewardedVideo, success, errorCode, errorMessage);
            }
            else
            {
                adHandler.OnAdAvailabilityUpdate(success);
                ForceFreeCurrentDownload(AdsType.InterstitialRewardedVideo);
            }
            if (AdsManager.Debugging) Debug.Log(success ? "InterstitialRewardAds - Request Success" : ("InterstitialRewardAds - Request Fail - " + errorMessage));
        }

        void InterstitialRewardAdOpenedEvent()
        {
            AudioListener.volume = 0;
            FullscreenAdShowing = true;

            if (AdsManager.Debugging) Debug.Log("InterstitialRewardAds - Ads Opened");
            AdDisplayResultCallback(AdsType.InterstitialRewardedVideo, true, null, null);
        }

        void InterstitialRewardAdFailedToDisplayEvent(object sender, IAdErrorClient error)
        {
            if (AdsManager.Debugging) Debug.Log("InterstitialRewardAds - Display Fail - " + error.GetMessage());
            AudioListener.volume = 1;
            if (mRewardAdCallback != null)
            {
                mRewardAdCallback(false);
                mRewardAdCallback = null;
            }
            if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
            mCurrentFullscreenAd = null;
            FullscreenAdShowing = false;
            AdDisplayResultCallback(AdsType.InterstitialRewardedVideo, false, error.GetCode().ToString(), error.GetMessage());
        }

        void InterstitialRewardAdClosedEvent()
        {
            AudioListener.volume = 1;

            if (mCurrentFullscreenAd != null)
            {
                mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
            }

            if (mHaveRewarded)
            {
                if (mRewardAdCallback != null)
                {
                    mRewardAdCallback(mHaveRewarded);
                    mRewardAdCallback = null;
                }
                FullscreenAdShowing = false;
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

            if (AdsManager.Debugging) Debug.Log("InterstitialRewardAds - Ads Closed");
        }

        private void InterstitialRewardAdRewardedEvent(Reward reward)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                mHaveRewarded = true;
                if (AdsManager.Debugging) Debug.Log("InterstitialRewardAds - Handle Reward");
            });
        }
#endregion
    }

    public class AdmodAdStatusHandler : AdStatusHandler
    {
        public IBannerClient bannerData { get; protected set; }
        public IInterstitialClient interstitialData { get; protected set; }
        public IRewardedAdClient rewardedVideoData { get; protected set; }
        public IRewardedInterstitialAdClient interstitiaRewardedVideoData { get; protected set; }

        public AdmodAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return bannerData != null && _Available;
        }

        protected override bool InterstitialAdAvailable()
        {
            return interstitialData != null && _Available;// interstitialData.IsLoaded();
        }

        protected override bool RewardAdAvailable()
        {
            return rewardedVideoData != null && _Available;// rewardedVideoData.IsLoaded();
        }

        protected override bool InterstitialRewardAdAvailable()
        {
            return interstitiaRewardedVideoData != null;
        }

        //protected override bool OfferWallAvailable()
        //{
        //    return false;//not supported
        //}

        public override void OnAdAvailabilityUpdate(bool result)
        {
            base.OnAdAvailabilityUpdate(result);
            if (result)
            {
                //mLastCheckTime = -1;
            }
            else
            {
                CleanData();
            }
        }

        public void AssignData(object ptr)
        {
            switch (_Type)
            {
                case AdsType.Banner:
                    bannerData = ptr as IBannerClient;
                    break;
                case AdsType.Interstitial:
                    interstitialData = ptr as IInterstitialClient;
                    break;
                case AdsType.RewardedVideo:
                    rewardedVideoData = ptr as IRewardedAdClient;
                    break;
                case AdsType.InterstitialRewardedVideo:
                    interstitiaRewardedVideoData = ptr as IRewardedInterstitialAdClient;
                    break;
            }
        }

        void CleanData()
        {
            if (bannerData != null)
            {
                bannerData.DestroyBannerView();
                bannerData = null;
            }
            else if (interstitialData != null)
            {
                interstitialData.DestroyInterstitial();
                interstitialData = null;
            }
            else if (rewardedVideoData != null)
            {
                rewardedVideoData.DestroyRewardedAd();
                rewardedVideoData = null;
            }
            else if (interstitiaRewardedVideoData != null)
            {
                interstitiaRewardedVideoData.DestroyRewardedInterstitialAd();
                interstitiaRewardedVideoData = null;
            }
        }
    }
}
#endif