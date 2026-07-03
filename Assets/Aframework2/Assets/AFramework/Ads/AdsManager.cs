using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework;
using System;
using AdsConfig = AFramework.Ads.BaseAdapterConfig;
using AFramework.Analytics;

#if USE_ADMOB
using AdsAdapter = AFramework.Ads.AdmobAdapter;
#elif USE_IRONSOURCE_ADS
using AdsAdapter = AFramework.Ads.IronSourceAdapter;
#elif USE_APPLOVIN_ADS
using AdsAdapter = AFramework.Ads.AppLovinAdapter;
#elif USE_MOPUB_ADS
using AdsAdapter = AFramework.Ads.MopubAdapter;
#elif USE_FB_ADS
using AdsAdapter = AFramework.Ads.FacebookAdapter;
#elif USE_TOPON_ADS
using AdsAdapter = AFramework.Ads.TopOnAdapter;
#elif USE_FB_ADS
using AdsAdapter = AFramework.Ads.FacebookAdapter;
#elif USE_UNITY_ADS
using AdsAdapter = AFramework.Ads.UnityAdapter;
#elif USE_GAMESDK_ADS
using AdsAdapter = AFramework.Ads.GameSDKAdapter;
#else
using AdsAdapter = AFramework.Ads.BaseAdsAdapter;
#endif

namespace AFramework.Ads
{
    public enum AdsLoadState
    {
        Idle,
        Downloading,
        Loaded,
        Error,
    }

    public enum BannerPosition
    {
        Top = 0,
        Bottom = 1,
        TopLeft = 2,
        TopRight = 3,
        BottomLeft = 4,
        BottomRight = 5,
        Center = 6,
        CenterLeft = 7,
        CenterRight = 8,
    }

    public enum AdsVideoEndError
    {
        UserCancel = 0,
        None,
    }

    public class AdsManager : SingletonMono<AdsManager>, ISaveData
    {
#if DEVELOPMENT_BUILD || USE_CHEAT || UNITY_EDITOR
#if SKIP_ADS
        public static bool CheatSkipAds = true;
#else
        public static bool CheatSkipAds = false;
#endif
#else
        public const bool CheatSkipAds = false;
#endif
#if ADS_DEBUGGING
        public const bool Debugging = true;
#else
        public const bool Debugging = false;
#endif
        public static System.Action<bool> EventOnBannerAdsChanged;
        public static System.Action<bool> EventOnInterstitialAdsChanged;
        public static System.Action<bool> EventOnRewardAdsChanged;
        public static System.Action<bool> EventOnOfferWallChanged;
        public static System.Action EventOnFullScreenAdsShown;

        [SerializeField] protected FrameworkGlobalConfig _Config;
        [SerializeField] protected bool AutoInit = true;
        [SerializeField] protected bool AutoStartAdThread = true;


        [NamedArrayAttribute(typeof(AdsType))]
        public bool[] SupportAdsType = new bool[(int)AdsType.NUM] { true, true, true, false, false, false };//BANNER, INTERSTITIAL, REWARD, INTERSTITIAL_REWARD, OFFER_WALL
        public string[] OfferwallCreditNames = new string[] { "Credits" };

        [SerializeField] BannerPosition _DefaultBannerPosition = BannerPosition.Bottom;
        [SerializeField] bool _ShowSimpleLoadingScreen = false;
        [SerializeField] bool _UseInterstitialForRewardAd = false;

        protected AdsAdapter _Adapter;
        protected bool mInited = false;

        protected Dictionary<string, string> mInterstitialAdPlacementConfig = new Dictionary<string, string>();
        protected Dictionary<string, string> mRewardAdPlacementConfig = new Dictionary<string, string>();
        protected Dictionary<string, string> mInterstitialRewardAdPlacementConfig = new Dictionary<string, string>();
        protected Dictionary<string, string> mOfferWallPlacementConfig = new Dictionary<string, string>();
        protected Dictionary<string, string> mAppOpenPlacementConfig = new Dictionary<string, string>();
        Dictionary<string, object>[] mLastAdsShowData = new Dictionary<string, object>[(int)AdsType.NUM];

