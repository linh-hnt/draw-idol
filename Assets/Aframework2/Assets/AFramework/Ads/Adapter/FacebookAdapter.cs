#if USE_FB_ADS

using System;
using AudienceNetwork;
using UnityEngine;

namespace AFramework.Ads
{
    public class FacebookAdapter : BaseAdsAdapter
    {
        private BaseAdapterConfig Config { get; set; }
        private bool isInterstitialAdLoaded;
        private bool isRewardAdLoaded;
        private bool isBannerAdLoaded;
        private bool didClose;

        private AdView adView;
        private AdPosition currentAdViewPosition;
        private ScreenOrientation currentScreenOrientation;
        private AdSize[] adSizeArray = (AdSize[]) Enum.GetValues(typeof(AdSize));
        private int currentAdSize;


        private InterstitialAd interstitialAd;
        private RewardedVideoAd rewardedVideoAd;

        public override void Init(object[] parameters)
        {
            mConfig = (BaseAdapterConfig) parameters[0];
            Config = mConfig;
            AudienceNetworkAds.Initialize();
            base.Init(parameters);
        }

        protected override void DownloadAd(AdStatusHandler ad)
        {
            TODO AdDisplayResultCallback
            if (ad._Type == AdsType.Banner)
            {
                LoadBanner();
            }
            else if (ad._Type == AdsType.Interstitial)
            {
                LoadInterstitial();
            }
            else if (ad._Type == AdsType.RewardedVideo)
            {
                LoadRewardedVideo();
            }

            base.DownloadAd(ad);
        }

#region BANNER ADS

        private void LoadBanner()
        {
            if (adView)
            {
                adView.Dispose();
            }

            // Create a banner's ad view with a unique placement ID
            // (generate your own on the Facebook app settings).
            // Use different ID for each ad placement in your app.
            adView = new AdView(Config.Platform.BannerlId, adSizeArray[currentAdSize]);

            adView.Register(gameObject);

            // Set delegates to get notified on changes or when the user interacts
            // with the ad.
            adView.AdViewDidLoad = delegate()
            {
                currentScreenOrientation = Screen.orientation;
                isBannerAdLoaded = true;
                SetAdViewPosition(currentAdViewPosition);
                string isAdValid = adView.IsValid() ? "valid" : "invalid";
                if (AdsManager.Debugging) Debug.Log("Banner loaded");
            };
            adView.AdViewDidFailWithError = delegate(string error)
            {
                if (AdsManager.Debugging) Debug.Log("Banner failed to load with error: " + error);
            };
            adView.AdViewWillLogImpression = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Banner logged impression.");
            };
            adView.AdViewDidClick = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Banner clicked.");
            };

            // Initiate a request to load an ad.
            adView.LoadAd();
        }

        private void SetAdViewPosition(AdPosition adPosition)
        {
            switch (adPosition)
            {
                case AdPosition.TOP:
                    adView.Show(AdPosition.TOP);
                    currentAdViewPosition = AdPosition.TOP;
                    break;
                case AdPosition.BOTTOM:
                    adView.Show(AdPosition.BOTTOM);
                    currentAdViewPosition = AdPosition.BOTTOM;
                    break;
                case AdPosition.CUSTOM:
                    adView.Show(100);
                    currentAdViewPosition = AdPosition.CUSTOM;
                    break;
            }
        }

        public override void SetBannerPosition(BannerPosition position)
        {
            if (position == BannerPosition.Bottom)
                currentAdViewPosition = AdPosition.BOTTOM;
            else if (position == BannerPosition.Top)
                currentAdViewPosition = AdPosition.TOP;
            base.SetBannerPosition(position);
        }

        public override void ShowAdsBanner()
        {
            base.ShowAdsBanner();
            if (adView != null && isBannerAdLoaded)
                adView.Show(currentAdViewPosition);

            if (AdsManager.EventOnBannerAdsChanged != null)
                AdsManager.EventOnBannerAdsChanged(true);
            if (AdsManager.Debugging) Debug.Log("ShowAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

        public override void HideAdsBanner()
        {
            base.HideAdsBanner();
            if (AdsManager.EventOnBannerAdsChanged != null)
                AdsManager.EventOnBannerAdsChanged(false);
            if (adView != null && isBannerAdLoaded)
            {
                isBannerAdLoaded = false;
                adView.Dispose();
                adView = null;
            }

            if (AdsManager.Debugging) Debug.Log("HideAdsBanner" + mDefaultBannerAdList[0]._Id);
        }

#endregion

#region INTERSTITIAL ADS

        private void LoadInterstitial()
        {
            // Create the interstitial unit with a placement ID (generate your own on the Facebook app settings).
            // Use different ID for each ad placement in your app.
            interstitialAd = new InterstitialAd(Config.Platform.InterstitialId);

            interstitialAd.Register(gameObject);

            // Set delegates to get notified on changes or when the user interacts with the ad.
            interstitialAd.InterstitialAdDidLoad = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Interstitial ad loaded.");
                isInterstitialAdLoaded = true;
                didClose = false;
                string isAdValid = interstitialAd.IsValid() ? "valid" : "invalid";
            };
            interstitialAd.InterstitialAdDidFailWithError = delegate(string error)
            {
                if (AdsManager.Debugging) Debug.Log("Interstitial ad failed to load with error: " + error);
            };
            interstitialAd.InterstitialAdWillLogImpression = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Interstitial ad logged impression.");
            };
            interstitialAd.InterstitialAdDidClick = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Interstitial ad clicked.");
            };
            interstitialAd.InterstitialAdDidClose = delegate()
            {
                FullscreenAdShowing = false;
                if (AdsManager.Debugging) Debug.Log("Interstitial ad did close.");
                didClose = true;
                interstitialAd?.Dispose();
            };

