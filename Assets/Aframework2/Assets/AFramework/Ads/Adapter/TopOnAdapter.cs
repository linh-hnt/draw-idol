#if USE_TOPON_ADS
using AnyThinkAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.Ads
{
    public class TopOnAdapter : BaseAdsAdapter, ATSDKInitListener//, ATBannerAdListener, ATInterstitialAdListener, ATRewardedVideoListener
    {
        string TopOnAppKey = "caf3d5f36c7dd6f8bbe28218542c502c";
        protected BaseAdapterConfig Config { get; set; }
        bool mHaveRewarded = false;

        public override void Init(object[] parameters)
        {
            mConfig = ((BaseAdapterConfig)parameters[0]);
            Config = mConfig;
            if (!string.IsNullOrEmpty(Config.Platform.TopOnAppKey))
            {
                TopOnAppKey = Config.Platform.TopOnAppKey;
            }

            AnyThinkAds.Api.ATSDKAPI.setLogDebug(false);
            AnyThinkAds.Api.ATSDKAPI.initSDK(Config.Platform.TopOnAppId, TopOnAppKey, this);
        }

        public void initFail(string message)
        {
            if (AdsManager.Debugging) Debug.Log("TopOnAdapter initFail");
            StartCoroutine(AFramework.Utility.CRDelayFunction(60, () => {
                AnyThinkAds.Api.ATSDKAPI.initSDK(Config.Platform.TopOnAppId, TopOnAppKey, this);
            }));
        }

        public void initSuccess()
        {
            TODO AdDisplayResultCallback
            if (AdsManager.Debugging) Debug.Log("TopOnAdapter initSuccess");
            if (AdsManager.I.IsAdEnabled(AdsType.Banner))
            {
                //ATBannerAd.Instance.client.setListener(this);
                var client = ATBannerAd.Instance.client;
                client.onAdLoadEvent += onAdLoad;
                client.onAdSourceLoadFailureEvent += onAdLoadFail;

                client.onAdImpressEvent += onAdImpress;
                client.onAdClickEvent += onAdClick;
                client.onAdAutoRefreshEvent += onAdAutoRefresh;
                client.onAdAutoRefreshFailureEvent += onAdAutoRefreshFail;
                client.onAdCloseEvent += onAdClose;
                client.onAdCloseButtonTappedEvent += onAdCloseButtonTapped;
            }
            if (AdsManager.I.IsAdEnabled(AdsType.Interstitial))
            {
                //ATInterstitialAd.Instance.client.setListener(this);
                var client = ATInterstitialAd.Instance.client;
                client.onAdLoadEvent += onInterstitialAdLoad;
                client.onAdSourceLoadFailureEvent += onInterstitialAdLoadFail;

                client.onAdShowEvent += onInterstitialAdShow;
                client.onAdShowFailureEvent += onInterstitialAdFailedToShow;
                client.onAdCloseEvent += onInterstitialAdClose;
                client.onAdClickEvent += onInterstitialAdClick;
                client.onAdVideoStartEvent += onInterstitialAdStartPlayingVideo;
                client.onAdVideoFailureEvent += onInterstitialAdFailedToPlayVideo;
                client.onAdVideoEndEvent += onInterstitialAdEndPlayingVideo;
            }
            if (AdsManager.I.IsAdEnabled(AdsType.RewardedVideo))
            {
                //ATRewardedVideo.Instance.client.setListener(this);
                var client = ATRewardedVideo.Instance.client;

                client.onAdLoadEvent += onRewardedVideoAdLoaded;
                client.onAdSourceLoadFailureEvent += onRewardedVideoAdLoadFail;

                client.onAdVideoStartEvent += onRewardedVideoAdPlayStart;
                client.onAdVideoEndEvent += onRewardedVideoAdPlayEnd;
                client.onAdVideoFailureEvent += onRewardedVideoAdPlayFail;
                client.onAdVideoCloseEvent += onRewardedVideoAdPlayClosed;
                client.onAdClickEvent += onRewardedVideoAdPlayClicked;
                client.onRewardEvent += onReward;
            }

            base.Init(null);
        }

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            return new TopOnAdStatusHandler(type, id);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            base.DownloadAd(ad);
            switch (ad._Type)
            {
                case AdsType.Banner:
                    {
                        Dictionary<string, object> jsonmap = new Dictionary<string, object>();
                        int bannerWidth = Mathf.Min(Screen.width, Screen.height);
                        int bannerHeight = Mathf.FloorToInt(bannerWidth * 50f / 320);

                        ATSize bannerSize = new ATSize(bannerWidth, bannerHeight, true);
                        jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraBannerAdSizeStruct, bannerSize);
                        jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraInlineAdaptiveWidth, bannerSize.width);
                        jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraInlineAdaptiveOrientation, ATBannerAdLoadingExtra.kATBannerAdLoadingExtraInlineAdaptiveOrientationCurrent);
                        ATBannerAd.Instance.loadBannerAd(ad._Id, jsonmap);
                    }
                    break;
                case AdsType.Interstitial:
                    {
                        Dictionary<string, object> jsonmap = new Dictionary<string, object>();
                        jsonmap.Add(AnyThinkAds.Api.ATConst.USE_REWARDED_VIDEO_AS_INTERSTITIAL, AnyThinkAds.Api.ATConst.USE_REWARDED_VIDEO_AS_INTERSTITIAL_NO);
                        ATInterstitialAd.Instance.loadInterstitialAd(ad._Id, jsonmap);
                    }
                    break;
                case AdsType.RewardedVideo:
                    {
                        Dictionary<string, string> jsonmap = new Dictionary<string, string>();
                        ATRewardedVideo.Instance.loadVideoAd(ad._Id, jsonmap);
                    }
                    break;
            }
        }