        private WaitForSeconds autoCheckOfferwallRewardDelay = new WaitForSeconds(1f);

        private int showIntertisalNumber = 0;

        public int ShowIntertisalNumber { get { return showIntertisalNumber; } }

        public virtual AdsConfig CurrentConfig
        {
            get { return _Config.AdsConfig; }
        }
        public BannerPosition DefaultBannerPosition { get { return _DefaultBannerPosition; } }
        public bool IsAdapterInited() { return mInited && _Adapter != null && _Adapter.IsInited; }
        public bool IsATTAsked { get; protected set; }

        public bool IsAutoInit => AutoInit;

        protected virtual void Start()
        {
#if UNITY_ANDROID
            _Config?.CheckTester();
#endif
            if (AutoInit)
            {
                Init();
            }

            showIntertisalNumber = GetIntertisalShowNumber();
        }

        public void Init()
        {
            if (mInited) return;
            mInited = true;
            var createInstance = UnityMainThreadDispatcher.instance;
            if (CurrentConfig == null || CurrentConfig.Platform == null)
            {
                Debug.LogError("AdsManager does not have correct Ads Config");
                return;
            }
            _Adapter = new GameObject("AdsAdapter").AddComponent<AdsAdapter>();
            _Adapter.transform.SetParent(this.transform);
            _Adapter.Init(new object[] { CurrentConfig });
            _Adapter.SetBannerPosition(_DefaultBannerPosition);

            BaseAdsAdapter.InternalEventOnInterstitialAdsChanged += () =>
            {
                if ((object)EventOnInterstitialAdsChanged != null)
                {
                    EventOnInterstitialAdsChanged(_Adapter.IsInterstitialAdAvailable(null, 0));
                }
            };
            BaseAdsAdapter.InternalEventOnRewardAdsChanged += () =>
            {
                if ((object)EventOnRewardAdsChanged != null)
                {
                    EventOnRewardAdsChanged(_Adapter.IsRewardAdAvailable(null, 0));
                }
            };
            BaseAdsAdapter.InternalEventOnOfferWallChanged += () =>
            {
                if ((object)EventOnOfferWallChanged != null)
                {
                    EventOnOfferWallChanged(_Adapter.IsOfferWallAvailable(null, 0));
                }
            };
            _Adapter.SetOfferWallCallback(onOfferwallRewardCallback);

            if (!string.IsNullOrEmpty(CurrentConfig.Platform.placementConfig[(int)AdsType.Interstitial]))
            {
                string[] parsePlacement = CurrentConfig.Platform.placementConfig[(int)AdsType.Interstitial].Split(';');
                for (int i = 0; (i + 1) < parsePlacement.Length; i += 2)
                {
                    mInterstitialAdPlacementConfig[parsePlacement[i]] = parsePlacement[i + 1];
                }
            }
            if (!string.IsNullOrEmpty(CurrentConfig.Platform.placementConfig[(int)AdsType.RewardedVideo]))
            {
                string[] parsePlacement = CurrentConfig.Platform.placementConfig[(int)AdsType.RewardedVideo].Split(';');
                for (int i = 0; (i + 1) < parsePlacement.Length; i += 2)
                {
                    mRewardAdPlacementConfig[parsePlacement[i]] = parsePlacement[i + 1];
                }
            }
            if (!string.IsNullOrEmpty(CurrentConfig.Platform.placementConfig[(int)AdsType.InterstitialRewardedVideo]))
            {
                string[] parsePlacement = CurrentConfig.Platform.placementConfig[(int)AdsType.InterstitialRewardedVideo].Split(';');
                for (int i = 0; (i + 1) < parsePlacement.Length; i += 2)
                {
                    mInterstitialRewardAdPlacementConfig[parsePlacement[i]] = parsePlacement[i + 1];
                }
            }
            if (!string.IsNullOrEmpty(CurrentConfig.Platform.placementConfig[(int)AdsType.OfferWall]))
            {
                string[] parsePlacement = CurrentConfig.Platform.placementConfig[(int)AdsType.OfferWall].Split(';');
                for (int i = 0; (i + 1) < parsePlacement.Length; i += 2)
                {
                    mOfferWallPlacementConfig[parsePlacement[i]] = parsePlacement[i + 1];
                }
            }

            if (!string.IsNullOrEmpty(CurrentConfig.Platform.placementConfig[(int)AdsType.AppOpen]))
            {
                string[] parsePlacement = CurrentConfig.Platform.placementConfig[(int)AdsType.AppOpen].Split(';');
                for (int i = 0; (i + 1) < parsePlacement.Length; i += 2)
                {
                    mAppOpenPlacementConfig[parsePlacement[i]] = parsePlacement[i + 1];
                }
            }

            if (AutoStartAdThread)
            {
                UpdateAdThread();
            }
        }

