using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AFramework.Promo
{
    [RequireComponent(typeof(RawImage), typeof(Button))]
    public class APromoPlayer : MonoBehaviour
    {
        public string location = "";
        public bool sound = false;
        public bool loop = false;
        public bool autoPlay = false;
        public bool freeWhenClose = true;
        public Vector2 cacheRenderTextureSize = Vector2.zero;

        VideoHandler mVideoHandler;
        RawImage mRawImage;
        Button mLinkButton;
        RenderTexture mRenderTextureCache = null;

        protected virtual void Awake()
        {
            mLinkButton = this.GetComponent<Button>();
            mLinkButton.onClick.AddListener(OnClicked);

            mRawImage = this.GetComponent<RawImage>();
        }

        protected virtual void OnEnable()
        {
            if (autoPlay)
            {
                Play();
            }
        }

        protected virtual void OnDisable()
        {
            if (autoPlay)
            {
                Stop();
            }
        }

        public virtual void Play()
        {
            mRawImage.enabled = false;
            if (mRenderTextureCache == null && cacheRenderTextureSize.x > 0 && cacheRenderTextureSize.y > 0)
            {
                mRenderTextureCache = new RenderTexture(Mathf.RoundToInt(cacheRenderTextureSize.x), Mathf.RoundToInt(cacheRenderTextureSize.y), 16, RenderTextureFormat.ARGB32);
            }
            mVideoHandler = APromoManager.I.PlayVideo(mRawImage, location, loop ? null : (System.Action)OnPlayFinished, mRenderTextureCache);
            if (mVideoHandler == null)
            {
                return;
            }
            mVideoHandler.SetVolume(sound ? 1.0f : 0f);
            if (mVideoHandler.HasVideo())
            {
                mRawImage.enabled = true;
                mVideoHandler.PlayVideo(loop);
                HandlePromoShown(mVideoHandler.VideoData.promoUrl, location);
            }
            else if (mVideoHandler.HasImage())
            {
                mRawImage.enabled = true;
                mVideoHandler.ShowImage(false);
                HandlePromoShown(mVideoHandler.VideoData.promoUrl, location);
            }
        }

        public virtual void Stop()
        {
            mRawImage.enabled = false;
            if (freeWhenClose)
            {
                mVideoHandler?.Close(true, true, true);
                mRenderTextureCache = null;
            }
            else
            {
                mVideoHandler?.Close(false, false, false);
            }
        }

        protected virtual void OnPlayFinished()
        {
            if (mVideoHandler == null) return;
            if (mVideoHandler.HasImage())
            {
                mRawImage.enabled = true;
                mVideoHandler.ShowImage(false);
            }
        }

        protected virtual void OnClicked()
        {
            if (mVideoHandler == null) return;
            HandlePromoClicked(mVideoHandler.VideoData.promoUrl, location);
        }

        public static void HandlePromoShown(string urlData, string location)
        {
            if (urlData.StartsWith("game:"))
            {
#if USE_APPSFLYER_ANALYTICS
                var str = urlData.Substring(urlData.IndexOf("game:") + 5);
                CrossPromoteImpresstion(str, location);
#endif
            }
        }

        public static void HandlePromoClicked(string urlData, string location)
        {
            if (urlData.StartsWith("game:"))
            {
                var str = urlData.Substring(urlData.IndexOf("game:") + 5);
#if USE_APPSFLYER_ANALYTICS
                TrackAndOpenStore(str, location);
#else
#if UNITY_ANDROID
                Application.OpenURL("market://details?id=" + str);
#elif UNITY_IOS
                Application.OpenURL("itms-apps://itunes.apple.com/app/id" + str);
#endif
#endif
            }
            else if (urlData.StartsWith("link:"))
            {
                Application.OpenURL(urlData.Substring(urlData.IndexOf("link:") + 5));
            }
        }

#if USE_APPSFLYER_ANALYTICS
        static public void CrossPromoteImpresstion(string promotedAppID, string campaign)
        {
#if !UNITY_EDITOR
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            AppsFlyerSDK.AppsFlyer.recordCrossPromoteImpression(promotedAppID, campaign, parameters);
#endif
        }

        static public void TrackAndOpenStore(string promoteAppID, string campaign)
        {
#if !UNITY_EDITOR
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            AppsFlyerSDK.AppsFlyer.attributeAndOpenStore(promoteAppID, campaign, parameters, AFramework.Analytics.TrackingManager.appsflyerObject);
#endif
        }
#endif
    }
}