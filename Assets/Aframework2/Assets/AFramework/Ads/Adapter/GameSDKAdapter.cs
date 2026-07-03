#if USE_GAMESDK_ADS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.Ads
{
    using GameSdk.Scripts.Interface;
    using System;

    public class GameSDKAdapter : BaseAdsAdapter
    {
        public override void Init(object[] parameters)
        {
            mConfig = ((BaseAdapterConfig)parameters[0]);
            var cacheParameters = parameters;
            var initListener = new InitListener
            {
                Callback = (code, message) =>
                {
                    //Returns 200, indicating successful initialization.
                    if (code == 200)
                    {
                        //It is strongly recommended to preload interstitial and rewarded ads in the 
                        //callback of successful initialization.
                        Debug.Log("Initialization succeeded.");
                        base.Init(cacheParameters);
                    }
                }
            };
            //Add initialization listener
            AdHelper.InitListener(initListener);
            //Initialize SDK (the first parameter contains "test" and "release", the second parameter is the log switch)
            if (AdsManager.Debugging)
                AdHelper.InitSDK("test", true);
            else
                AdHelper.InitSDK("release", false);
        }

        protected override void UpdateDownloadList()
        {
            AdStatusHandler handler = null;
            if (AdsManager.I.IsAdEnabled(AdsType.RewardedVideo))
            {
                handler = CreateAdHandler(AdsType.RewardedVideo, "");
                mAdDownloadHandler.Add("rewardad", handler);
                mAdHighPriorityList.Add(handler);
                mDefaultRewardAdList.Add(handler);
            }
            //if (AdsManager.I.IsAdEnabled(AdsType.OfferWall))
            //{
            //    handler = CreateAdHandler(AdsType.OfferWall, "");
            //    mAdDownloadHandler.Add("offerwall", handler);
            //    mAdHighPriorityList.Add(handler);
            //    mDefaultOfferWallAdList.Add(handler);
            //}
            if (AdsManager.I.IsAdEnabled(AdsType.Interstitial))
            {
                handler = CreateAdHandler(AdsType.Interstitial, "");
                mAdDownloadHandler.Add("interstitialad" + 0, handler);
                mAdHighPriorityList.Add(handler);
                mDefaultInterstitialAdList.Add(handler);
            }
            if (AdsManager.I.IsAdEnabled(AdsType.Banner))
            {
                handler = CreateAdHandler(AdsType.Banner, "");
                mAdDownloadHandler.Add("bannerad" + 0, handler);
                mAdHighPriorityList.Add(handler);
                mDefaultBannerAdList.Add(handler);
            }

            //Tin: not support sdk 6.16.1, currently Ironsource Option/Demanded Ad is messup too much, callback is not consistency 
            //base.UpdateDownloadList();
        }

        protected override AdStatusHandler CreateAdHandler(AdsType type, string id)
        {
            return new GameSDKAdStatusHandler(type, id);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            if (AdsManager.Debugging) Debug.Log("DownloadAd " + ad._Type.ToString() + " id " + ad._Id);
            var adId = ad._Id;
            base.DownloadAd(ad);
            switch (ad._Type)
            {
                case AdsType.Interstitial:
                    {
                        if (AdHelper.IsInterstitialReady())
                        {
                            AdDownloadCallback(AdsType.Interstitial, true, null, null);
                        }
                        else
                        {
                            var listener = new GameAdLoadListener
                            {
                                OnAdLoaded = () =>
                                {
                                    //It is forbidden to directly call the display interface in the callback of successful loading. 
                                    //The logic of loading and display should be handled separately.
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
                                },

                                OnAdError = (i, s) =>
                                {
                                    //Interstitial ads have a retry mechanism inside, and after the internal retry is completed, 
                                    //a callback will be issued to indicate that the loading failed.
                                    //Do not directly initiate a retry request on this interface, 
                                    //otherwise it will cause many useless requests and may cause the application to freeze
                                    //If a retry is required, it is recommended to choose another appropriate time, 
                                    //or design a limit on the number of retries.
                                    var currentDownload = GetCurrentDownload(AdsType.Interstitial);
                                    if (currentDownload != null && currentDownload._Id == adId)
                                    {
                                        AdDownloadCallback(AdsType.Interstitial, false, i.ToString(), s);
                                    }
                                    else if (mAdDownloadHandler.ContainsKey(adId))
                                    {
                                        mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                                        ForceFreeCurrentDownload(AdsType.Interstitial);
                                    }
                                }
                            };
                            AdHelper.LoadInterstitial(listener);
                        }
                    }
                    break;
                case AdsType.RewardedVideo:
                    {
                        if (AdHelper.IsRewardReady())
                        {
                            AdDownloadCallback(AdsType.RewardedVideo, true, null, null);
                        }
                        else
                        {
                            var listener = new GameAdLoadListener
                            {
                                OnAdLoaded = () =>
                                {
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
                                },
                                OnAdError = (i, s) =>
                                {
                                    var currentDownload = GetCurrentDownload(AdsType.RewardedVideo);
                                    if (currentDownload != null && currentDownload._Id == adId)
                                    {
                                        AdDownloadCallback(AdsType.RewardedVideo, false, i.ToString(), s);
                                    }
                                    else if (mAdDownloadHandler.ContainsKey(adId))
                                    {
                                        mAdDownloadHandler[adId].OnAdAvailabilityUpdate(false);
                                        ForceFreeCurrentDownload(AdsType.RewardedVideo);
                                    }
                                }
                            };
                            AdHelper.LoadReward(listener);
                        }
                    }
                    break;
            }
        }

        #region Interstitial
        public override bool ShowAdsInterstitial(Action<bool> callback, string adId = null)
        {
            if (!IsInterstitialAdAvailable(adId)) return false;
            if (!AdHelper.IsInterstitialReady()) return false;
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

            var listener = new GameAdShowListener
            {
                OnShow = () =>
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
                },
                OnClick = () =>
                {
                    if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialClickedEvent");
                    AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.Interstitial);
                },
                OnImpression = () =>
                {
                    //Debug.Log("The interstitial ad impression.");
                },
                OnClose = () =>
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
                },
                OnShowError = (i, s) =>
                {
                    if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialAdFailedToDisplayEvent " + adId + " code: " + i);
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
                    AdDisplayResultCallback(AdsType.Interstitial, false, i.ToString(), s);
                }
            };

            AdHelper.ShowInterstitial(listener);
            if (AdsManager.Debugging) Debug.Log("ShowAdsInterstitial " + mCurrentFullscreenAd._Id);
            return true;
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

            if (AdsManager.Debugging) Debug.Log("HandleOnInterstitialAdFailedToDisplayEvent " + (mCurrentFullscreenAd != null ? mCurrentFullscreenAd._Id : "") + " timeout");
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
            AdDisplayResultCallback(AdsType.Interstitial, false, "-1", null);
        }
        #endregion Interstitial

        #region RewardedVideo
        bool mHaveRewarded = false;

        public override bool ShowAdsReward(Action<bool> callback, string adId = null)
        {
            if (!IsRewardAdAvailable(adId)) return false;
            if (!AdHelper.IsRewardReady()) return false;
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

            var listener = new GameAdRewardShowListener
            {
                OnClick = () => {
                    if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdClickedEvent " + adId);
                    AFramework.Analytics.TrackingManager.I.TrackAdsClick(AdsType.RewardedVideo);
                },
                OnClose = () => {
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
                },
                OnImpression = () => {
                    //AddLog("The reward ad impression.")
                },
                OnShow = () => {
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
                },
                OnShowError = (i, s) => {
                    if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdFailedToDisplayEvent " + adId + " code " + i);
                    AudioListener.volume = 1;
                    if (mRewardAdCallback != null)
                    {
                        mRewardAdCallback(false);
                        mRewardAdCallback = null;
                    }
                    if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
                    mCurrentFullscreenAd = null;
                    FullscreenAdShowing = false;
                    AdDisplayResultCallback(AdsType.RewardedVideo, false, i.ToString(), s);
                },
                OnUserEarnedReward = () => {
                    if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdReceivedRewardEvent " + adId);
                    mHaveRewarded = true;
                },
            };
            AdHelper.ShowReward(listener);
            return true;
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

            if (AdsManager.Debugging) Debug.Log("HandleOnRewardedAdFailedToDisplayEvent " + (mCurrentFullscreenAd != null ? mCurrentFullscreenAd._Id : "") + " timeout");
            AudioListener.volume = 1;
            if (mRewardAdCallback != null)
            {
                mRewardAdCallback(false);
                mRewardAdCallback = null;
            }
            if (mCurrentFullscreenAd != null) mCurrentFullscreenAd.OnAdAvailabilityUpdate(false);
            mCurrentFullscreenAd = null;
            FullscreenAdShowing = false;
            AdDisplayResultCallback(AdsType.RewardedVideo, false, "-1", null);
        }
        #endregion RewardedVideo
    }

    public class GameSDKAdStatusHandler : AdStatusHandler
    {
        public GameSDKAdStatusHandler(AdsType type, string id) : base(type, id) { }

        protected override bool BannerAdAvailable()
        {
            return _Available;
        }

        protected override bool InterstitialAdAvailable()
        {
            return AdHelper.IsInterstitialReady();
        }

        protected override bool RewardAdAvailable()
        {
            return AdHelper.IsRewardReady();
        }

        protected override bool AppOpenAdAvailable()
        {
            return AdHelper.IsOpenAdReady();
        }
    }
}
#endif