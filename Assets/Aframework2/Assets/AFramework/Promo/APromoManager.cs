using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace AFramework.Promo
{
    [System.Serializable]
    public class PromoData
    {
        public string game_name;
        public string location;
        public int max_stage = 0;
        public string width;
        public string height;
        public string videoUrl;
        public string imageUrl;
        public string promoUrl;

        public string VideoFileDownloaded;
        public string ImageFileDownloaded;

        Texture mCacheTexture = null;

        public bool IsValid() { return !string.IsNullOrEmpty(promoUrl) && (!string.IsNullOrEmpty(promoUrl) || !string.IsNullOrEmpty(promoUrl)); }

        public Texture GetImageTexture(bool cache)
        {
            if (mCacheTexture != null) return mCacheTexture;
            if (string.IsNullOrEmpty(ImageFileDownloaded)) return null;
            byte[] byteArray = System.IO.File.ReadAllBytes(ImageFileDownloaded);
            Texture2D texture = new Texture2D(0, 0);
            texture.LoadImage(byteArray);
            if (cache) mCacheTexture = texture;
            return texture;
        }

        public static bool operator == (PromoData left,
                                         PromoData right)
        {
            var leftNull = object.ReferenceEquals(left, null);
            var rightNull = object.ReferenceEquals(right, null);
            if (leftNull && rightNull) return true;
            else if (leftNull != rightNull) return false;

            return (
                (left.location == right.location)
                && (left.videoUrl == right.videoUrl)
                && (left.imageUrl == right.imageUrl)
                && (left.promoUrl == right.promoUrl)
                );
        }

        public static bool operator !=(PromoData left,
                                         PromoData right)
        {
            var leftNull = object.ReferenceEquals(left, null);
            var rightNull = object.ReferenceEquals(right, null);
            if (leftNull && rightNull) return false;
            else if (leftNull != rightNull) return true;

            return (
                (left.location != right.location)
                || (left.videoUrl != right.videoUrl)
                || (left.imageUrl != right.imageUrl)
                || (left.promoUrl != right.promoUrl)
                );
        }
    }

    public class VideoHandler
    {
        public Color BackgroundColor = Color.black;
        public PromoData VideoData { get; set; }
        RawImage mVideoHolder;
        UnityEngine.Video.VideoPlayer mVideoPlayer;
        AudioSource mAudioSource;
        System.Action mEndCallback;
        RenderTexture mRenderTexture;

        public VideoHandler(RawImage videoHolder, PromoData promoData, System.Action endCallback, RenderTexture renderTexture = null)
        {
            mVideoHolder = videoHolder;
            VideoData = promoData;
            mRenderTexture = renderTexture;
            mEndCallback = endCallback;
            if (!string.IsNullOrEmpty(VideoData.VideoFileDownloaded))
            {
                mVideoPlayer = mVideoHolder.gameObject.GetComponent<UnityEngine.Video.VideoPlayer>();
                if (mVideoPlayer == null) mVideoPlayer = mVideoHolder.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                mVideoPlayer.loopPointReached += OnLoopPointReached;

                mAudioSource = mVideoHolder.gameObject.GetComponent<AudioSource>();
                if (mAudioSource == null) mAudioSource = mVideoHolder.gameObject.AddComponent<AudioSource>();

                mVideoPlayer.source = UnityEngine.Video.VideoSource.Url;
                mVideoPlayer.url = VideoData.VideoFileDownloaded;
                mVideoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
                mVideoPlayer.EnableAudioTrack(0, true);
                mVideoPlayer.SetTargetAudioSource(0, mAudioSource);
                mVideoPlayer.Prepare();

                if (mRenderTexture == null)
                {
                    mRenderTexture = new RenderTexture(int.Parse(VideoData.width), int.Parse(VideoData.height), 16, RenderTextureFormat.ARGB32);
                }
                mVideoPlayer.targetTexture = mRenderTexture;
            }
            mVideoHolder.texture = mRenderTexture;
        }

        public void SetVolume(float volume)
        {
            if (mAudioSource != null) mAudioSource.volume = volume;
        }

        public bool HasVideo() { return !string.IsNullOrEmpty(VideoData.VideoFileDownloaded); }
        public bool HasImage() { return !string.IsNullOrEmpty(VideoData.ImageFileDownloaded); }

        public void ShowImage(bool cache)
        {
            mVideoHolder.texture = VideoData.GetImageTexture(cache);
        }

        public void PlayVideo(bool loop)
        {
            APromoManager.I.StartCoroutine(CRPlayThread(loop));
        }

        IEnumerator CRPlayThread(bool loop)
        {
            while (!mVideoPlayer.isPrepared) yield return null;
            mVideoPlayer.isLooping = loop;
            mVideoPlayer.Play();
        }

        public bool IsPlaying() { return mVideoPlayer.isPlaying; }

        public void Close(bool freePlayer, bool freeAudio, bool freeRenderTexture)
        {
            mVideoPlayer?.Stop();
            mAudioSource?.Stop();

            if (freePlayer)
            {
                GameObject.Destroy(mVideoPlayer);
                mVideoPlayer = null;
            }

            if (freeAudio)
            { 
                GameObject.Destroy(mAudioSource);
                mAudioSource = null;
            }

            if (freeRenderTexture)
            {
                GameObject.Destroy(mRenderTexture);
                mRenderTexture = null;
            }

            mVideoHolder = null;
            VideoData = null;
            mEndCallback = null;
        }

        void OnLoopPointReached(UnityEngine.Video.VideoPlayer source)
        {
            if (mEndCallback != null) mEndCallback();
        }
    }

    public class APromoManager : AFramework.SingletonMono<APromoManager>
    {
        public const string LEVEL_KEY = "max_stage";

        public static System.Action EventOnDataChanged;
        public static System.Action EventOnDataDownloaded;

        const string PromoTempFile = "promodownload.temp";
        const string VideoFileExtension = "mp4";
        const string ImageFileExtension = "api";

        PromoData[] allPromoData = null;
        public virtual bool IsDownloading { get { return mDownloadThread != null; } }
        public float ErrorWaitTime = 3;
        //public float ShowWaitTime = 0.1f;

        protected IEnumerator mDownloadThread = null;
        protected UnityWebRequest mWebRequest = null;
        protected Dictionary<string, List<PromoData>> mLocationToPromoDataList = new Dictionary<string, List<PromoData>>();

        //protected override void Awake()
        //{
        //    base.Awake();//must keep this
        //}

        public virtual bool IsSafeToDownload() { return true; }
        public virtual bool IsSafeToShow() { return true; }
        public virtual bool IsDataReady(string location)
        {
            if (allPromoData == null) return false;
            if (!mLocationToPromoDataList.ContainsKey(location)) return false;
            var locationData = mLocationToPromoDataList[location];
            var current_max_stage = PlayerPrefs.GetInt(LEVEL_KEY, 0);
            for(int i = 0; i < locationData.Count; ++i)
            {
                if (current_max_stage < locationData[i].max_stage) continue;
                if (!string.IsNullOrEmpty(locationData[i].VideoFileDownloaded)) return true;
                if (!string.IsNullOrEmpty(locationData[i].ImageFileDownloaded)) return true;
            }
            return false;
        }

        public virtual bool OnReceivePromoData(string json)
        {
            PromoData[] newData = null;
            try
            {
                newData = JsonHelper.getJsonArray<PromoData>(json);
            }
            catch (System.Exception e)
            {
                newData = null;
            }

            allPromoData = newData;
            if (mDownloadThread != null)
            {
                StopCoroutine(mDownloadThread);
            }
            if (allPromoData == null) return true;

            {
                var newMappingList = new Dictionary<string, List<PromoData>>();
                for (int i = 0; i < allPromoData.Length; ++i)
                {
                    if (!newMappingList.ContainsKey(allPromoData[i].location))
                    {
                        newMappingList[allPromoData[i].location] = new List<PromoData>();
                    }
                    newMappingList[allPromoData[i].location].Add(allPromoData[i]);
                }
                mLocationToPromoDataList = newMappingList;
            }

            mDownloadThread = CRDownloadAll();
            StartCoroutine(mDownloadThread);
            if (EventOnDataChanged != null) EventOnDataChanged();
            return true;
        }

        protected IEnumerator CRDownloadAll()
        {
            if (allPromoData == null)
            {
                yield break;
            }

            string savepath = AFramework.Utility.GetSavePath();
            var tempFilePath = savepath + PromoTempFile;
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
                yield return null;
            }

            List<string> uniqueExtensionList = new List<string>();
            uniqueExtensionList.Add(VideoFileExtension);
            uniqueExtensionList.Add(ImageFileExtension);

            //simple check first, will write correctly if data array is used
            List<string> nameList = new List<string>();
            List<string> extensionList = new List<string>();
            List<string> urlList = new List<string>();
            List<string> deleteFiles = new List<string>();

            for (int index = 0; index < allPromoData.Length; ++index)
            {
                var data_request = allPromoData[index];
                {
                    var fileName = Utility.GetUrlFilename(data_request.videoUrl);
                    extensionList.Add(VideoFileExtension);
                    if (fileName != null)
                    {
                        nameList.Add(fileName.Remove(fileName.IndexOf('.')));
                        urlList.Add(data_request.videoUrl);
                    }
                    else
                    {
                        nameList.Add(null);
                        urlList.Add(null);
                    }
                }
                {
                    var fileName = Utility.GetUrlFilename(data_request.imageUrl);
                    extensionList.Add(ImageFileExtension);
                    if (fileName != null)
                    {
                        nameList.Add(fileName.Remove(fileName.IndexOf('.')));
                        urlList.Add(data_request.imageUrl);
                    }
                    else
                    {
                        nameList.Add(null);
                        urlList.Add(null);
                    }
                }
            }

            //search and delete old files
            for (int i = 0; i < uniqueExtensionList.Count; ++i)
            {
                string[] files = System.IO.Directory.GetFiles(savepath, "*." + uniqueExtensionList[i]);
                yield return null;
                if (files == null || files.Length <= 0) continue;

                for (int j = 0; j < files.Length; ++j)
                {
                    bool fileNeeded = false;
                    int fileNameHash = -1;
                    int.TryParse(Path.GetFileNameWithoutExtension(files[j]), out fileNameHash);
                    for (int k = 0; k < nameList.Count; ++k)
                    {
                        if (nameList[k] == null || extensionList[k] != uniqueExtensionList[i]) continue;
                        if (nameList[k].GetHashCode() == fileNameHash)
                        {
                            fileNeeded = true;
                            break;
                        }
                    }

                    if (!fileNeeded)
                    {
                        deleteFiles.Add(files[j]);
                    }
                }
            }

            for (int index = 0; index < allPromoData.Length; ++index)
            {
                if (mWebRequest != null) yield return new WaitUntil(() => mWebRequest.isDone);
                mWebRequest = null;
                yield return new WaitUntil(() => IsSafeToDownload());

                var data_request = allPromoData[index];   

                //download file
                var waitSafeCondition = new WaitUntil(() => IsSafeToDownload() && Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork);
                var waitError = new WaitForSeconds(ErrorWaitTime);

                int downloadStartIndex = index * 2;
                for (int i = downloadStartIndex; i < nameList.Count && i < downloadStartIndex + 2; ++i)
                {
                    if (nameList[i] == null) continue;
                    yield return waitSafeCondition;
                    var saveFilePath = savepath + nameList[i].GetHashCode() + "." + extensionList[i];
                    if (File.Exists(saveFilePath))
                    {
                        continue;
                    }

                    mWebRequest = UnityWebRequest.Get(urlList[i]);
                    mWebRequest.method = UnityWebRequest.kHttpVerbGET;
                    DownloadHandlerFile dlh = new DownloadHandlerFile(tempFilePath);
                    mWebRequest.downloadHandler = dlh;
                    dlh.removeFileOnAbort = true;
                    yield return mWebRequest.SendWebRequest();
                    while (!mWebRequest.isDone)
                    {
                        yield return null;
                    }

                    if (mWebRequest.isHttpError || mWebRequest.isNetworkError)
                    {
                        mWebRequest = null;
                        yield return waitError;
                    }
                    else
                    {
                        mWebRequest = null;
                        yield return null;//wait a frame to make sure file is ready;
                        if (File.Exists(tempFilePath))
                        {
                            File.Move(tempFilePath, saveFilePath);
                            yield return null;
                        }
                    }
                }

                mDownloadThread = null;

                if (!string.IsNullOrEmpty(nameList[downloadStartIndex])) data_request.VideoFileDownloaded = savepath + nameList[downloadStartIndex].GetHashCode() + "." + extensionList[downloadStartIndex];
                if (nameList.Count > downloadStartIndex + 1 && !string.IsNullOrEmpty(nameList[downloadStartIndex + 1]))
                {
                    data_request.ImageFileDownloaded = savepath + nameList[downloadStartIndex + 1].GetHashCode() + "." + extensionList[downloadStartIndex + 1];
                }
            }

            for (int i = 0; i < deleteFiles.Count; ++i)
            {
                File.Delete(deleteFiles[i]);
                yield return null;
            }
        }

        public VideoHandler PlayVideo(RawImage videoHolder, string location, System.Action endCallback, RenderTexture renderTexture = null)
        {
            var showData = GetPromoDataForLocation(location);
            if (showData == null) return null;
            return new VideoHandler(videoHolder, showData, endCallback, renderTexture);
        }

        public void PlayVideoFullscreen(string location, System.Action callback, float safeStateWaitTime = 0f)
        {
            var showData = GetPromoDataForLocation(location);
            if (showData == null) return;
            var backupCallback = callback;
            StartCoroutine(SimpleCRPlayPromo(showData, backupCallback, safeStateWaitTime));
        }

        IEnumerator SimpleCRPlayPromo(PromoData showData, System.Action callback, float safeStateWaitTime = 0f)
        {
            var backupCallback = callback;
            if (safeStateWaitTime > 0)
            {
                while (!IsSafeToShow())
                {
                    yield return null;
                    safeStateWaitTime -= Time.deltaTime;
                }
            }
            if (!IsSafeToShow())
            {
                Debug.LogWarning("Current state does not safe to show cross promo");
                yield break;
            }
            AFramework.UI.CanvasManager.ShowSystemLoadingPopup(true);
            yield return null;
#if UNITY_IOS || UNITY_ANDROID
            Handheld.PlayFullScreenMovie(showData.videoUrl, Color.black, FullScreenMovieControlMode.CancelOnInput);
#endif
            yield return null;
            AFramework.UI.CanvasManager.ShowSystemLoadingPopup(false);
            if (backupCallback != null) backupCallback();
        }

        PromoData GetPromoDataForLocation(string location)
        {
            if (!IsDataReady(location)) return null;
            var current_max_stage = PlayerPrefs.GetInt(LEVEL_KEY, 0);
            var currentList = mLocationToPromoDataList[location];
            List<PromoData> filterList = new List<PromoData>();
            for (int i = 0; i < currentList.Count; ++i)
            {
                if (current_max_stage < currentList[i].max_stage) continue;
                filterList.Add(currentList[i]);
            }
            
            return filterList.Count > 0 ? filterList[Random.Range(0, filterList.Count)] : null;
        }
    }
}