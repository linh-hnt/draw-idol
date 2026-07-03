#if USE_UNITY_ADS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Mediation;
using System;
using System.Threading.Tasks;

namespace AFramework.Ads
{
    public class UnityAdapter : BaseAdsAdapter
    {
        protected BaseAdapterConfig Config { get; private set; }

        public override void Init(object[] parameters)
        {
            mConfig = ((BaseAdapterConfig)parameters[0]);
            Config = mConfig;// (AppLovinAdapterConfig)mConfig;

            InitAsync(parameters);
        }

        async void InitAsync(object[] parameters)
        {
#if USE_FIREBASE
            await Task.Delay(500);
#endif
            InitializationOptions options = new InitializationOptions();
            options.SetGameId(Config.Platform.AppId);
            await UnityServices.InitializeAsync(options);
            MediationService.Instance.ImpressionEventPublisher.OnImpression += (sender, args) => {
                UnityMainThreadDispatcher.instance.Enqueue(() =>
                {
                    ImpressionEvent(sender, args);
                });
            };
#if USE_APPSFLYER_ANALYTICS
            AppsFlyerSDK.AppsFlyerAdRevenue.start();
#endif
            base.Init(parameters);
        }

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            return new UnityAdStatusHandler(type, id);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            UnityAdStatusHandler unityHandler = ad as UnityAdStatusHandler;
            switch (ad._Type)
            {
                case AdsType.Banner:
                    if (unityHandler.bannerAd == null)
                    {
                        unityHandler.bannerAd = MediationService.Instance.CreateBannerAd(ad._Id, BannerAdPredefinedSize.Banner.ToBannerAdSize(), AdapterBannerPosition(mBannerPosition));
                        //todo callback
                    }
                    if (unityHandler.bannerAd.AdState != AdState.Unloaded) return;
                    break;
                case AdsType.Interstitial:
                    if (unityHandler.interstitialAd == null)
                    {
                        unityHandler.interstitialAd = MediationService.Instance.CreateInterstitialAd(ad._Id);

                        unityHandler.interstitialAd.OnLoaded += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnInterstitialLoadedEvent(sender, args);
                            });
                        };
                        unityHandler.interstitialAd.OnFailedLoad += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnInterstitialLoadFailedEvent(sender, args);
                            });
                        };
                        unityHandler.interstitialAd.OnShowed += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnInterstitialDisplayedEvent(sender, args);
                            });
                        };
                        unityHandler.interstitialAd.OnClicked += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnInterstitialClickedEvent(sender, args);
                            });
                        };
                        unityHandler.interstitialAd.OnClosed += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnInterstitialHiddenEvent(sender, args);
                            });
                        };
                        unityHandler.interstitialAd.OnFailedShow += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnInterstitialAdFailedToDisplayEvent(sender, args);
                            });
                        };
                    }
                    //if (!(unityHandler.interstitialAd.AdState == AdState.Unloaded || (unityHandler.interstitialAd.AdState == AdState.Showing && !FullscreenAdShowing))) return;
                    if (unityHandler.interstitialAd.AdState != AdState.Unloaded) return;
                    break;
                case AdsType.RewardedVideo:
                    if (unityHandler.rewardAd == null)
                    {
                        unityHandler.rewardAd = MediationService.Instance.CreateRewardedAd(ad._Id);

                        unityHandler.rewardAd.OnLoaded += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdLoadedEvent(sender, args);
                            });
                        };
                        unityHandler.rewardAd.OnFailedLoad += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdLoadFailedEvent(sender, args);
                            });
                        };
                        unityHandler.rewardAd.OnShowed += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdDisplayedEvent(sender, args);
                            });
                        };
                        unityHandler.rewardAd.OnClicked += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdClickedEvent(sender, args);
                            });
                        };
                        unityHandler.rewardAd.OnClosed += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdHiddenEvent(sender, args);
                            });
                        };
                        unityHandler.rewardAd.OnFailedShow += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdFailedToDisplayEvent(sender, args);
                            });
                        };
                        unityHandler.rewardAd.OnUserRewarded += (sender, args) => {
                            UnityMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                HandleOnRewardedAdReceivedRewardEvent(sender, args);
                            });
                        };
                    }
                    //if (!(unityHandler.rewardAd.AdState == AdState.Unloaded || (unityHandler.rewardAd.AdState == AdState.Showing && !FullscreenAdShowing))) return;
                    if (unityHandler.rewardAd.AdState != AdState.Unloaded) return;
                    break;
            }
            LoadUnityAd(unityHandler);
            base.DownloadAd(ad);
        }

        async void LoadUnityAd(UnityAdStatusHandler handle)
        {
            try
            {
                if (handle._Type == AdsType.Banner) await handle.bannerAd.LoadAsync();
                else if (handle._Type == AdsType.Interstitial) await handle.interstitialAd.LoadAsync();
                else if (handle._Type == AdsType.RewardedVideo) await handle.rewardAd.LoadAsync();
            }
            catch (LoadFailedException e)
            {
                AdDownloadCallback(handle._Type, false, e.LoadError.ToString(), e.Message);
            }
        }

        void ImpressionEvent(object sender, ImpressionEventArgs args)
        {
            if (AdsManager.Debugging) Debug.Log("Unity ImpressionEvent");
            var adHandler = mAdDownloadHandler.ContainsKey(args.AdUnitId) ? mAdDownloadHandler[args.AdUnitId] : null;
            if (adHandler != null)
            {
                if (args.ImpressionData != null)
                {
                    TrackAdRevenue(adHandler._Type, args.ImpressionData.PublisherRevenuePerImpression, args.ImpressionData.AdSourceName, args.ImpressionData.Currency, "Unity", args.ImpressionData.AdUnitId, args.ImpressionData.AdUnitFormat);

#if USE_APPSFLYER_ANALYTICS
                    var dic = new Dictionary<string, string>();
                    dic.Add("AdUnitID", args.ImpressionData.AdUnitId);
                    AppsFlyerSDK.AppsFlyerAdRevenue.logAdRevenue(args.ImpressionData.AdSourceName, AppsFlyerSDK.AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeUnity, args.ImpressionData.PublisherRevenuePerImpression, args.ImpressionData.Currency, dic);
#endif
                }
                else
                {
                    TrackAdRevenue(adHandler._Type, 0, "unknow", "USD");
                }
            }
        }

        #region BannerAd
        BannerAdAnchor AdapterBannerPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                    return BannerAdAnchor.TopCenter;
                case BannerPosition.Bottom:
                    return BannerAdAnchor.BottomCenter;
                case BannerPosition.TopLeft:
                    return BannerAdAnchor.TopLeft;
                case BannerPosition.TopRight:
                    return BannerAdAnchor.TopRight;
                case BannerPosition.BottomLeft:
                    return BannerAdAnchor.BottomLeft;
                case BannerPosition.BottomRight:
                    return BannerAdAnchor.BottomRight;
                case BannerPosition.Center:
                    return BannerAdAnchor.Center;
                case BannerPosition.CenterLeft:
                    return BannerAdAnchor.MiddleLeft;
                case BannerPosition.CenterRight:
                    return BannerAdAnchor.MiddleRight;
            }
            return BannerAdAnchor.BottomCenter;
        }

        public override void ShowAdsBanner()
        {
            base.ShowAdsBanner();
            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            var unityHandler = mDefaultBannerAdList[0] as UnityAdStatusHandler;
            if (unityHandler.bannerAd.AdState != AdState.Loaded) return;
            Debug.LogError("TODO");
        }

        public override void HideAdsBanner()
        {
            base.HideAdsBanner();
            Debug.LogError("TODO");
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

            try
            {
                var showOptions = new InterstitialAdShowOptions();
                showOptions.AutoReload = true;
                (mCurrentFullscreenAd as UnityAdStatusHandler).interstitialAd.ShowAsync(showOptions);
            } catch (ShowFailedException e)
            {
                return false;
            }
            if (AdsManager.Debugging) Debug.Log("ShowAdsInterstitial " + mCurrentFullscreenAd._Id);
            return true;
        }

        private void HandleOnInterstitialLoadedEvent(object sender, EventArgs args)
        {
            var adId = (sender as IInterstitialAd).AdUnitId;
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

        private void HandleOnInterstitialLoadFailedEvent(object sender, LoadErrorEventArgs error)
        {
            IInterstitialAd senderAd = sender as IInterstitialAd;
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialLoadFailedEvent " + senderAd.AdUnitId + " code: " + (error != null ? error.Error.ToString() : "-1"));
            var currentDownload = GetCurrentDownload(AdsType.Interstitial);
            if (currentDownload != null && currentDownload._Id == senderAd.AdUnitId)
            {
                AdDownloadCallback(AdsType.Interstitial, false, error.Error.ToString(), error.Message);
            }
            else if (mAdDownloadHandler.ContainsKey(senderAd.AdUnitId))
            {
                mAdDownloadHandler[senderAd.AdUnitId].OnAdAvailabilityUpdate(false);
                ForceFreeCurrentDownload(AdsType.Interstitial);
            }
        }

        private void HandleOnInterstitialDisplayedEvent(object sender, EventArgs args)
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

        private void HandleOnInterstitialClickedEvent(object sender, EventArgs args)
        {
            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialClickedEvent");
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Interstitial);
        }

        private void HandleOnInterstitialHiddenEvent(object sender, EventArgs args)
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

        private void HandleOnInterstitialAdFailedToDisplayEvent(object sender, ShowErrorEventArgs error)
        {
            if (AdsManager.Debugging)
            {
                string adId = sender != null ? (sender as IInterstitialAd).AdUnitId : "unknow";
                Debug.Log("HandleOnInterstitialAdFailedToDisplayEvent " + adId + " code: " + (error != null ? error.Error.ToString() : "-1"));
            }
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
                AdDisplayResultCallback(AdsType.Interstitial, false, error.Error.ToString(), error.Message);
            }
            else
            {
                AdDisplayResultCallback(AdsType.Interstitial, false, "-1", null);
            }
        }

        IEnumerator mAdsInterstitialTimeoutThread;
        IEnumerator CRAdsInterstitialTimeoutThread()
        {
            int currentFrame = Time.frameCount;
            WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(0.5f);
            float totalWaitTime = 10;
            int totalWaitFrame = currentFrame + 90;
            while (totalWaitTime > 0)
            {
                totalWaitTime -= 0.5f;
                yield return waitTime;
            }
            yield return new WaitUntil(() => Time.frameCount > totalWaitFrame);
            HandleOnInterstitialAdFailedToDisplayEvent(mCurrentFullscreenAd != null ? (mCurrentFullscreenAd as UnityAdStatusHandler).interstitialAd : null, null);
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

            try
            {
                var showOptions = new RewardedAdShowOptions();
                showOptions.AutoReload = true;
                (mCurrentFullscreenAd as UnityAdStatusHandler).rewardAd.ShowAsync(showOptions);
            }
            catch (ShowFailedException e)
            {
                return false;
            }
            return true;
        }

        private void HandleOnRewardedAdLoadedEvent(object sender, EventArgs args)
        {
            var adId = (sender as IRewardedAd).AdUnitId;
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

        private void HandleOnRewardedAdLoadFailedEvent(object sender, LoadErrorEventArgs error)
        {
            var adId = (sender as IRewardedAd).AdUnitId;
            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdLoadFailedEvent " + adId + " code " + (error != null ? error.Error.ToString() : "-1"));
            var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
            if (currentDownload != null && currentDownload._Id == adId)
            {
                if (error != null)
                {
                    AdDownloadCallback(AdsType.RewardedVideo, false, error.Error.ToString(), error.Message);
                }
                else
                {
                    AdDownloadCallback(AdsType.RewardedVideo, false, "-1", null);
                }
            }
            else if (mAdDownloadHandler.ContainsKey(adId))
            {
                mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                ForceFreeCurrentDownload(AdsType.RewardedVideo);
            }
        }

        private void HandleOnRewardedAdDisplayedEvent(object sender, EventArgs args)
        {
            var adId = (sender as IRewardedAd).AdUnitId;
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

        private void HandleOnRewardedAdClickedEvent(object sender, EventArgs args)
        {
            if (AdsManager.Debugging)
            {
                var adId = (sender as IRewardedAd).AdUnitId;
                Debug.Log("HandleOnRewardedAdClickedEvent " + adId);
            }
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.RewardedVideo);
        }

        private void HandleOnRewardedAdHiddenEvent(object sender, EventArgs args)
        {
            var adId = (sender as IRewardedAd).AdUnitId;
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

        private void HandleOnRewardedAdFailedToDisplayEvent(object sender, ShowErrorEventArgs error)
        {
            //Tin: Warning error + adInfo can be null
            if (AdsManager.Debugging)
            {
                string adId = sender != null ? (sender as IInterstitialAd).AdUnitId : "unknow";
                Debug.Log("HandleOnRewardedAdFailedToDisplayEvent " + adId + " code " + (error != null ? error.Error.ToString() : "-1"));
            }
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
                AdDisplayResultCallback(AdsType.RewardedVideo, false, error.Error.ToString(), error.Message);
            }
            else
            {
                AdDisplayResultCallback(AdsType.RewardedVideo, false, "-1", null);
            }
        }

        private void HandleOnRewardedAdReceivedRewardEvent(object sender, RewardEventArgs reward)
        {
            if (AdsManager.Debugging)
            {
                var adId = (sender as IRewardedAd).AdUnitId;
                Debug.Log("HandleOnRewardedAdReceivedRewardEvent " + adId);
            }
            mHaveRewarded = true;
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
            HandleOnRewardedAdFailedToDisplayEvent(mCurrentFullscreenAd != null ? (mCurrentFullscreenAd as UnityAdStatusHandler).rewardAd : null, null);
        }
        #endregion
    }

    public class UnityAdStatusHandler : AdStatusHandler
    {
        public IBannerAd bannerAd;
        public IInterstitialAd interstitialAd;
        public IRewardedAd rewardAd;

        public UnityAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return bannerAd != null && bannerAd.AdState >= AdState.Loaded;
        }

        protected override bool InterstitialAdAvailable()
        {
            return interstitialAd != null && interstitialAd.AdState == AdState.Loaded;
        }

        protected override bool RewardAdAvailable()
        {
            return rewardAd != null && rewardAd.AdState == AdState.Loaded;
        }
    }
}
#endif