#region Banner
        string AdapterBannerPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                case BannerPosition.TopLeft:
                case BannerPosition.TopRight:
                    return ATBannerAdLoadingExtra.kATBannerAdShowingPisitionTop;
            }
            return ATBannerAdLoadingExtra.kATBannerAdShowingPisitionBottom;
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
            ATBannerAd.Instance.showBannerAd(mDefaultBannerAdList[0]._Id, AdapterBannerPosition(mBannerPosition));
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(true);
            }
            if (AdsManager.Debugging) Debug.Log("ShowAdsBanner " + mDefaultBannerAdList[0]._Id);
        }

        public override void HideAdsBanner()
        {
            bool is_shown = mBannerAdVisibility;
            base.HideAdsBanner();
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(false);
            }

            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            if (is_shown)
            {
                ATBannerAd.Instance.cleanBannerAd(mDefaultBannerAdList[0]._Id);
                //ATBannerAd.Instance.hideBannerAd(mDefaultBannerAdList[0]._Id);//Hide cause bug, need to check again in later version
                mDefaultBannerAdList[0].OnAdAvailabilityUpdate(false);
            }
            if (AdsManager.Debugging) Debug.Log("HideAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

        public void onAdAutoRefresh(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onAdAutoRefreshFail(object sender, ATAdErrorEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onAdClick(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Banner);
                if (AdsManager.Debugging) Debug.Log("HandleonBannerAdClickedEvent " + eventArgs.placementId);
            });
        }

        public void onAdClose(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onAdCloseButtonTapped(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onAdImpress(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onAdLoad(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                var currentDownload = GetCurrentDownload(AdsType.Banner);
                if (currentDownload != null && currentDownload._Id == eventArgs.placementId)
                {
                    AdDownloadCallback(AdsType.Banner, true, null);
                }
                else if (mAdDownloadHandler.ContainsKey(eventArgs.placementId))
                {
                    mAdDownloadHandler[eventArgs.placementId].OnAdAvailabilityUpdate(true);
                    ForceFreeCurrentDownload(AdsType.Banner);
                }

                if (mBannerAdVisibility)
                {
                    ShowAdsBanner();
                }
                else
                {
                    HideAdsBanner();
                }
                if (AdsManager.Debugging) Debug.Log("HandleonBannerAdLoadedEvent " + eventArgs.placementId);
            });
        }

        public void onAdLoadFail(object sender, ATAdErrorEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                var currentDownload = GetCurrentDownload(AdsType.Banner);
                if (currentDownload != null && currentDownload._Id == eventArgs.placementId)
                {
                    AdDownloadCallback(AdsType.Banner, false, eventArgs.errorMessage);
                }
                else if (mAdDownloadHandler.ContainsKey(eventArgs.placementId))
                {
                    mAdDownloadHandler[eventArgs.placementId].OnAdAvailabilityUpdate(false);
                    ForceFreeCurrentDownload(AdsType.Banner);
                }
                if (AdsManager.Debugging) Debug.Log("HandleonBannerAdLoadFailedEvent " + eventArgs.placementId + " code: " + eventArgs.errorCode + " message: " + eventArgs.errorMessage);
            });
        }