        public void UpdateAdThread()
        {
            if (_Adapter != null)
            {
                StartCoroutine(CRUpdateAdThread());
            }
            else
            {
                Debug.LogError("AdsManager is not inited correctly!");
            }
        }

        IEnumerator CRUpdateAdThread()
        {
            yield return new WaitUntil(() => mInited && _Adapter != null && _Adapter.IsInited);
            _Adapter.StartAdsThread();
        }

        public virtual bool IsAdsRemoved()
        {
            return mSaveData != null && mSaveData.RemoveAds;
        }

        public void PurchaseRemoveAds()
        {
            mSaveData.RemoveAds = true;
            DataChanged = true;
            UpdateAdThread();
        }

        public void SetBannerPosition(BannerPosition position)
        {
            if (_Adapter != null) _Adapter.SetBannerPosition(position);
        }

        public void SetBannerPosition(Vector2 screenPos)
        {
            if (_Adapter != null) _Adapter.SetBannerPosition(screenPos);
        }

        public virtual void ShowBanner()
        {
            if (!IsAdsRemoved() && _Adapter != null) _Adapter.ShowAdsBanner();
        }

        public virtual void HideBanner()
        {
            if (_Adapter != null) _Adapter.HideAdsBanner();
        }

        public virtual bool IsInterstitialAvailabie(string placement = null)
        {
            if (_Adapter == null || !AFramework.Utility.HasInternet()) return false;
            return _Adapter.IsInterstitialAdAvailable((placement != null && mInterstitialAdPlacementConfig.ContainsKey(placement)) ? mInterstitialAdPlacementConfig[placement] : null);
        }

        public bool ShowInterstitial(Action<bool> callback, string reason, string placement = null)
        {
            if (!CheatSkipAds)
            {
                if (AFramework.Utility.HasInternet())
                {
                    if (!IsAdsRemoved() && (!_Adapter || !_Adapter.FullscreenAdShowing))
                    {
                        var args = new Dictionary<string, object>();
                        args["location"] = reason;
                        AFramework.Analytics.TrackingManager.I.TrackAdsReady(AdsType.Interstitial, args);
                    }

                    {
                        var args = new Dictionary<string, object>();
                        args["location"] = reason;
                        mLastAdsShowData[(int)AdsType.Interstitial] = args;
                    }

                    if (!IsAdsRemoved() && _Adapter != null && !_Adapter.FullscreenAdShowing && _Adapter.ShowAdsInterstitial(callback, (placement != null && mInterstitialAdPlacementConfig.ContainsKey(placement)) ? mInterstitialAdPlacementConfig[placement] : null))
                    {
                        //if (_Adapter.FullscreenAdShowing)
                        //{
                        //    var args = new Dictionary<string, object>();
                        //    args["location"] = reason;
                        //    AFramework.Analytics.TrackingManager.I.TrackAdsView(AdsType.Interstitial, args);
                        //}

                        showIntertisalNumber++;
                        SaveIntertisalShowNumber(showIntertisalNumber);

                        return true;
                    }
                }
            }

            callback?.Invoke(false);
            return false;
        }

