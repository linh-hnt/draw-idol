using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AFramework
{
    public class APlayerFilter : MonoBehaviour
    {
        public SystemLanguage[] RestrictLanguages = new SystemLanguage[] { SystemLanguage.Chinese, SystemLanguage.ChineseSimplified, SystemLanguage.ChineseTraditional };
        public string[] RestrictRegions = new string[] { "cn", "zh" };
        public string[] RestrictRegionsByIP = new string[] { "cn" };
        public string IPSite = "https://freegeoip.app/json/"; 

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(CRCheckThread());
        }

        IEnumerator CRCheckThread()
        {
            //check language
            if (RestrictLanguages.Length > 0)
            {
                var language = Application.systemLanguage;
                for (int i = 0; i < RestrictLanguages.Length; ++i)
                {
                    if (language == RestrictLanguages[i]) yield break;
                }
            }

            //check region
            if (RestrictRegions.Length > 0)
            {
                try
                {
                    var region = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLower();
                    for (int i = 0; i < RestrictRegions.Length; ++i)
                    {
                        if (region.Contains(RestrictRegions[i])) yield break;
                    }
                }
                catch (System.Exception e) { }
            }

            //check ip
            if (RestrictRegionsByIP.Length > 0)
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    var www = UnityEngine.Networking.UnityWebRequest.Get(IPSite);
                    yield return www.SendWebRequest();
                    while (!www.isDone) yield return null;
                    try
                    {
                        object deserializeResult = null;
                        var downloadText = www.downloadHandler.text;
                        if (!string.IsNullOrEmpty(downloadText))
                        {
#if UNITY_PURCHASING || SIS_IAP
                            deserializeResult = UnityEngine.Purchasing.MiniJSON.Json.Deserialize(downloadText);
#else
                            deserializeResult = AFramework.MiniJSON.Json.Deserialize(downloadText);
#endif
                        }

                        Dictionary<string, object> result = null;
                        if (deserializeResult != null && deserializeResult is Dictionary<string, object>)
                        {
                            result = deserializeResult as Dictionary<string, object>;
                        }
                        if (result != null && result.ContainsKey("country_code"))
                        {
                            var country_code = result["country_code"].ToString().ToLower();
                            for (int i = 0; i < RestrictRegionsByIP.Length; ++i)
                            {
                                if (country_code.Contains(RestrictRegionsByIP[i])) yield break;
                            }
                        }
                    }
                    catch (System.Exception e) { }
                }
            }

            //seem ok
            AsyncOperation loadScene = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        }
    }
}