#endregion

#region Interstitial
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

            ATInterstitialAd.Instance.showInterstitialAd(mCurrentFullscreenAd._Id);
            if (AdsManager.Debugging) Debug.Log("ShowAdsInterstitial " + mCurrentFullscreenAd._Id);
            return true;
        }

        public void onInterstitialAdClick(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Interstitial);
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdClickedEvent " + eventArgs.placementId);
            });
        }

        public void onInterstitialAdClose(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
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
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdClosedEvent " + eventArgs.placementId);
            });
        }

        public void onInterstitialAdEndPlayingVideo(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onInterstitialAdFailedToPlayVideo(object sender, ATAdErrorEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                if (mInterstitialAdCallback != null)
                {
                    mInterstitialAdCallback(false);
                    mInterstitialAdCallback = null;
                }
                if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdShowFailedEvent " + eventArgs.placementId + " code: " + eventArgs.errorCode + " message: " + eventArgs.errorMessage);
            });
        }

        public void onInterstitialAdFailedToShow(object sender, ATAdErrorEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                if (mInterstitialAdCallback != null)
                {
                    mInterstitialAdCallback(false);
                    mInterstitialAdCallback = null;
                }
                if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdShowFailedEvent " + eventArgs.placementId + " code: " + eventArgs.errorCode + " message: " + eventArgs.errorMessage);
            });
        }

        public void onInterstitialAdLoad(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                var currentDownload = GetCurrentDownload(AdsType.Interstitial);
                if (currentDownload != null && currentDownload._Id == eventArgs.placementId)
                {
                    AdDownloadCallback(AdsType.Interstitial, true, null);
                }
                else if (mAdDownloadHandler.ContainsKey(eventArgs.placementId))
                {
                    mAdDownloadHandler[eventArgs.placementId].OnAdAvailabilityUpdate(true);
                    ForceFreeCurrentDownload(AdsType.Interstitial);
                }
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdReadyEvent " + eventArgs.placementId);
            });
        }

        public void onInterstitialAdLoadFail(object sender, ATAdErrorEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                var currentDownload = GetCurrentDownload(AdsType.Interstitial);
                if (currentDownload != null && currentDownload._Id == eventArgs.placementId)
                {
                    AdDownloadCallback(AdsType.Interstitial, false, eventArgs.errorMessage);
                }
                else if (mAdDownloadHandler.ContainsKey(eventArgs.placementId))
                {
                    mAdDownloadHandler[eventArgs.placementId].OnAdAvailabilityUpdate(false);
                    ForceFreeCurrentDownload(AdsType.Interstitial);
                }
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdReadyEvent " + eventArgs.placementId + " code: " + eventArgs.errorCode + " message: " + eventArgs.errorMessage);
            });
        }

        public void onInterstitialAdShow(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
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
                if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdShowSucceededEvent " + eventArgs.placementId);

                StartCoroutine(CRAdsShowTimeout());
            });
        }

        public void onInterstitialAdStartPlayingVideo(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
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
            onInterstitialAdFailedToShow(this, new ATAdErrorEventArgs(mCurrentFullscreenAd != null ? mCurrentFullscreenAd._Id : "", "-1", "timeout"));
        }
#endregion