        public virtual bool IsRewardAvailable(string location, string placement = null, bool rewardAdOnly = false)
        {
            if (_Adapter == null || !AFramework.Utility.HasInternet()) return false;
            if (location != null && !_Adapter.FullscreenAdShowing)
            {
                var args = new Dictionary<string, object>();
                args["location"] = location;
                AFramework.Analytics.TrackingManager.I.TrackAdsReady(AdsType.RewardedVideo, args);
            }

            return CheatSkipAds || (_Adapter.IsRewardAdAvailable((placement != null && mRewardAdPlacementConfig.ContainsKey(placement)) ? mRewardAdPlacementConfig[placement] : null)
                || (!rewardAdOnly && _UseInterstitialForRewardAd && _Adapter.IsInterstitialAdAvailable(null))
                );
        }

        public bool ShowRewardVideo(System.Action<bool> callback, string location, string item_type, string placement = null, bool rewardAdOnly = false)
        {
            if (CheatSkipAds)
            {
                callback?.Invoke(true);
                OnRewardAdsViewed(true);
                return true;
            }

            if (_Adapter == null || _Adapter.FullscreenAdShowing || !AFramework.Utility.HasInternet())
            {
                callback?.Invoke(false);
                return false;
            }

            {
                var args = new Dictionary<string, object>();
                args["location"] = location;
                args["type"] = item_type;
                mLastAdsShowData[(int)AdsType.RewardedVideo] = args;
            }

            if (_Adapter.ShowAdsReward(callback, (placement != null && mRewardAdPlacementConfig.ContainsKey(placement)) ? mRewardAdPlacementConfig[placement] : null))
            {
                if (_Adapter.FullscreenAdShowing)
                {
                    if (_ShowSimpleLoadingScreen) AFramework.UI.CanvasManager.ShowSystemLoadingPopup(true);
                    _Adapter.AddRewardCallback(OnRewardAdsViewed);

                    //var args = new Dictionary<string, object>();
                    //args["location"] = location;
                    //args["type"] = item_type;
                    //AFramework.Analytics.TrackingManager.I.TrackAdsView(AdsType.RewardedVideo, args);
                    return true;
                }
            }
#if !USE_ADMOB
            else if (!rewardAdOnly && _UseInterstitialForRewardAd)
            {
                {
                    var args = new Dictionary<string, object>();
                    args["location"] = location;
                    args["type"] = item_type;
                    mLastAdsShowData[(int)AdsType.Interstitial] = args;
                }

                if (_Adapter.ShowAdsInterstitial(callback) && _Adapter.FullscreenAdShowing)
                {
                    if (_ShowSimpleLoadingScreen) AFramework.UI.CanvasManager.ShowSystemLoadingPopup(true);
                    _Adapter.AddInterstitialCallback(OnRewardAdsViewed);

                    //var args = new Dictionary<string, object>();
                    //args["location"] = location;
                    //args["type"] = item_type;
                    //AFramework.Analytics.TrackingManager.I.TrackAdsView(AdsType.InterstitialRewardedVideo, args);
                    return true;
                }
            }
#endif
            callback?.Invoke(false);
            return false;
        }

        public virtual bool IsInterstitialRewardAvailable(string location, string placement = null, bool rewardAdOnly = false)
        {
            if (_Adapter == null || !AFramework.Utility.HasInternet()) return false;
            if (location != null && !_Adapter.FullscreenAdShowing)
            {
                var args = new Dictionary<string, object>();
                args["location"] = location;
                AFramework.Analytics.TrackingManager.I.TrackAdsReady(AdsType.InterstitialRewardedVideo, args);
            }

            return CheatSkipAds || _Adapter.IsInterstitialRewardAdAvailable((placement != null && mInterstitialRewardAdPlacementConfig.ContainsKey(placement)) ? mInterstitialRewardAdPlacementConfig[placement] : null);
        }

