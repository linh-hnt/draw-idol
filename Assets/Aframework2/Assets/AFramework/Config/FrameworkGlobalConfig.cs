using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework
{
    [System.Serializable]
    public class iOSAlternateIcon
    {
        [System.Serializable]
        public class AlternateIcon
        {
            public string iconName;
            public Texture2D source;
            public string sourcePath;
        }

        [SerializeField] Texture2D defaultIcon;
        [SerializeField] string defaultIconPath;
        [SerializeField] AlternateIcon[] alternativeIcons;

        [System.NonSerialized] AlternateIcon[] allIconList;
        public AlternateIcon[] AllIcons { get { 
                if (allIconList == null)
                {
                    var list = new List<AlternateIcon>();
                    list.Add(new AlternateIcon() { iconName = "", source = defaultIcon, sourcePath = defaultIconPath });
                    list.AddRange(alternativeIcons);
                    allIconList = list.ToArray();
                }
                return allIconList;
            } }

        public AlternateIcon[] AlternativeIcons { get { return alternativeIcons; } }

        public void SetIcon(string iconName)
        {
#if UNITY_EDITOR || USE_CHEAT
            PlayerPrefs.SetString("AppIcon.Name", iconName);
#endif
            AppIconChanger.iOS.SetAlternateIconName(iconName);
        }

        public string GetCurrentIcon()
        {
#if UNITY_EDITOR || USE_CHEAT
            return PlayerPrefs.GetString("AppIcon.Name", string.Empty);
#endif
            return AppIconChanger.iOS.AlternateIconName ?? string.Empty;
        }
    }

    public class FrameworkGlobalConfig : ScriptableObject
    {

        [SerializeField] AFramework.IAP.IAPPackages _IAPConfig;
        [SerializeField] AFramework.IAP.IAPPackages _IAPConfigVIP;
        public AFramework.IAP.IAPPackages IAPConfig
        {
            get
            {
#if PREMIUM
                return _IAPConfigVIP;
#else
                return _IAPConfig;
#endif
            }
        }

        [SerializeField] AFramework.Ads.BaseAdapterConfig _AdsConfig;
        [SerializeField] AFramework.Ads.BaseAdapterConfig _AdsConfigVIP;
        public AFramework.Ads.BaseAdapterConfig AdsConfig
        {
            get
            {
#if PREMIUM
                return _AdsConfigVIP;
#else
                return _AdsConfig;
#endif
            }
        }

        [SerializeField] bool _DisableBitcode = true;
        public bool DisableBitcode { get { return _DisableBitcode; } }

        public iOSATTConfig iosATT;

        [SerializeField] string[] _iOSAdditionalFramework;
        public string[] iOSAdditionalFramework {
            get { return _iOSAdditionalFramework; }
            set { _iOSAdditionalFramework = value; }
        }
        
        [SerializeField] string[] _iOSPodFramework;
        public string[] iOSPodFramework {
            get { return _iOSPodFramework; }
            set { _iOSPodFramework = value; }
        }
        [SerializeField] iOSAppIdData[] _iOSAppIds;
        public string GetiOSAppId(string packageId)
        {
            if (_iOSAppIds == null || _iOSAppIds.Length == 0) return string.Empty;
            for (int i = 0; i < _iOSAppIds.Length; ++i)
            {
                if (string.Equals(packageId, _iOSAppIds[i].packageId)) return _iOSAppIds[i].appId;
            }
            return string.Empty;
        }

        public KeyStringConfig[] iOSPListCustomkey;
        public iOSAlternateIcon iOSIcon;

        public string androidAFStore;

        [SerializeField] string[] _TesterLocalAddress;
        public static bool IsTester { get; protected set; }
#if UNITY_ANDROID
        static bool _IsTesterChecked = false;
        public void CheckTester()
        {
#if !UNITY_EDITOR_OSX
             if (_IsTesterChecked || _TesterLocalAddress == null) return;
            var localAddresses = AFramework.Utility.GetLocalIPAddress();
            for (int i = 0; i < _TesterLocalAddress.Length && !IsTester; ++i)
            {
                foreach(var address in localAddresses)
                {
                    if (address.Contains(_TesterLocalAddress[i]))
                    {
                        IsTester = true;
                        break;
                    }
                }
                
            }
            _IsTesterChecked = true;
#endif
        }
#endif

        public FrameworkGlobalConfig(AFramework.IAP.IAPPackages iap, AFramework.IAP.IAPPackages iapVip, AFramework.Ads.BaseAdapterConfig ads, AFramework.Ads.BaseAdapterConfig adsVIP)
        {
            _IAPConfig = iap;
            _IAPConfigVIP = iapVip;
            _AdsConfig = ads;
            _AdsConfigVIP = adsVIP;
        }

#if UNITY_EDITOR
        public iOSAppIdData[] iOSAppIds { get { return _iOSAppIds; } set { _iOSAppIds = value; } }

        private const string SettingsFile = "Assets/FrameworkGlobalConfig.asset";
        private static FrameworkGlobalConfig instance;
        public static FrameworkGlobalConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (FrameworkGlobalConfig)UnityEditor.AssetDatabase.LoadAssetAtPath(
                        SettingsFile, typeof(FrameworkGlobalConfig));

                    if (instance == null)
                    {
                        if (!System.IO.File.Exists(SettingsFile))
                        {
                            instance = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
                            UnityEditor.AssetDatabase.CreateAsset(instance, SettingsFile);
                            Debug.LogError("Could not find " + System.IO.Path.GetFullPath(SettingsFile) + " - Create new one");
                        }
                    }
                }
                return instance;
            }
        }
#endif
    }
}