#region Reward
        public override bool ShowAdsReward(System.Action<bool> callback, string adId = null)
        {
            if (!IsRewardAdAvailable(adId)) return false;
            base.ShowAdsReward(callback);
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

            ATRewardedVideo.Instance.showAd(mCurrentFullscreenAd._Id);
            return true;
        }

        public void onReward(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                mHaveRewarded = true;
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdRewardedEvent " + eventArgs.placementId);
            });
        }

        public void onRewardedVideoAdLoaded(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
                if (currentDownload != null && currentDownload._Id == eventArgs.placementId)
                {
                    AdDownloadCallback(AdsType.RewardedVideo, true, null);
                }
                else if (mAdDownloadHandler.ContainsKey(eventArgs.placementId))
                {
                    mAdDownloadHandler[eventArgs.placementId].OnAdAvailabilityUpdate(true);
                    ForceFreeCurrentDownload(AdsType.RewardedVideo);
                }
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdReadyEvent " + eventArgs.placementId);
            });
        }

        public void onRewardedVideoAdLoadFail(object sender, ATAdErrorEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
                if (currentDownload != null && currentDownload._Id == eventArgs.placementId)
                {
                    AdDownloadCallback(AdsType.RewardedVideo, false, eventArgs.errorMessage);
                }
                else if (mAdDownloadHandler.ContainsKey(eventArgs.placementId))
                {
                    mAdDownloadHandler[eventArgs.placementId].OnAdAvailabilityUpdate(false);
                    ForceFreeCurrentDownload(AdsType.RewardedVideo);
                }
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdLoadFailedEvent " + eventArgs.placementId + " code: " + eventArgs.errorCode + " message: " + eventArgs.errorMessage);
            });
        }

        public void onRewardedVideoAdPlayClicked(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.RewardedVideo);
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdClickedEvent " + eventArgs.placementId);
            });
        }

        IEnumerator mDelayRewardCheckThread;
        public void onRewardedVideoAdPlayClosed(object sender, ATAdRewardEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
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
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdClosedEvent " + eventArgs.placementId);
            });
        }

        public void onRewardedVideoAdPlayEnd(object sender, ATAdEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
        }

        public void onRewardedVideoAdPlayFail(object sender, ATAdErrorEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
                AudioListener.volume = 1;
                if (mRewardAdCallback != null)
                {
                    mRewardAdCallback(false);
                    mRewardAdCallback = null;
                }
                if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                mCurrentFullscreenAd = null;
                FullscreenAdShowing = false;
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdShowFailedEvent " + eventArgs.placementId + " code: " + eventArgs.errorCode + " message: " + eventArgs.errorMessage);
            });
        }

        public void onRewardedVideoAdPlayStart(object sender, ATAdEventArgs eventArgs)
        {
            UnityMainThreadDispatcher.instance.Enqueue(() => {
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
                if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdShowSucceededEvent " + eventArgs.placementId);

                StartCoroutine(CRAdsShowTimeout());
            });
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
            onRewardedVideoAdPlayFail(this, new ATAdErrorEventArgs(mCurrentFullscreenAd != null ? mCurrentFullscreenAd._Id : "", "-1", "timeout"));
        }
#endregion

        IEnumerator CRAdsShowTimeout()
        {
            string id = mCurrentFullscreenAd._Id;
            float timeout = 60;
            while (timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
                if (!FullscreenAdShowing)
                {
                    yield break;
                }
            }
            if (mCurrentFullscreenAd != null && mCurrentFullscreenAd._Id == id)
            {
                mCurrentFullscreenAd = null;
                FullscreenAdShowing = false;
                if (AdsManager.Debugging) Debug.Log("CRAdsShowTimeout cancel id " + id);
            }
        }

        public void startLoadingADSource(string placementId, ATCallbackInfo callbackInfo)
        {
            //throw new NotImplementedException();
        }

        public void finishLoadingADSource(string placementId, ATCallbackInfo callbackInfo)
        {
            //throw new NotImplementedException();
        }

        public void failToLoadADSource(string placementId, ATCallbackInfo callbackInfo, string code, string message)
        {
            //throw new NotImplementedException();
        }

        public void startBiddingADSource(string placementId, ATCallbackInfo callbackInfo)
        {
            //throw new NotImplementedException();
        }

        public void finishBiddingADSource(string placementId, ATCallbackInfo callbackInfo)
        {
            //throw new NotImplementedException();
        }

        public void failBiddingADSource(string placementId, ATCallbackInfo callbackInfo, string code, string message)
        {
            //throw new NotImplementedException();
        }
    }

    public class TopOnAdStatusHandler : AdStatusHandler
    {
        public TopOnAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return _Available;
        }

        protected override bool InterstitialAdAvailable()
        {
            return ATInterstitialAd.Instance.hasInterstitialAdReady(_Id);
        }

        protected override bool RewardAdAvailable()
        {
            return ATRewardedVideo.Instance.hasAdReady(_Id);
        }
    }
}
#endif