        public bool ShowInterstitialRewardVideo(System.Action<bool> callback, string location, string item_type, string placement = null)
        {
            if (CheatSkipAds)
            {
                if (callback != null) callback(true);
                OnRewardAdsViewed(true);
                return true;
            }

            if (_Adapter == null || _Adapter.FullscreenAdShowing || !AFramework.Utility.HasInternet())
            {
                callback?.Invoke(false);
                return false;
            }

            {
                var args = new Dictionary<string, object>();
                args["location"] = location;
                args["type"] = item_type;
                mLastAdsShowData[(int)AdsType.Interstitial] = args;
            }

            if (_Adapter.ShowAdsInterstitialReward(callback, (placement != null && mInterstitialRewardAdPlacementConfig.ContainsKey(placement)) ? mInterstitialRewardAdPlacementConfig[placement] : null))
            {
                if (_Adapter.FullscreenAdShowing)
                {
                    if (_ShowSimpleLoadingScreen) AFramework.UI.CanvasManager.ShowSystemLoadingPopup(true);
                    _Adapter.AddRewardCallback(OnRewardAdsViewed);

                    //var args = new Dictionary<string, object>();
                    //args["location"] = location;
                    //args["type"] = item_type;
                    //AFramework.Analytics.TrackingManager.I.TrackAdsView(AdsType.InterstitialRewardedVideo, args);
                    return true;
                }
            }
            callback?.Invoke(false);
            return false;
        }

        public void CheckOfferwallReward()
        {
            if (_Adapter == null || _Adapter.FullscreenAdShowing) return;
            _Adapter.CheckOfferwallReward();
        }

        public virtual bool IsOfferWallAvailable(string location, string placement = null)
        {
            if (_Adapter == null || !AFramework.Utility.HasInternet()) return false;

            if (location != null && !_Adapter.FullscreenAdShowing)
            {
                var args = new Dictionary<string, object>();
                args["location"] = location;
                AFramework.Analytics.TrackingManager.I.TrackAdsReady(AdsType.OfferWall, args);
            }

            return _Adapter.IsOfferWallAvailable((placement != null && mOfferWallPlacementConfig.ContainsKey(placement)) ? mOfferWallPlacementConfig[placement] : null);
        }

        public void ShowOfferWall(string location, string placement = null)
        {
            if (_Adapter == null || _Adapter.FullscreenAdShowing) return;

            if (_Adapter.ShowOfferWall(placement))
            {
                var args = new Dictionary<string, object>();
                args["location"] = location;
                AFramework.Analytics.TrackingManager.I.TrackAdsView(AdsType.OfferWall, args);
            }
        }

        public virtual bool IsAppOpenAvailabie(string placement = null)
        {
            if (_Adapter == null || !AFramework.Utility.HasInternet()) return false;
            return _Adapter.IsAppOpenAdAvailable((placement != null && mAppOpenPlacementConfig.ContainsKey(placement)) ? mAppOpenPlacementConfig[placement] : null);
        }

        public bool ShowAppOpen(Action<bool> callback, string reason, string placement = null)
        {
            if (!CheatSkipAds)
            {
                if (AFramework.Utility.HasInternet())
                {
                    if (!IsAdsRemoved() && (!_Adapter || !_Adapter.FullscreenAdShowing))
                    {
                        var args = new Dictionary<string, object>();
                        args["location"] = reason;
                        AFramework.Analytics.TrackingManager.I.TrackAdsReady(AdsType.AppOpen, args);
                    }

                    {
                        var args = new Dictionary<string, object>();
                        args["location"] = reason;
                        mLastAdsShowData[(int)AdsType.AppOpen] = args;
                    }

                    if (!IsAdsRemoved() && _Adapter != null && !_Adapter.FullscreenAdShowing && _Adapter.ShowAdAppOpen(callback, (placement != null && mAppOpenPlacementConfig.ContainsKey(placement)) ? mAppOpenPlacementConfig[placement] : null))
                    {
                        //if (_Adapter.FullscreenAdShowing)
                        //{
                        //    var args = new Dictionary<string, object>();
                        //    args["location"] = reason;
                        //    AFramework.Analytics.TrackingManager.I.TrackAdsView(AdsType.Interstitial, args);
                        //}
                        return true;
                    }
                }
            }

            callback?.Invoke(false);
            return false;
        }

