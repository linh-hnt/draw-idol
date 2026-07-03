using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.Ads
{
    public class BaseAdsAdapter : MonoBehaviour
    {
        public const float DEFAULT_DPI = 160;
        public const float BANNER_RATIO = 320f / 50;

        protected delegate AdsLoadState LoadStateDelegate();
        protected delegate bool boolDelegate();

        public static System.Action InternalEventOnBannerAdsChanged;
        public static System.Action InternalEventOnInterstitialAdsChanged;
        public static System.Action InternalEventOnRewardAdsChanged;
        public static System.Action InternalEventOnOfferWallChanged;
        protected static int[] sAdsErrorDelayTime = new int[4] { 0, 2, 4, 6 };
        public const float AdsAvailableSafeTime = 0.5f;

        protected bool mInited = false;
        public bool IsInited { get { return mInited; } }
        protected BaseAdapterConfig mConfig;
        protected BannerPosition mBannerPosition = BannerPosition.Bottom;
        protected System.Action<bool> mInterstitialAdCallback;
        protected System.Action<bool> mRewardAdCallback;
        protected System.Action<Dictionary<string, object>> mOfferWallCallback;
        protected System.Action<bool> mAppOpenCallback;
        protected IEnumerator mAdsThreadHolder;

        protected bool mBannerAdVisibility = false;
        protected bool mFullScreenAdShowing = false;

        protected Dictionary<string, AdStatusHandler> mAdDownloadHandler = new Dictionary<string, AdStatusHandler>();
        protected List<AdStatusHandler> mAdHighPriorityList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mAdLowPriorityList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mDefaultBannerAdList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mDefaultInterstitialAdList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mDefaultRewardAdList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mDefaultOfferWallAdList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mDefaultInterstitialRewardAdList = new List<AdStatusHandler>();
        protected List<AdStatusHandler> mDefaultAppOpenAdList = new List<AdStatusHandler>();
        //protected AdsLoadState mAdLoadState = AdsLoadState.Idle;
        //protected AdStatusHandler mCurrentDownload = null;
        //protected AdStatusHandler CurrentDownload { get { return mCurrentDownload; } }

        private AdsLoadState[] mAdsLoadStateArray;
        private AdStatusHandler[] mCurrentDownloadArray;
        protected bool IsAnyAdsDownload()
        {
            int len = mCurrentDownloadArray.Length;
            for (int i = 0; i < len; ++i)
            {
                if (mCurrentDownloadArray[i] != null) return true;
            }
            return false;
        }
        protected AdStatusHandler GetCurrentDownload(AdsType type) { return mCurrentDownloadArray[(int)type]; }
        protected AdsLoadState GetCurrentAdsLoadState(AdsType type) { return mAdsLoadStateArray[(int)type]; }
        protected void ForceFreeCurrentDownload(AdsType type)
        {
            mCurrentDownloadArray[(int)type] = null;
            mAdsLoadStateArray[(int)type] = AdsLoadState.Idle;
        }


        protected AdStatusHandler mCurrentFullscreenAd = null;
        float mLastFullScreenAdsTime = -1;
        public bool FullscreenAdShowing
        {
            get { return mFullScreenAdShowing || mCurrentFullscreenAd != null || mLastFullScreenAdsTime + 1.5f > Time.realtimeSinceStartup; }
            protected set
            {
                if (value == true)
                {
                    mLastFullScreenAdsTime = float.MaxValue - 5;
                }
                else if (mFullScreenAdShowing == true)
                {
                    mLastFullScreenAdsTime = Time.realtimeSinceStartup;
                }
                mFullScreenAdShowing = value;
            }
        }

        private void Awake()
        {
            mAdsLoadStateArray = new AdsLoadState[(int)AdsType.NUM];
            mCurrentDownloadArray = new AdStatusHandler[(int)AdsType.NUM];

            StartCoroutine(CRWaitForTrackingId());
        }

        public virtual void Init(object[] parameters)
        {
            UpdateDownloadList();
            mInited = true;
        }
        public virtual void StartAdsThread()
        {
            if (mConfig == null) return;
            if (mAdsThreadHolder != null) return;

            if (mConfig.Platform.LoadMultipleAds)
            {
                int delayTime = 0;

                if (AdsManager.I.IsAdEnabled(AdsType.AppOpen))
                {
                    StartCoroutine(CRAutoRequestThread(AdsType.AppOpen, delayTime));
                    delayTime = 4;
                }
                mAdsThreadHolder = CRAutoRequestThread(AdsType.RewardedVideo, delayTime++);
                StartCoroutine(mAdsThreadHolder);

                StartCoroutine(CRAutoRequestThread(AdsType.Interstitial, delayTime++));
                StartCoroutine(CRAutoRequestThread(AdsType.Banner, delayTime++));
                StartCoroutine(CRAutoRequestThread(AdsType.OfferWall, delayTime++));
                StartCoroutine(CRAutoRequestThread(AdsType.InterstitialRewardedVideo, delayTime++));
            }
            else
            {
                mAdsThreadHolder = CRAutoRequestThread(AdsType.NUM, 0);
                StartCoroutine(mAdsThreadHolder);
            }
        }

        public virtual void SetBannerPosition(BannerPosition position) { mBannerPosition = position; }
        public virtual void SetBannerPosition(Vector2 screenPos) { Debug.LogError("TODO"); }
        public virtual void ShowAdsBanner() { mBannerAdVisibility = true; }
        public virtual void HideAdsBanner() { mBannerAdVisibility = false; }
        public virtual bool ShowAdsInterstitial(System.Action<bool> callback, string adId = null)
        {
            mInterstitialAdCallback = callback;
            return false;
        }

        public void AddInterstitialCallback(System.Action<bool> callback)
        {
            mInterstitialAdCallback += callback;
        }

        public virtual bool ShowAdsReward(System.Action<bool> callback, string adId = null)
        {
            mRewardAdCallback = callback;
            return false;
        }

        public void AddRewardCallback(System.Action<bool> callback)
        {
            mRewardAdCallback += callback;
        }

        public virtual bool ShowAdsInterstitialReward(System.Action<bool> callback, string adId = null)
        {
            mRewardAdCallback = callback;
            return false;
        }

        public virtual bool ShowOfferWall(string placementName)
        {
            return false;
        }

        public virtual void CheckOfferwallReward() { }

        public void SetOfferWallCallback(System.Action<Dictionary<string, object>> callback)
        {
            mOfferWallCallback = callback;
        }

        public virtual bool ShowAdAppOpen(System.Action<bool> callback, string adId = null)
        {
            mAppOpenCallback = callback;
            return false;
        }

        public virtual bool IsInterstitialAdAvailable(string adId = null, float safeTime = BaseAdsAdapter.AdsAvailableSafeTime)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultInterstitialAdList.Count; ++i)
                {
                    if (mDefaultInterstitialAdList[i].IsAvailable(safeTime)) return true;
                }
                return false;
            }
            return mAdDownloadHandler[adId].IsAvailable(safeTime);
        }
        public virtual bool IsRewardAdAvailable(string adId = null, float safeTime = BaseAdsAdapter.AdsAvailableSafeTime)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultRewardAdList.Count; ++i)
                {
                    if (mDefaultRewardAdList[i].IsAvailable(safeTime)) return true;
                }
                return false;
            }
            return mAdDownloadHandler[adId].IsAvailable(safeTime);
        }

        public virtual bool IsInterstitialRewardAdAvailable(string adId = null, float safeTime = BaseAdsAdapter.AdsAvailableSafeTime)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultInterstitialRewardAdList.Count; ++i)
                {
                    if (mDefaultInterstitialRewardAdList[i].IsAvailable(safeTime)) return true;
                }
                return false;
            }
            return mAdDownloadHandler[adId].IsAvailable(safeTime);
        }

        public virtual bool IsOfferWallAvailable(string adId = null, float safeTime = BaseAdsAdapter.AdsAvailableSafeTime)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultOfferWallAdList.Count; ++i)
                {
                    if (mDefaultOfferWallAdList[i].IsAvailable(safeTime)) return true;
                }
                return false;
            }
            return mAdDownloadHandler[adId].IsAvailable(safeTime);
        }

        public virtual bool IsAppOpenAdAvailable(string adId = null, float safeTime = BaseAdsAdapter.AdsAvailableSafeTime)
        {
            if (string.IsNullOrEmpty(adId))
            {
                for (int i = 0; i < mDefaultAppOpenAdList.Count; ++i)
                {
                    if (mDefaultAppOpenAdList[i].IsAvailable(safeTime)) return true;
                }
                return false;
            }
            return mAdDownloadHandler[adId].IsAvailable(safeTime);
        }

        protected virtual AdStatusHandler CreateAdHandler(AdsType type, string id) { return new AdStatusHandler(type, id); }

        protected virtual void UpdateDownloadList()
        {
            if (mConfig == null || mConfig.Platform == null) return;
            var platformConfig = mConfig.Platform;
            AdsType[] priorityOrder = new AdsType[(int)AdsType.NUM] {
                AdsType.AppOpen, AdsType.RewardedVideo, AdsType.OfferWall, AdsType.Interstitial, AdsType.Banner, AdsType.InterstitialRewardedVideo
            };
            List<AdStatusHandler>[] defaultAdOrder = new List<AdStatusHandler>[(int)AdsType.NUM] {
                mDefaultAppOpenAdList, mDefaultRewardAdList, mDefaultOfferWallAdList, mDefaultInterstitialAdList, mDefaultBannerAdList, mDefaultInterstitialRewardAdList
            };

            string[][] mainIdList = new string[(int)AdsType.NUM][] {
                platformConfig.AppOpenId.Split(';'),
                platformConfig.RewardedVideoId.Split(';'),
                platformConfig.OfferWallId.Split(';'),
                platformConfig.InterstitialId.Split(';'),
                platformConfig.BannerlId.Split(';'),
                platformConfig.InterstitialRewardedId.Split(';')
            };
            //calculate Main max length
            int maxMainIdLength = 0;
            for (int i = 0; i < (int)AdsType.NUM; ++i)
            {
                maxMainIdLength = Mathf.Max(maxMainIdLength, mainIdList[i].Length);
            }

            //calculate placement max length
            string[][] placementIdList = new string[(int)AdsType.NUM][];
            int maxPlacementIdLength = 0;
            for (int i = 0; i < (int)AdsType.NUM; ++i)
            {
                var ids = platformConfig.placementConfig[i].Split(';');
                placementIdList[i] = ids;
                maxPlacementIdLength = Mathf.Max(maxPlacementIdLength, ids.Length / 2);
            }

            //Add first Main Priority to DownloadList
            for (int i = 0; i < priorityOrder.Length; ++i)
            {
                var mainIds = mainIdList[i];
                AdStatusHandler handler = null;
                var adType = priorityOrder[i];
                if (AdsManager.I.IsAdEnabled(adType) && mainIds.Length > 0 && !string.IsNullOrEmpty(mainIds[0]))
                {
                    handler = CreateAdHandler(adType, mainIds[0]);
                    if (!mAdDownloadHandler.ContainsKey(handler._Id))
                        mAdDownloadHandler.Add(handler._Id, handler);
                    mAdHighPriorityList.Add(handler);
                    defaultAdOrder[i].Add(handler);
                }
            }

            //Add Placement to Download List
            for (int index = 0; index < maxPlacementIdLength; ++index)
            {
                for (int i = 0; i < priorityOrder.Length; ++i)
                {
                    var placementIds = placementIdList[i];
                    var adType = priorityOrder[i];
                    if (AdsManager.I.IsAdEnabled(adType) && index < placementIds.Length && !mAdDownloadHandler.ContainsKey(placementIds[index + 1]))
                    {
                        AdStatusHandler handler = CreateAdHandler(adType, placementIds[index + 1]);
                        mAdDownloadHandler.Add(handler._Id, handler);
                        mAdLowPriorityList.Add(handler);
                    }
                }
            }

            //Add remain Main to Download List
            for (int index = 1; index < maxMainIdLength; ++index)
            {
                for (int i = 0; i < priorityOrder.Length; ++i)
                {
                    var ids = mainIdList[i];
                    var adType = priorityOrder[i];
                    if (AdsManager.I.IsAdEnabled(adType) && index < ids.Length && !mAdDownloadHandler.ContainsKey(ids[index]))
                    {
                        AdStatusHandler handler = CreateAdHandler(adType, ids[index]);
                        mAdDownloadHandler.Add(handler._Id, handler);
                        mAdLowPriorityList.Add(handler);
                        defaultAdOrder[i].Add(handler);
                    }
                }
            }
        }

        IEnumerator CRAutoRequestThread(AdsType adsType, float delayTime)
        {
            if (delayTime > 0)
            {
                yield return new WaitForSeconds(delayTime);
            }

            AdStatusHandler[] highPriorityList = null;
            AdStatusHandler[] lowPriorityList = null;

            if (adsType == AdsType.NUM)
            {
                highPriorityList = mAdHighPriorityList.ToArray();
                lowPriorityList = mAdLowPriorityList.ToArray();
            }
            else
            {
                List<AdStatusHandler> tempList = new List<AdStatusHandler>();
                for (int i = 0; i < mAdHighPriorityList.Count; ++i)
                {
                    if (mAdHighPriorityList[i]._Type == adsType)
                    {
                        tempList.Add(mAdHighPriorityList[i]);
                    }
                }
                highPriorityList = tempList.ToArray();

                tempList.Clear();
                for (int i = 0; i < mAdLowPriorityList.Count; ++i)
                {
                    if (mAdLowPriorityList[i]._Type == adsType)
                    {
                        tempList.Add(mAdLowPriorityList[i]);
                    }
                }
                lowPriorityList = tempList.ToArray();
            }

            if (highPriorityList.Length <= 0) yield break;
            int offsetIndex = 0;
            int errorCount = 0;
            float downloadTime = 0;
            AdStatusHandler lastDownloadHandler = null;
            while (true)
            {
                while (!Utility.DelayHasInternet())
                {
                    yield return null;
                }

                AdStatusHandler selectedDownload = null;
                for (int i = 0; i < highPriorityList.Length; ++i)
                {
                    if (!highPriorityList[i].IsAvailable(0) && highPriorityList[i] != lastDownloadHandler)
                    {
                        selectedDownload = highPriorityList[i];
                        break;
                    }
                }

                if (selectedDownload == null)
                {
                    for (int i = 0; i < lowPriorityList.Length; ++i)
                    {
                        int index = (offsetIndex + i) % lowPriorityList.Length;
                        if (!lowPriorityList[index].IsAvailable(0) && lowPriorityList[index] != lastDownloadHandler)
                        {
                            selectedDownload = lowPriorityList[index];
                            break;
                        }
                    }
                    ++offsetIndex;
                }

                lastDownloadHandler = selectedDownload;
                if (selectedDownload != null)
                {
                    while (mCurrentFullscreenAd != null)
                    {
                        yield return null;
                    }

                    selectedDownload.ForceCheckAvailable();
                    if (!selectedDownload.IsAvailable(0))
                    {
                        DownloadAd(selectedDownload);
                        downloadTime = 0;
                        while (mAdsLoadStateArray[(int)selectedDownload._Type] == AdsLoadState.Downloading)
                        {
                            yield return null;

                            downloadTime += Time.deltaTime;
                            if (downloadTime >= (AdsManager.I.GetAdsDownloadTimeout() > 30 ? AdsManager.I.GetAdsDownloadTimeout() : mConfig.GetDownloadTimeout()))
                            {
                                mAdsLoadStateArray[(int)selectedDownload._Type] = AdsLoadState.Error;
                                if (selectedDownload != null) AdDownloadCallback(selectedDownload._Type, false, "timeout", null);
                            }
                        }

                        if (mAdsLoadStateArray[(int)selectedDownload._Type] == AdsLoadState.Error)
                        {
                            yield return new WaitForSeconds(mConfig.GetErrorRetryInterval() + sAdsErrorDelayTime[Mathf.Min(errorCount, sAdsErrorDelayTime.Length - 1)]);
                            ++errorCount;
                        }
                        else if (mAdsLoadStateArray[(int)selectedDownload._Type] == AdsLoadState.Loaded)
                        {
                            errorCount = 0;

                            Dictionary<string, object> dic = new Dictionary<string, object>();
                            dic["value"] = downloadTime;
                            Analytics.TrackingManager.I.TrackEvent(string.Format("ADS_{0}_DOWNLOAD", selectedDownload._Type.ToString().ToUpper()), dic);
                        }
                    }
                }
#if USE_UNITY_ADS
                else
                {
                    bool allAvailable = true;
                    for (int i = 0; i < highPriorityList.Length; ++i)
                    {
                        if (!highPriorityList[i].IsAvailable(0))
                        {
                            allAvailable = false;
                            break;
                        }
                    }

                    for (int i = 0; i < lowPriorityList.Length; ++i)
                    {
                        if (!lowPriorityList[i].IsAvailable(0))
                        {
                            allAvailable = false;
                            break;
                        }
                    }

                    if (allAvailable)
                    {
                        yield break;
                    }
                }
#endif

                yield return null;
            }
        }

        protected virtual void DownloadAd(AdStatusHandler ad)
        {
            mAdsLoadStateArray[(int)ad._Type] = AdsLoadState.Downloading;
            mCurrentDownloadArray[(int)ad._Type] = ad;
            //Need each adapter to implement it's own code
        }

        protected virtual void AdDownloadCallback(AdsType adType, bool result, string errorCode, string errorMessage)
        {
            var currentDownload = GetCurrentDownload(adType);
            if (currentDownload == null)
            {
                HandleAdsStatusEvent(adType);
                if (AdsManager.Debugging) Debug.Log("AdDownload mCurrentDownload is null but there is still have callback");
                return;
            }

            if (adType != currentDownload._Type)
            {
                HandleAdsStatusEvent(adType);
                if (AdsManager.Debugging) Debug.Log("AdDownload callback type " + adType + " does not match current download type " + currentDownload._Type);
                return;
            }

            if (result)
            {
                if (AdsManager.Debugging) Debug.Log("AdDownload " + currentDownload._Type + " id " + currentDownload._Id + " result success");
                mAdsLoadStateArray[(int)adType] = AdsLoadState.Loaded;
            }
            else
            {
                if (AdsManager.Debugging) Debug.Log("AdDownload " + currentDownload._Type + " id " + currentDownload._Id + " result failed: " + errorCode + " - " + errorMessage);
                mAdsLoadStateArray[(int)adType] = AdsLoadState.Error;
                Analytics.TrackingManager.I.TrackAdsDownloadFail(currentDownload._Type, errorCode, errorMessage);
            }
            currentDownload.OnAdAvailabilityUpdate(result);
            mCurrentDownloadArray[(int)adType] = null;
            HandleAdsStatusEvent(adType);
        }

        protected virtual void AdDisplayResultCallback(AdsType adType, bool result, string errorCode, string errorMessage)
        {
            if (result)
            {
                AdsManager.I.OnAdsShowSuccess(adType);
            }
            else
            {
                AFramework.Analytics.TrackingManager.I.TrackAdsShowFail(adType, errorCode, errorMessage);
            }
        }

        protected virtual void SetCustomUserId(string userId) { }

        IEnumerator CRWaitForTrackingId()
        {
            while (!Analytics.TrackingManager.IsInstanceValid() || !Analytics.TrackingManager.I.IsInited)
            {
                yield return null;
            }

            string resultId = null;
#if false//USE_FIREBASE
            while (!AFramework.FirebaseService.FirebaseInstance.HasInstance)
            {
                yield return null;
            }
            System.Threading.Tasks.Task<string> t = Firebase.Installations.FirebaseInstallations.DefaultInstance.GetIdAsync();
            while (!t.IsCompleted)
            {
                yield return null;
            }
            resultId = t.Result;
#else
            resultId = Analytics.TrackingManager.I.AppsflyerID;
#endif
            if (!string.IsNullOrEmpty(resultId))
            {
                SetCustomUserId(resultId);
            }
        }

        protected void TrackAdRevenue(AdsType adType, double revenue, string network, string currency, string adPlatform, string adUnitName, string adFormat, string placement)
        {
            Analytics.TrackingManager.I.TrackAdsRevenue(adType, revenue, network, currency, adPlatform, adUnitName, adFormat, placement);
        }

        void HandleAdsStatusEvent(AdsType type)
        {
            if (type == AdsType.RewardedVideo)
            {
                if (InternalEventOnRewardAdsChanged != null) InternalEventOnRewardAdsChanged.Invoke();
            }
            else if (type == AdsType.Interstitial || type == AdsType.InterstitialRewardedVideo)
            {
                if (InternalEventOnInterstitialAdsChanged != null) InternalEventOnInterstitialAdsChanged.Invoke();
            }
            else if (type == AdsType.Banner)
            {
                if (InternalEventOnBannerAdsChanged != null) InternalEventOnBannerAdsChanged.Invoke();
            }
            else if (type == AdsType.OfferWall)
            {
                if (InternalEventOnOfferWallChanged != null) InternalEventOnOfferWallChanged.Invoke();
            }
        }
    }

    public class AdStatusHandler
    {
        public AdsType _Type { get; protected set; }
        public string _Id { get; protected set; }
        public bool _Available { get; protected set; }

        public AdStatusHandler(AdsType type, string id)
        {
            _Type = type;
            _Id = id;
        }

        const float MaxAvailableTime = float.MaxValue - 999;
        protected float mLastCheckTime = -1;
        protected float mAdAvailableTime = MaxAvailableTime;
        public virtual bool IsAvailable(float safeTime = BaseAdsAdapter.AdsAvailableSafeTime)
        {
            float checkTime = _Available ? 5.0f : 1.0f;
            if (mLastCheckTime + checkTime < Time.time)
            {
                bool cache = _Available;
                switch (_Type)
                {
                    case AdsType.Banner:
                        cache = BannerAdAvailable();
                        break;
                    case AdsType.Interstitial:
                        cache = InterstitialAdAvailable();
                        break;
                    case AdsType.RewardedVideo:
                        cache = RewardAdAvailable();
                        break;
                    case AdsType.InterstitialRewardedVideo:
                        cache = InterstitialRewardAdAvailable();
                        break;
                    case AdsType.OfferWall:
                        cache = OfferWallAvailable();
                        break;
                }

                if (cache != _Available)
                {
                    OnAdAvailabilityUpdate(cache);
                }
                else
                {
                    mLastCheckTime = Time.time;
                }
            }
            return _Available && Time.time >= (mAdAvailableTime + safeTime);
        }

        protected virtual bool BannerAdAvailable() { return false; }
        protected virtual bool InterstitialAdAvailable() { return false; }
        protected virtual bool RewardAdAvailable() { return false; }
        protected virtual bool InterstitialRewardAdAvailable() { return false; }
        protected virtual bool OfferWallAvailable() { return false; }
        protected virtual bool AppOpenAdAvailable() { return false; }

        public virtual void OnAdAvailabilityUpdate(bool result)
        {
            mLastCheckTime = Time.time;
            _Available = result;
            if (result) mAdAvailableTime = Time.time;
            else mAdAvailableTime = MaxAvailableTime;
        }

        public void ForceCheckAvailable()
        {
            bool cache = false;
            switch (_Type)
            {
                case AdsType.Banner:
                    cache = BannerAdAvailable();
                    break;
                case AdsType.Interstitial:
                    cache = InterstitialAdAvailable();
                    break;
                case AdsType.RewardedVideo:
                    cache = RewardAdAvailable();
                    break;
                case AdsType.InterstitialRewardedVideo:
                    cache = InterstitialRewardAdAvailable();
                    break;
                case AdsType.OfferWall:
                    cache = OfferWallAvailable();
                    break;
            }
            OnAdAvailabilityUpdate(cache);
        }
    }

}