#if UNITY_ANDROID
        /*
         * Only relevant to Android.
         * This callback will only be triggered if the Interstitial activity has
         * been destroyed without being properly closed. This can happen if an
         * app with launchMode:singleTask (such as a Unity game) goes to
         * background and is then relaunched by tapping the icon.
         */
        interstitialAd.interstitialAdActivityDestroyed = delegate() {
            if (!didClose) {
                Debug.Log("Interstitial activity destroyed without being closed first.");
                Debug.Log("Game should resume.");
            }
        };
#endif

            // Initiate the request to load the ad.
            interstitialAd.LoadAd();
        }

        public override bool ShowAdsInterstitial(Action<bool> callback, string adId = null)
        {
            if (!IsInterstitialAdAvailable(adId)) return false;
            FullscreenAdShowing = true;
            isInterstitialAdLoaded = false;
            interstitialAd.Show();
            return true;
        }


        public override bool IsInterstitialAdAvailable(string adId = null)
        {
            return isInterstitialAdLoaded;
        }

#endregion

#region REWARD ADS

        public void LoadRewardedVideo()
        {
            // Create the rewarded video unit with a placement ID (generate your own on the Facebook app settings).
            // Use different ID for each ad placement in your app.
            rewardedVideoAd = new RewardedVideoAd(Config.Platform.RewardedVideoId);

            // For S2S validation you can create the rewarded video ad with the reward data
            // Refer to documentation here:
            // https://developers.facebook.com/docs/audience-network/android/rewarded-video#server-side-reward-validation
            // https://developers.facebook.com/docs/audience-network/ios/rewarded-video#server-side-reward-validation
            RewardData rewardData = new RewardData
            {
                UserId = "USER_ID",
                Currency = "REWARD_ID"
            };
#pragma warning disable 0219
            RewardedVideoAd s2sRewardedVideoAd = new RewardedVideoAd("TEST_AD_TYPE#YOUR_PLACEMENT_ID", rewardData);
#pragma warning restore 0219

            rewardedVideoAd.Register(gameObject);

            // Set delegates to get notified on changes or when the user interacts with the ad.
            rewardedVideoAd.RewardedVideoAdDidLoad = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("RewardedVideo ad loaded.");
                isRewardAdLoaded = true;
                didClose = false;
                string isAdValid = rewardedVideoAd.IsValid() ? "valid" : "invalid";
            };
            rewardedVideoAd.RewardedVideoAdDidFailWithError = delegate(string error)
            {
                if (AdsManager.Debugging) Debug.Log("RewardedVideo ad failed to load with error: " + error);
            };
            rewardedVideoAd.RewardedVideoAdWillLogImpression = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("RewardedVideo ad logged impression.");
            };
            rewardedVideoAd.RewardedVideoAdDidClick = delegate() { Debug.Log("RewardedVideo ad clicked."); };

            // For S2S validation you need to register the following two callback
            // Refer to documentation here:
            // https://developers.facebook.com/docs/audience-network/android/rewarded-video#server-side-reward-validation
            // https://developers.facebook.com/docs/audience-network/ios/rewarded-video#server-side-reward-validation
            rewardedVideoAd.RewardedVideoAdDidSucceed = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Rewarded video ad validated by server");
            };

            rewardedVideoAd.RewardedVideoAdDidFail = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Rewarded video ad not validated, or no response from server");
            };

            rewardedVideoAd.RewardedVideoAdDidClose = delegate()
            {
                if (AdsManager.Debugging) Debug.Log("Rewarded video ad did close.");
                didClose = true;
                if (rewardedVideoAd != null)
                {
                    rewardedVideoAd.Dispose();
                }
            };

#if UNITY_ANDROID
        /*
         * Only relevant to Android.
         * This callback will only be triggered if the Rewarded Video activity
         * has been destroyed without being properly closed. This can happen if
         * an app with launchMode:singleTask (such as a Unity game) goes to
         * background and is then relaunched by tapping the icon.
         */
        rewardedVideoAd.RewardedVideoAdActivityDestroyed = delegate ()
        {
            if (!didClose)
            {
                Debug.Log("Rewarded video activity destroyed without being closed first.");
                Debug.Log("Game should resume. User should not get a reward.");
            }
        };
#endif

            // Initiate the request to load the ad.
            rewardedVideoAd.LoadAd();
        }

        public override bool ShowAdsReward(Action<bool> callback, string adId = null)
        {
            if (!IsRewardAdAvailable(adId)) return false;
            isRewardAdLoaded = false;
            rewardedVideoAd.Show();
            return true;
        }

        public override bool IsRewardAdAvailable(string adId = null)
        {
            return isRewardAdLoaded;
        }

#endregion
    }
}
#endif