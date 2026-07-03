using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AFramework
{
    public class FrameworkBuildPreProcess
#if UNITY_ANDROID || UNITY_IOS
        : IPreprocessBuildWithReport
#endif
    {
        public int callbackOrder { get { return 100; } }
#if UNITY_ANDROID
        private const string META_APPLICATION_ID = "com.google.android.gms.ads.APPLICATION_ID";
        private const string AD_ID_PERMISSION_ATTR = "com.google.android.gms.permission.AD_ID";
        private const string MANIFEST_PERMISSION = "uses-permission";
        private const string MANIFEST_META_DATA = "meta-data";
        private const string META_APPLOVIN_KEY = "applovin.sdk.key";
        private XNamespace ns = "http://schemas.android.com/apk/res/android";

        public void OnPreprocessBuild(BuildReport report)
        {
            string manifestPath = Path.Combine(
                    Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            XDocument manifest = null;
            try
            {
                manifest = XDocument.Load(manifestPath);
            }
#pragma warning disable 0168
            catch (IOException e)
#pragma warning restore 0168
            {
                Debug.LogError("AndroidManifest.xml is missing. Try re-importing the plugin.");
            }

            XElement elemManifest = manifest.Element("manifest");
            if (elemManifest == null)
            {
                Debug.LogError("AndroidManifest.xml is not valid. Try re-importing the plugin.");
            }

            XElement elemApplication = elemManifest.Element("application");
            if (elemApplication == null)
            {
                Debug.LogError("AndroidManifest.xml is not valid. Try re-importing the plugin.");
            }

            IEnumerable<XElement> metas = elemApplication.Descendants().Where(elem => elem.Name.LocalName.Equals(MANIFEST_META_DATA));
            IEnumerable<XElement> permissons = elemManifest.Descendants().Where(elem => elem.Name.LocalName.Equals(MANIFEST_PERMISSION));

            if (FrameworkGlobalConfig.Instance != null && FrameworkGlobalConfig.Instance.AdsConfig != null)
            {
#if !USE_UNITY_ADS
                XElement elemAdMobEnabled = GetMetaElement(metas, META_APPLICATION_ID);
                if (!string.IsNullOrEmpty(FrameworkGlobalConfig.Instance.AdsConfig.Platform.AppId))
                {
                    string appId = FrameworkGlobalConfig.Instance.AdsConfig.Platform.AppId;

                    if (appId.Length == 0)
                    {
                        Debug.LogError(
                            "Android AdMob app ID is empty. Please enter a valid app ID to run ads properly.");
                    }

                    if (elemAdMobEnabled == null)
                    {
                        elemApplication.Add(CreateMetaElement(META_APPLICATION_ID, appId));
                    }
                    else
                    {
                        elemAdMobEnabled.SetAttributeValue(ns + "value", appId);
                    }
                }
                else
                {
                    if (elemAdMobEnabled != null)
                    {
                        elemAdMobEnabled.Remove();
                    }
                }
#endif

#if USE_IRONSOURCE_ADS
                if (UnityEditor.PlayerSettings.Android.targetSdkVersion > UnityEditor.AndroidSdkVersions.AndroidApiLevel30)
                {
                    if (GetPermissionElement(permissons, AD_ID_PERMISSION_ATTR) == null)
                    {
                        elemManifest.Add(CreatePermissionElement(AD_ID_PERMISSION_ATTR));
                    }
                }
                else if (GetPermissionElement(permissons, AD_ID_PERMISSION_ATTR) != null)
                {
                    GetPermissionElement(permissons, AD_ID_PERMISSION_ATTR).Remove();
                }
#endif

#if USE_ADMOB || USE_MOPUB_ADS
                XElement elemApplovinEnabled = GetMetaElement(metas, META_APPLOVIN_KEY);
                if (!string.IsNullOrEmpty(FrameworkGlobalConfig.Instance.AdsConfig.Platform.ApplovingSDKKey))
                {
                    string applovinSDKKey = FrameworkGlobalConfig.Instance.AdsConfig.Platform.ApplovingSDKKey;

                    if (applovinSDKKey.Length == 0)
                    {
                        Debug.LogError(
                            "Android Applovin SDK Key is empty. Please enter a valid SDK Key to run ads properly.");
                    }

                    if (elemApplovinEnabled == null)
                    {
                        elemApplication.Add(CreateMetaElement(META_APPLOVIN_KEY, applovinSDKKey));
                    }
                    else
                    {
                        elemApplovinEnabled.SetAttributeValue(ns + "value", applovinSDKKey);
                    }
                }
                else
                {
                    if (elemApplovinEnabled != null)
                    {
                        elemApplovinEnabled.Remove();
                    }
                }
#endif

#if USE_MOPUB_ADS
                (FrameworkGlobalConfig.Instance.AdsConfig as AFramework.Ads.MopubAdapterConfig).BuildMopubAdapterObj();
#endif
            }

            elemManifest.Save(manifestPath);
        }

        private XElement CreateMetaElement(string name, object value)
        {
            return new XElement(MANIFEST_META_DATA,
                    new XAttribute(ns + "name", name), new XAttribute(ns + "value", value));
        }

        private XElement CreatePermissionElement(string name)
        {
            return new XElement(MANIFEST_PERMISSION,
                    new XAttribute(ns + "name", name));
        }

        private XElement GetMetaElement(IEnumerable<XElement> metas, string metaName)
        {
            foreach (XElement elem in metas)
            {
                IEnumerable<XAttribute> attrs = elem.Attributes();
                foreach (XAttribute attr in attrs)
                {
                    if (attr.Name.Namespace.Equals(ns)
                            && attr.Name.LocalName.Equals("name") && attr.Value.Equals(metaName))
                    {
                        return elem;
                    }
                }
            }
            return null;
        }

        private XElement GetPermissionElement(IEnumerable<XElement> manifest, string permissionName)
        {

            foreach (XElement elem in manifest)
            {
                IEnumerable<XAttribute> attrs = elem.Attributes();
                foreach (XAttribute attr in attrs)
                {
                    if (attr.Name.Namespace.Equals(ns)
                            && attr.Name.LocalName.Equals("name") && attr.Value.Equals(permissionName))
                    {
                        return elem;
                    }
                }
            }
            return null;
        }

#elif UNITY_IOS
        public void OnPreprocessBuild(BuildReport report)
        {
            if (FrameworkGlobalConfig.Instance != null && FrameworkGlobalConfig.Instance.AdsConfig != null)
            {
#if USE_MOPUB_ADS
                (FrameworkGlobalConfig.Instance.AdsConfig as AFramework.Ads.MopubAdapterConfig).BuildMopubAdapterObj();
#endif
            }
        }
#endif
            }
}