        public int GetRewardAdsViewNumber()
        {
            return mSaveData != null ? mSaveData.RewardAdsViewed : 0;
        }

        public bool IsFullscreenAdShowing()
        {
            return _Adapter != null && _Adapter.FullscreenAdShowing;
        }

        public int GetAdsDownloadTimeout()
        {
            if (mSaveData == null) return -1;
            return mSaveData.AdsDownloadTimeout;
        }

        protected virtual void OnRewardAdsViewed(bool result)
        {
            if (result)
            {
                if (mSaveData != null)
                {
                    ++mSaveData.RewardAdsViewed;
                    DataChanged = true;
                }
            }
            else
            {
                Analytics.TrackingManager.I.TrackAdsRewardSkip(null);
            }
            if (_ShowSimpleLoadingScreen) AFramework.UI.CanvasManager.ShowSystemLoadingPopup(false);
        }

        public bool IsAdEnabled(AdsType type)
        {
            switch (type)
            {
                case AdsType.Banner:
                case AdsType.Interstitial:
                    return (!IsAdsRemoved() || _UseInterstitialForRewardAd) && SupportAdsType[(int)type];
                case AdsType.RewardedVideo:
                case AdsType.InterstitialRewardedVideo:
                case AdsType.OfferWall:
                case AdsType.AppOpen:
                    return SupportAdsType[(int)type];
            }
            return false;
        }

        public void SetUseInterstitialForReward(bool allow)
        {
            _UseInterstitialForRewardAd = allow;
            if (mSaveData == null) return;
            mSaveData.UseInterstitialForReward = allow;
            DataChanged = true;
        }

        public void SetAdsDownloadTimeout(int time)
        {
            if (mSaveData == null) return;
            mSaveData.AdsDownloadTimeout = time;
            DataChanged = true;
        }

        void onOfferwallRewardCallback(Dictionary<string, object> rewardData)
        {
            if (rewardData == null) return;
#if USE_IRONSOURCE_ADS
            for (int i = 0; i < OfferwallCreditNames.Length; ++i)
            {
                if (rewardData.ContainsKey(OfferwallCreditNames[i].ToLower()))
                {
                    int rewardValue = int.Parse(rewardData[OfferwallCreditNames[i].ToLower()].ToString());
                    if (rewardValue > 0)
                    {
                        mSaveData.TotalOfferwallCreditRewarded[OfferwallCreditNames[i]] += rewardValue;
                        DataChanged = true;
                    }
                }
            }
#else
            Debug.LogError("onOfferwallRewardCallback unhandle");
#endif

            bool hasReward = false;
            foreach (var pair in mSaveData.TotalOfferwallCreditRewarded)
            {
                if (pair.Value > 0)
                {
                    hasReward = true;
                    break;
                }
            }

            if (hasReward) HandleOfferwallReward();
        }

        protected void onConsumeAllOfferwallReward()
        {
            for (int i = 0; i < OfferwallCreditNames.Length; ++i)
            {
                if (mSaveData.TotalOfferwallCreditRewarded[OfferwallCreditNames[i]] > 0)
                {
                    var args = new Dictionary<string, object>();
                    args["type"] = OfferwallCreditNames[i];
                    args["value"] = mSaveData.TotalOfferwallCreditRewarded[OfferwallCreditNames[i]];
                    AFramework.Analytics.TrackingManager.I.TrackEvent("ADS_OFFERWALL_CLAIM", args);
                }
                mSaveData.TotalOfferwallCreditRewarded[OfferwallCreditNames[i]] = 0;
            }
            DataChanged = true;
        }

        protected virtual void HandleOfferwallReward()
        {
            Debug.LogWarning("Need to override and implement for each game!!!");
        }

