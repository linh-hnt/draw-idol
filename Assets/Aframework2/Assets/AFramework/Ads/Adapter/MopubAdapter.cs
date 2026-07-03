#if USE_MOPUB_ADS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace AFramework.Ads
{
    public class MopubAdapter : BaseAdsAdapter
    {
        protected MopubAdapterConfig Config { get; set; }
        bool mHaveRewarded = false;

        // API to make calls to the platform-specific MoPub SDK.
        internal static MoPubPlatformApi MoPubPlatformApi { get; private set; }

        public override void Init(object[] parameters)
        {
            var backupParams = parameters;
            mConfig = ((BaseAdapterConfig)parameters[0]);
            Config = mConfig as MopubAdapterConfig;

            string anyAdUnitId = Config.Platform.RewardedVideoId;
            if (string.IsNullOrEmpty(anyAdUnitId)) anyAdUnitId = Config.Platform.InterstitialId;
            else if (string.IsNullOrEmpty(anyAdUnitId)) anyAdUnitId = Config.Platform.BannerlId;
            else if (string.IsNullOrEmpty(anyAdUnitId)) Debug.LogError("MopubAdapter no available AdUnitId");

            //attach event
            {
                TODO AdDisplayResultCallback
                //banner
                if (AdsManager.I.IsAdEnabled(AdsType.Banner))
                {
                    MoPubManager.OnAdLoadedEvent += HandleonBannerAdLoadedEvent;
                    MoPubManager.OnAdFailedEvent += HandleonBannerAdLoadFailedEvent;
                    MoPubManager.OnAdClickedEvent += HandleonBannerAdClickedEvent;
                }

                //Interstitial
                if (AdsManager.I.IsAdEnabled(AdsType.Interstitial))
                {
                    MoPubManager.OnInterstitialLoadedEvent += HandleonInterstitialAdReadyEvent;
                    MoPubManager.OnInterstitialFailedEvent += HandleonInterstitialAdLoadFailedEvent;
                    MoPubManager.OnInterstitialShownEvent += HandleonInterstitialAdShowSucceededEvent;
                    MoPubManager.OnInterstitialDismissedEvent += HandleonInterstitialAdClosedEvent;
                    MoPubManager.OnInterstitialExpiredEvent += HandleonInterstitialAdExpiredEvent;
                    MoPubManager.OnInterstitialClickedEvent += HandleonInterstitialAdClickedEvent;
                }

                //Reward
                if (AdsManager.I.IsAdEnabled(AdsType.RewardedVideo))
                {
                    MoPubManager.OnRewardedVideoLoadedEvent += HandleonRewardedVideoAdReadyEvent;
                    MoPubManager.OnRewardedVideoFailedEvent += HandleonRewardedVideoAdLoadFailedEvent;
                    MoPubManager.OnRewardedVideoShownEvent += HandleonRewardedVideoAdShowSucceededEvent;
                    MoPubManager.OnRewardedVideoFailedToPlayEvent += HandleonRewardedVideoAdShowFailedEvent;
                    MoPubManager.OnRewardedVideoReceivedRewardEvent += HandleonRewardedVideoAdRewardedEvent;
                    MoPubManager.OnRewardedVideoClosedEvent += HandleonRewardedVideoAdClosedEvent;
                    MoPubManager.OnRewardedVideoExpiredEvent += HandleonRewardedVideoAdExpiredEvent;
                    MoPubManager.OnRewardedVideoClickedEvent += HandleonRewardedVideoAdClickedEvent;
                }
            }

            var adapterObj = Instantiate(Config.MopubAdapterObj);
            adapterObj.name = "MoPubManager";//must have correct name or it wont run on device X_X
            adapterObj.transform.SetParent(AdsManager.I.transform);
            var mopubManager = adapterObj.GetComponent<MoPubManager>();          
            mopubManager.AdUnitId = anyAdUnitId;
            mopubManager.LocationAware = true;
            mopubManager.AllowLegitimateInterest = false;
            mopubManager.LogLevel = AdsManager.Debugging ? MoPub.LogLevel.Debug : MoPub.LogLevel.None;
#if UNITY_IOS
            mopubManager.itunesAppId = AFramework.Analytics.TrackingManager.IsInstanceValid() ? AFramework.Analytics.TrackingManager.I.AppflyerAppId : "";
#endif
            var initCallback = (UnityEngine.Events.UnityAction<string>)((adunitid) =>
            {
                if (mInited) return;
                base.Init(backupParams);

                List<string> _bannerAdUnits = new List<string>();
                List<string> _interstitialAdUnits = new List<string>();
                List<string> _rewardedAdUnits = new List<string>();

                foreach(var pair in mAdDownloadHandler)
                {
                    if (pair.Value._Type == AdsType.RewardedVideo) _rewardedAdUnits.Add(pair.Value._Id);
                    else if (pair.Value._Type == AdsType.Interstitial) _interstitialAdUnits.Add(pair.Value._Id);
                    else if (pair.Value._Type == AdsType.Banner) _bannerAdUnits.Add(pair.Value._Id);
                }

                if (_bannerAdUnits.Count > 0) MoPub.LoadBannerPluginsForAdUnits(_bannerAdUnits.ToArray());
                if (_interstitialAdUnits.Count > 0) MoPub.LoadInterstitialPluginsForAdUnits(_interstitialAdUnits.ToArray());
                if (_rewardedAdUnits.Count > 0) MoPub.LoadRewardedVideoPluginsForAdUnits(_rewardedAdUnits.ToArray());

                if (AdsManager.Debugging) Debug.Log("MopubAdapter init success");
            });
            mopubManager.Initialized.AddListener(initCallback);
            mopubManager.AutoInitializeOnStart = true;
        }

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            return new MopubAdStatusHandler(type, id);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            base.DownloadAd(ad);
            switch (ad._Type)
            {
                case AdsType.Banner:
                    MoPub.RequestBanner(ad._Id, AdapterBannerPosition(mBannerPosition));
                    break;
                case AdsType.Interstitial:
                    MoPub.RequestInterstitialAd(ad._Id);
                    break;
                case AdsType.RewardedVideo:
                    MoPub.RequestRewardedVideo(ad._Id);
                    break;
            }
        }

#region Banner
        MoPub.AdPosition AdapterBannerPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top:
                    return MoPub.AdPosition.TopCenter;
                case BannerPosition.Bottom:
                    return MoPub.AdPosition.BottomCenter;
                case BannerPosition.TopLeft:
                    return MoPub.AdPosition.TopLeft;
                case BannerPosition.TopRight:
                    return MoPub.AdPosition.TopRight;
                case BannerPosition.BottomLeft:
                    return MoPub.AdPosition.BottomLeft;
                case BannerPosition.BottomRight:
                    return MoPub.AdPosition.BottomRight;
                case BannerPosition.Center:
                    return MoPub.AdPosition.Centered;
            }
            return MoPub.AdPosition.BottomCenter;
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
            MoPub.ShowBanner(mDefaultBannerAdList[0]._Id, true);
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(true);
            }
            if (AdsManager.Debugging) Debug.Log("ShowAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

        public override void HideAdsBanner()
        {
            base.HideAdsBanner();
            if (AdsManager.EventOnBannerAdsChanged != null)
            {
                AdsManager.EventOnBannerAdsChanged(false);
            }

            if (mDefaultBannerAdList.Count <= 0 || !mDefaultBannerAdList[0]._Available) return;
            MoPub.ShowBanner(mDefaultBannerAdList[0]._Id, false);
            if (AdsManager.Debugging) Debug.Log("HideAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

        void HandleonBannerAdLoadedEvent(string adUnitId, float height)
        {
            if (mCurrentDownload != null && mCurrentDownload._Id == adUnitId)
            {
                AdDownloadCallback(AdsType.Banner, true, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(true);
            }

            if (mBannerAdVisibility)
            {
                ShowAdsBanner();
            }
            else
            {
                HideAdsBanner();
            }
            MoPub.SetAutorefresh(adUnitId, true);
            if (AdsManager.Debugging) Debug.Log("HandleonBannerAdLoadedEvent " + adUnitId);
        }

        void HandleonBannerAdLoadFailedEvent(string adUnitId, string error)
        {
            if (mCurrentDownload != null && mCurrentDownload._Id == adUnitId)
            {
                AdDownloadCallback(AdsType.Banner, false, error);
            }
            else if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(false);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonBannerAdLoadFailedEvent " + adUnitId + " error " + error);
        }

        void HandleonBannerAdClickedEvent(string adUnitId)
        {
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Banner);
            if (AdsManager.Debugging) Debug.Log("HandleonBannerAdClickedEvent " + adUnitId);
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

            MoPub.ShowInterstitialAd(mCurrentFullscreenAd._Id);
            if (AdsManager.Debugging) Debug.Log("ShowAdsInterstitial " + mCurrentFullscreenAd._Id);
            return true;
        }

        void HandleonInterstitialAdReadyEvent(string adUnitId)
        {
            if (mCurrentDownload != null && mCurrentDownload._Id == adUnitId)
            {
                AdDownloadCallback(AdsType.Interstitial, true, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(true);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdReadyEvent " + adUnitId);
        }

        void HandleonInterstitialAdLoadFailedEvent(string adUnitId, string error)
        {
            if (mCurrentDownload != null && mCurrentDownload._Id == adUnitId)
            {
                AdDownloadCallback(AdsType.Interstitial, false, error);
            }
            else if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(false);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdReadyEvent " + adUnitId + " error " + error);
        }

        void HandleonInterstitialAdShowSucceededEvent(string adUnitId)
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
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdShowSucceededEvent " + adUnitId);
        }

        void HandleonInterstitialAdShowFailedEvent(string error)
        {
            if (mInterstitialAdCallback != null)
            {
                mInterstitialAdCallback(false);
                mInterstitialAdCallback = null;
            }
            if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
            mCurrentFullscreenAd = null;
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdShowFailedEvent " + error);
        }

        void HandleonInterstitialAdClosedEvent(string adUnitId)
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
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdClosedEvent " + adUnitId);
        }

        void HandleonInterstitialAdExpiredEvent(string adUnitId)
        {
            if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(false);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdExpiredEvent " + adUnitId);
        }

        void HandleonInterstitialAdClickedEvent(string adUnitId)
        {
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Interstitial);
            if (AdsManager.Debugging) Debug.Log("HandleonInterstitialAdClickedEvent " + adUnitId);
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
            HandleonInterstitialAdShowFailedEvent("timeout");
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

            if (mAdsRewardTimeoutThread != null)
            {
                StopCoroutine(mAdsRewardTimeoutThread);
                mAdsRewardTimeoutThread = null;
            }
            mAdsRewardTimeoutThread = CRAdsRewardTimeoutThread(mCurrentFullscreenAd._Id);
            StartCoroutine(mAdsRewardTimeoutThread);

            MoPub.ShowRewardedVideo(mCurrentFullscreenAd._Id);
            return true;
        }

        void HandleonRewardedVideoAdReadyEvent(string adUnitId)
        {
            if (mCurrentDownload != null && mCurrentDownload._Id == adUnitId)
            {
                AdDownloadCallback(AdsType.RewardedVideo, true, null);
            }
            else if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(true);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdReadyEvent " + adUnitId);
        }

        void HandleonRewardedVideoAdLoadFailedEvent(string adUnitId, string error)
        {
            if (mCurrentDownload != null && mCurrentDownload._Id == adUnitId)
            {
                AdDownloadCallback(AdsType.RewardedVideo, false, error);
            }
            else if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(false);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdLoadFailedEvent " + adUnitId + " error " + error);
        }

        void HandleonRewardedVideoAdShowSucceededEvent(string adUnitId)
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
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdShowSucceededEvent " + adUnitId);
        }

        void HandleonRewardedVideoAdShowFailedEvent(string adUnitId, string error)
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
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdShowFailedEvent " + adUnitId + " error " + error);
        }

        void HandleonRewardedVideoAdRewardedEvent(string adUnitId, string label, float amount)
        {
            mHaveRewarded = true;
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdRewardedEvent " + adUnitId);
        }

        IEnumerator mDelayRewardCheckThread;
        void HandleonRewardedVideoAdClosedEvent(string adUnitId)
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
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdClosedEvent " + adUnitId);
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

        void HandleonRewardedVideoAdExpiredEvent(string adUnitId)
        {
            if (mAdDownloadHandler.ContainsKey(adUnitId))
            {
                mAdDownloadHandler[adUnitId].OnAdAvailabilityUpdate(false);
            }
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdExpiredEvent " + adUnitId);
        }

        void HandleonRewardedVideoAdClickedEvent(string adUnitId)
        {
            AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.RewardedVideo);
            if (AdsManager.Debugging) Debug.Log("HandleonRewardedVideoAdClickedEvent " + adUnitId);
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
            HandleonRewardedVideoAdShowFailedEvent(cacheId, "timeout");
        }
#endregion
    }

    public class MopubAdStatusHandler : AdStatusHandler
    {
        public MopubAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return _Available;
        }

        protected override bool InterstitialAdAvailable()
        {
            return MoPub.IsInterstitialReady(_Id);
        }

        protected override bool RewardAdAvailable()
        {
            return MoPub.HasRewardedVideo(_Id);
        }
    }
}
#endif