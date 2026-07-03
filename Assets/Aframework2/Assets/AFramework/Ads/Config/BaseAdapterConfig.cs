using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.Ads
{
    [CreateAssetMenu(menuName = "ScriptableObject/AFramework/Ads/BaseAdapterConfig")]
    public class BaseAdapterConfig : ScriptableObject
    {
        [SerializeField] protected float AdsErrorRetryInterval = 3f;
        [SerializeField] protected float AdsDownloadTimeout = 180f;

        [System.Serializable]
        public class PlatformConfig
        {
            public bool LoadMultipleAds = true;
            public string AppId;
            public string BannerlId;
            public string InterstitialId;
            public string RewardedVideoId;
            public string InterstitialRewardedId;
            public string OfferWallId;
            public string AppOpenId;
            [NamedArrayAttribute(typeof(AdsType))]
            public string[] placementConfig = new string[(int)AdsType.NUM];

            [Header("Per Mediation")]
            public string IronsourceId;
            //public string ApplovingSDKKey;
            public string TopOnAppId;
            public string TopOnAppKey;

            public AdIdConfig AmazonConfig;
        }

        [System.Serializable]
        public class AdIdConfig
        {
            public string appId;
            public string banner;
            public string interstitial;
            public string reward;
        }

        [SerializeField] protected PlatformConfig Android;
        [SerializeField] protected PlatformConfig iOS;
        [SerializeField] protected PlatformConfig instanceGame;

        public PlatformConfig Platform
        {
            get
            {
#if UNITY_IOS
            return iOS;
#elif UNITY_ANDROID
            return Android;
#elif UNITY_WEBGL
            return instanceGame;
#else
            return null;
#endif
            }
        }

        public float GetErrorRetryInterval()
        {
            return AdsErrorRetryInterval;
        }

        public float GetDownloadTimeout()
        {
            return AdsDownloadTimeout;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var allConfig = new PlatformConfig[] { Android, iOS, instanceGame };
            for (int i = 0; i < allConfig.Length; ++i)
            {
                var config = allConfig[i];
                if (config.placementConfig.Length != (int)AdsType.NUM)
                {
                    config.placementConfig = AFramework.Utility.ResizeArray<string>(config.placementConfig, (int)AdsType.NUM, string.Empty);
                }
            }
        }
#endif
    }
}