        protected virtual IEnumerator CRAutoCheckOfferwallReward()
        {
            WaitForSeconds waitTime = autoCheckOfferwallRewardDelay;
            yield return waitTime;
            yield return waitTime;
            yield return waitTime;

            while (!IsOfferWallAvailable(null))
            {
                yield return waitTime;
            }

            const int OfferwallCheckTimeCooldown = 60 * 5;

            float timeCount = -1;
            while (true)
            {
                while (!AFramework.Utility.HasInternet() || !IsSafeState()) yield return waitTime;

                if (timeCount < 0)
                {
                    CheckOfferwallReward();
                    timeCount = OfferwallCheckTimeCooldown;
                }

                yield return waitTime;
                timeCount -= 1;
            }
        }

        bool safeState = true;
        public virtual bool IsSafeState() { return safeState; }
        public virtual void SetSafeState(bool state)
        {
            safeState = state;
        }

        public virtual void OnAdsShowSuccess(AdsType type)
        {
            var args = mLastAdsShowData[(int)type];
            if (args == null) args = new Dictionary<string, object>();
            AFramework.Analytics.TrackingManager.I.TrackAdsView(type, args);
        }

        bool ATTChecked = false;
        protected void CheckATTState()
        {
            if (ATTChecked) return;
            ATTChecked = true;
#if UNITY_IOS
            IsATTAsked = TrackingManager.I.GetATState() != 0;
#else
            IsATTAsked = true;
#endif
        }

        #region Save
        [System.Serializable]
        public class OfferwallCreditRewardData : com.spacepuppy.Collections.SerializableDictionaryBase<string, int> { }

        [System.Serializable]
        public class SaveData
        {
            public bool RemoveAds = false;
            public int RewardAdsViewed = 0;
            public bool UseInterstitialForReward = false;
            public OfferwallCreditRewardData TotalOfferwallCreditRewarded = new OfferwallCreditRewardData();
            public int AdsDownloadTimeout = -1;
        }

        protected SaveData mSaveData;

        public bool DataChanged { get; set; }

        public object GetData()
        {
            return mSaveData;
        }

        public void RegisterSaveData()
        {
            SaveGameManager.instance.RegisterMandatoryData("ads", this as ISaveData);
        }

        public virtual void SetData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                mSaveData = new SaveData();
                mSaveData.UseInterstitialForReward = _UseInterstitialForRewardAd;
                DataChanged = true;
            }
            else
            {
                mSaveData = JsonUtility.FromJson<SaveData>(data);
                _UseInterstitialForRewardAd = mSaveData.UseInterstitialForReward;
            }

            if (mSaveData.TotalOfferwallCreditRewarded == null) mSaveData.TotalOfferwallCreditRewarded = new OfferwallCreditRewardData();
            for (int i = 0; i < OfferwallCreditNames.Length; ++i)
            {
                if (!mSaveData.TotalOfferwallCreditRewarded.ContainsKey(OfferwallCreditNames[i])) mSaveData.TotalOfferwallCreditRewarded[OfferwallCreditNames[i]] = 0;
            }
        }

        public virtual void OnAllDataLoaded()
        {
            CheckATTState();
            if (AutoInit)
                Init();
            if (SupportAdsType[(int)AdsType.OfferWall])
            {
                StartCoroutine(CRAutoCheckOfferwallReward());
            }
        }
        #endregion

        private int GetIntertisalShowNumber()
        {
            return PlayerPrefs.GetInt("showIntertisalNumber", 0);
        }

        private void SaveIntertisalShowNumber(int number)
        {
            PlayerPrefs.SetInt("showIntertisalNumber", number);
        }

#if UNITY_EDITOR
        void Reset()
        {
            _Config = FrameworkGlobalConfig.Instance;
        }

        private void OnValidate()
        {
            if (SupportAdsType.Length != (int)AdsType.NUM)
            {
                SupportAdsType = AFramework.Utility.ResizeArray<bool>(SupportAdsType, (int)AdsType.NUM, false);
            }
        }
#endif
    }
}