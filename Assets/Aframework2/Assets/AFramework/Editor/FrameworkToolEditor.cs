using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using marijnz.EditorCoroutines;

namespace AFramework
{
    [InitializeOnLoad]
    public class FrameworkStartup
    {
        static FrameworkStartup()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(FrameworkToolEditor.BuildProcessCheck);

            EditorApplication.playModeStateChanged -= FrameworkToolEditor.OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += FrameworkToolEditor.OnPlayModeStateChanged;

            var isFirstLoad = SessionState.GetBool("FrameworkStartup", true);
            if (!isFirstLoad) return;
            SessionState.SetBool("FrameworkStartup", false);

            //try to update ios app id
            FrameworkToolEditor.CheckiOSAppId();
        }
    }

    [ExecuteInEditMode]
    public static class FrameworkToolEditor
    {
        [MenuItem("AFramework/Goto Save")]
        public static void GoToSaveDir()
        {
            EditorUtility.RevealInFinder(Utility.GetSavePath());
        }

        [MenuItem("AFramework/Delete Save")]
        public static void ClearSave()
        {
            AFramework.SaveGameManager.I.DeleteAll();
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("AFramework/Update FirebaseConfig")]
        public static void ForceUpdateFirebaseConfig()
        {
            PlayerPrefs.SetInt("ForceUpdateFirebaseConfig", 1);
        }

        public static string GetAFrameworkPath()
        {
            string fileName = "abcdef_lib_indicator";
            var targets = AssetDatabase.FindAssets(fileName);
            if (targets.Length <= 0)
            {
                Debug.LogError("Could not found indicator for path");
                return null;
            }
            var temp = AssetDatabase.GUIDToAssetPath(targets[0]);
            return temp.Substring(0, temp.LastIndexOf('/'));
        }

        public static void BuildProcessCheck(BuildPlayerOptions options)
        {
            if (!CheckAds())
            {
                throw new UnityEditor.Build.BuildFailedException("Fail ads check");
            }

            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }

        [MenuItem("AFramework/Check iOSId")]
        public static void CheckiOSAppId()
        {
            if (!Utility.HasInternet())
            {
                Debug.LogError("CheckiOSAppId fail, no internet");
                return;
            }
            var downloadData = AFramework.Utility.GetGoogleSheetCSVData("1RhxPZ2oj-lMBKJo05aaffmc4sZMEUmuXYOZUrBTTwgw", "0");
            if (string.IsNullOrEmpty(downloadData))
            {
                Debug.LogError("CheckiOSAppId fail, no data downloaded");
                return;
            }
            var allAppIdData = ParseiOSAppIdData(downloadData);
            var iOSIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            if (allAppIdData != null && allAppIdData.Count > 0 && allAppIdData.ContainsKey(iOSIdentifier))
            {
                List<iOSAppIdData> currentList = new List<iOSAppIdData>();
                if (FrameworkGlobalConfig.Instance.iOSAppIds != null && FrameworkGlobalConfig.Instance.iOSAppIds.Length > 0) currentList.AddRange(FrameworkGlobalConfig.Instance.iOSAppIds);
                bool found = false;
                for (int i = 0; i < currentList.Count; ++i)
                {
                    if (string.Equals(currentList[i].packageId, iOSIdentifier))
                    {
                        if (currentList[i].appId != allAppIdData[iOSIdentifier])
                        {
                            currentList[i].appId = allAppIdData[iOSIdentifier];

                            UnityEditor.EditorUtility.SetDirty(FrameworkGlobalConfig.Instance);
                            UnityEditor.AssetDatabase.SaveAssets();
                            UnityEditor.AssetDatabase.Refresh();
                        }
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    currentList.Add(new iOSAppIdData(iOSIdentifier, allAppIdData[iOSIdentifier]));
                    FrameworkGlobalConfig.Instance.iOSAppIds = currentList.ToArray();

                    UnityEditor.EditorUtility.SetDirty(FrameworkGlobalConfig.Instance);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
            else
            {
                Debug.LogError("[ERROR] Could not find iOS appId for " + iOSIdentifier);
            }
        }

        static Dictionary<string, string> ParseiOSAppIdData(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            string[] lines = input.Split("\n"[0]);

            if (lines == null || lines.Length == 0)
            {
                return null;
            }

            int comma1Index = lines[0].IndexOf(',');
            int comma2Index = lines[0].IndexOf(';');
            char defaultComma = ',';
            if (comma1Index < 0) defaultComma = ';';
            else if (comma2Index < 0) defaultComma = ',';
            else if (comma2Index < comma1Index) defaultComma = ';';
            string[] param_num = lines[0].Split(defaultComma);
            System.Text.RegularExpressions.Regex CSVParser = new System.Text.RegularExpressions.Regex(defaultComma + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            Dictionary<string, string> resultData = new Dictionary<string, string>();
            {
                for (int i = 1; i < lines.Length; ++i)
                {
                    try
                    {
                        string[] datas = CSVParser.Split(lines[i]);
                        iOSAppIdData info = new iOSAppIdData();
                        int validateInfo = 0;

                        for (int param_index = 0; param_index < param_num.Length; ++param_index)
                        {
                            var value = datas[param_index].Trim();
                            var param_name = param_num[param_index].Trim();

                            switch (param_name)
                            {
                                case "Package Id":
                                    {
                                        info.packageId = value;
                                        ++validateInfo;
                                    }
                                    break;
                                case "App Id":
                                    {
                                        info.appId = value;
                                    }
                                    break;
                            }
                        }
                        if (validateInfo == 2) resultData[info.packageId] = info.appId;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Config exception at: " + lines[i]);
                        Debug.LogError(e.StackTrace);
                    }
                }
            }
            return resultData;
        }

#region Ads Check
        [MenuItem("AFramework/Check Ads")]
        public static bool CheckAds()
        {
            if (!(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)) return true;
            string mediation_name = "";
            string platform = "";
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                platform = EditorUserBuildSettings.activeBuildTarget.ToString();
            }
            else
            {
                Debug.LogWarning("Not Support Platform");
                return true;
            }

#if USE_IRONSOURCE_ADS
            mediation_name = "Ironsource";
#elif USE_APPLOVIN_ADS
            mediation_name = "MAX";
#else
            Debug.LogWarning("Not Support Ads Flag");
            return true;
#endif

            string appIdentifier = PlayerSettings.GetApplicationIdentifier(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS);
            string saveName = platform + "_" + mediation_name + "_" + appIdentifier.Replace('.', '_');
            string downloadData = null;
            if (AFramework.Utility.HasInternet()) downloadData = AFramework.Utility.GetGoogleSheetCSVData("1RhxPZ2oj-lMBKJo05aaffmc4sZMEUmuXYOZUrBTTwgw", EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "798513210" : "750284004");
            string listAdapters = null;
            bool foundOnlineData = false;
            if (string.IsNullOrEmpty(downloadData))//if no data then try to get cache
            {
                listAdapters = PlayerPrefs.GetString(saveName, "");
            }
            else
            {
                string[] lines = downloadData.Split("\n"[0]);

                int comma1Index = lines[0].IndexOf(',');
                int comma2Index = lines[0].IndexOf(';');
                char defaultComma = ',';
                if (comma1Index < 0) defaultComma = ';';
                else if (comma2Index < 0) defaultComma = ',';
                else if (comma2Index < comma1Index) defaultComma = ';';
                string[] param_num = lines[0].Split(defaultComma);
                System.Text.RegularExpressions.Regex CSVParser = new System.Text.RegularExpressions.Regex(defaultComma + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                string onlineMediation = "";
                string onlineAdapters = "";
                {
                    for (int i = 1; i < lines.Length && !foundOnlineData; ++i)
                    {
                        try
                        {
                            string[] datas = CSVParser.Split(lines[i]);

                            for (int param_index = 0; param_index < param_num.Length; ++param_index)
                            {
                                var value = datas[param_index].Trim();
                                var param_name = param_num[param_index].Trim();

                                switch (param_name)
                                {
                                    case "PackageName":
                                        {
                                            if (appIdentifier.Equals(value))
                                            {
                                                foundOnlineData = true;
                                            }
                                        }
                                        break;
                                    case "Mediation":
                                        {
                                            if (foundOnlineData) onlineMediation = value;
                                        }
                                        break;
                                    case "Adapters":
                                        {
                                            if (foundOnlineData) onlineAdapters = value;
                                            param_index = 9999999;//end row
                                        }
                                        break;
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("Config exception at: " + lines[i]);
                            Debug.LogError(e.StackTrace);
                        }
                    }
                }

                if (!foundOnlineData)
                {
                    Debug.LogWarning("Allow pass Ads Check because there is no online data!!!");
                    return true;
                }
                else if (!onlineMediation.Equals(mediation_name))
                {
                    EditorUtility.DisplayDialog("Need Mediation", onlineMediation, "Ok");
                    return false;
                }
                else
                {
                    listAdapters = onlineAdapters;
                }
            }
            if (string.IsNullOrEmpty(listAdapters))
            {
                listAdapters = PlayerPrefs.GetString(saveName);
            }

            if (string.IsNullOrEmpty(listAdapters))
            {
                if (true)//!foundOnlineData)
                {
                    Debug.LogWarning("Allow pass Ads Check because there is no online data!!!");
                    return true;
                }
                else
                {
                    Debug.LogError("Fail to verify Ads Adapter!!!");
                    return false;
                }
            }

            PlayerPrefs.SetString(saveName, listAdapters);

            if (VerifyMediation(listAdapters.Split(';')))
            {
                Debug.Log("Ads adapter verify successfull");
                return true;
            }

            EditorUtility.DisplayDialog("Ads Adapter Error", listAdapters, "Ok");

            return false;
        }

        static bool  VerifyMediation(string[] adapters)
        {
            bool result = true;
            for (int i = 0; result && i < adapters.Length; ++i)
            {
                result &= IsMediationAdapterAvailable(adapters[i]);
            }
            return result;
        }

        static string sApplovinMediationPath = null;
        public static bool IsMediationAdapterAvailable(string adapter)
        {
#if USE_IRONSOURCE_ADS
            string[] findResult = null;
            switch (adapter)
            {
                case "Ironsource": return true;
                case "AdColony": findResult = AssetDatabase.FindAssets("ISAdColonyAdapterDependencies"); break;
                case "Applovin": findResult = AssetDatabase.FindAssets("ISAppLovinAdapterDependencies"); break;
                case "APS": findResult = AssetDatabase.FindAssets("ISAPSAdapterDependencies"); break;
                case "Chartboost": findResult = AssetDatabase.FindAssets("ISChartboostAdapterDependencies"); break;
                case "CSJ": findResult = AssetDatabase.FindAssets("ISChartboostAdapterDependencies"); break;
                case "Fyber": findResult = AssetDatabase.FindAssets("ISFyberAdapterDependencies"); break;
                case "AdMob": findResult = AssetDatabase.FindAssets("ISAdMobAdapterDependencies"); break;
                case "HyprMX": findResult = AssetDatabase.FindAssets("ISHyprMXAdapterDependencies"); break;
                case "InMobi": findResult = AssetDatabase.FindAssets("ISInMobiAdapterDependencies"); break;
                case "Liftoff": findResult = AssetDatabase.FindAssets("ISLiftoffAdapterDependencies"); break;
                case "Maio": findResult = AssetDatabase.FindAssets("ISMaioAdapterDependencies"); break;
                case "Facebook": findResult = AssetDatabase.FindAssets("ISFacebookAdapterDependencies"); break;
                case "Mintegral": findResult = AssetDatabase.FindAssets("ISMintegralAdapterDependencies"); break;
                case "MyTarget": findResult = AssetDatabase.FindAssets("ISMyTargetAdapterDependencies"); break;
                case "Pangle": findResult = AssetDatabase.FindAssets("ISPangleAdapterDependencies"); break;
                case "Smaato": findResult = AssetDatabase.FindAssets("ISSmaatoAdapterDependencies"); break;
                case "Tapjoy": findResult = AssetDatabase.FindAssets("ISTapJoyAdapterDependencies"); break;
                case "Unity": findResult = AssetDatabase.FindAssets("ISUnityAdsAdapterDependencies"); break;
                case "Vungle": findResult = AssetDatabase.FindAssets("ISVungleAdapterDependencies"); break;
                case "Yahoo": findResult = AssetDatabase.FindAssets("ISYahooAdapterDependencies"); break;

                //custom adapter, Asset name may not correct
                case "Bigo": findResult = AssetDatabase.FindAssets("ISBigoAdapterDependencies"); break;
                case "Yandex": findResult = AssetDatabase.FindAssets("IronSourceMobileadsMediationDependencies"); break;
            }
            return (findResult != null && findResult.Length > 0);
#elif USE_APPLOVIN_ADS
            if (sApplovinMediationPath == null)
            {
                var applovinSettingAsset = AssetDatabase.FindAssets("AppLovinSettings");
                if (applovinSettingAsset.Length <= 0)
                {
                    Debug.LogError("Could not found Applovin indicator for path");
                    return false;
                }
                var path = AssetDatabase.GUIDToAssetPath(applovinSettingAsset[0]);
                sApplovinMediationPath = path.Substring(0, path.LastIndexOf('/')) + "/../Mediation/";
            }

            switch (adapter)
            {
                case "AdColony":
                    return System.IO.File.Exists(sApplovinMediationPath + "AdColony/Editor/Dependencies.xml");
                case "Amazon":
                    return System.IO.File.Exists(sApplovinMediationPath + "Amazon/Editor/Dependencies.xml");
                case "Chartboost":
                    return System.IO.File.Exists(sApplovinMediationPath + "Chartboost/Editor/Dependencies.xml");
                case "Facebook":
                    return System.IO.File.Exists(sApplovinMediationPath + "Facebook/Editor/Dependencies.xml");
                case "Fyber":
                    return System.IO.File.Exists(sApplovinMediationPath + "Fyber/Editor/Dependencies.xml");
                case "Admob":
                    return System.IO.File.Exists(sApplovinMediationPath + "Google/Editor/Dependencies.xml");
                case "HyprMX":
                    return System.IO.File.Exists(sApplovinMediationPath + "HyprMX/Editor/Dependencies.xml");
                case "InMobi":
                    return System.IO.File.Exists(sApplovinMediationPath + "InMobi/Editor/Dependencies.xml");
                case "Ironsource":
                    return System.IO.File.Exists(sApplovinMediationPath + "IronSource/Editor/Dependencies.xml");
                case "Maio":
                    return System.IO.File.Exists(sApplovinMediationPath + "Maio/Editor/Dependencies.xml");
                case "Mintegral":
                    return System.IO.File.Exists(sApplovinMediationPath + "Mintegral/Editor/Dependencies.xml");
                case "MyTarget":
                    return System.IO.File.Exists(sApplovinMediationPath + "MyTarget/Editor/Dependencies.xml");
                case "Nend":
                    return System.IO.File.Exists(sApplovinMediationPath + "Nend/Editor/Dependencies.xml");
                case "Ogury":
                    return System.IO.File.Exists(sApplovinMediationPath + "OguryPresage/Editor/Dependencies.xml");
                case "Smaato":
                    return System.IO.File.Exists(sApplovinMediationPath + "Smaato/Editor/Dependencies.xml");
                case "Tapjoy":
                    return System.IO.File.Exists(sApplovinMediationPath + "Tapjoy/Editor/Dependencies.xml");
                case "Tencent":
                    return System.IO.File.Exists(sApplovinMediationPath + "TencentGDT/Editor/Dependencies.xml");
                case "Pangle":
                    return System.IO.File.Exists(sApplovinMediationPath + "ByteDance/Editor/Dependencies.xml");
                case "Unity":
                    return System.IO.File.Exists(sApplovinMediationPath + "UnityAds/Editor/Dependencies.xml");
                case "Verizon":
                    return System.IO.File.Exists(sApplovinMediationPath + "VerizonAds/Editor/Dependencies.xml");
                case "Vungle":
                    return System.IO.File.Exists(sApplovinMediationPath + "Vungle/Editor/Dependencies.xml");
                case "Yandex":
                    return System.IO.File.Exists(sApplovinMediationPath + "Yandex/Editor/Dependencies.xml");
            }
#endif
            return false;
        }

        public static string[] GetSKAdNetworkIDs()
        {
            List<string> result = new List<string>();
            void AddToList(string str)
            {
                if (result.Contains(str)) return;
                result.Add(str);
            }
            //no need for applovin, they auto add
#if USE_IRONSOURCE_ADS
            string[] adapterList = new string[] {
                "AdColony", "AdMob", "Applovin", "APS",
                "Chartboost", "CSJ", "Facebook", "Fyber", "HyprMX",
                "InMobi", "Liftoff", "Ironsource", "Maio", "Mintegral",
                "MyTarget", "Pangle",
                "Smaato", "Tapjoy",
                "Unity", "Vungle", "Yahoo",

                //Custom Adapter
                "Bigo", "Yandex"
            };

            //https://www.skanids.com/
            //https://unityads.unity3d.com/help/ios/skadnetwork-ids

            for (int i = 0; i < adapterList.Length; ++i)
            {
                if (!IsMediationAdapterAvailable(adapterList[i])) continue;
                switch(adapterList[i])
                {
                    case "AdColony": 
                        AddToList("4pfyvq9l8r.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("v72qych5uu.skadnetwork");
                        AddToList("tl55sbb4fm.skadnetwork"); AddToList("t38b2kh725.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                        AddToList("ppxm28t8ap.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("9t245vhmpl.skadnetwork");
                        AddToList("9rd848q2bz.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork");
                        AddToList("4fzdc2evr5.skadnetwork"); AddToList("4468km3ulz.skadnetwork"); AddToList("3rd42ekr43.skadnetwork");
                        AddToList("2u9pt9hc89.skadnetwork"); AddToList("m8dbw4sv7c.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                        AddToList("ejvt5qm6ak.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("44jx6755aq.skadnetwork");
                        AddToList("mtkv5xtk9e.skadnetwork"); AddToList("6g9af3uyq4.skadnetwork"); AddToList("uw77j35x4d.skadnetwork");
                        AddToList("u679fj5vs4.skadnetwork"); AddToList("rx5hdcabgc.skadnetwork"); AddToList("g28c52eehv.skadnetwork");
                        AddToList("cg4yq2srnc.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork"); AddToList("275upjj5gd.skadnetwork");
                        AddToList("wg4vff78zm.skadnetwork"); AddToList("qqp299437r.skadnetwork"); AddToList("k674qkevps.skadnetwork");
                        break;
                    case "AdMob":
                        AddToList("2fnua5tdw4.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork"); AddToList("3qcr597p9d.skadnetwork");
                        AddToList("3qy4746246.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork"); AddToList("5a6flpkh64.skadnetwork");
                        AddToList("7ug5zh24hu.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("9rd848q2bz.skadnetwork");
                        AddToList("9t245vhmpl.skadnetwork"); AddToList("av6w8kgt66.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork");
                        AddToList("cstr6suwn9.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork"); AddToList("f38h382jlk.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("kbd757ywx3.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork");
                        AddToList("n6fk4nfna4.skadnetwork"); AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork");
                        AddToList("prcb7njmu6.skadnetwork"); AddToList("s39g8k73mm.skadnetwork"); AddToList("t38b2kh725.skadnetwork");
                        AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                        AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork"); AddToList("zq492l623r.skadnetwork");
                        break;
                    case "Applovin":
                        AddToList("24t9a8vw3c.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                        AddToList("32z4fx6l9h.skadnetwork"); AddToList("3qcr597p9d.skadnetwork"); AddToList("3rd42ekr43.skadnetwork");
                        AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork"); AddToList("4468km3ulz.skadnetwork");
                        AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork"); AddToList("523jb4fst2.skadnetwork");
                        AddToList("54nzkqm89y.skadnetwork"); AddToList("578prtvx9j.skadnetwork"); AddToList("5a6flpkh64.skadnetwork");
                        AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("6xzpu9s2p8.skadnetwork");
                        AddToList("79pbpufp6p.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork");
                        AddToList("8s468mfl3y.skadnetwork"); AddToList("9b89h5y424.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork");
                        AddToList("9rd848q2bz.skadnetwork"); AddToList("9t245vhmpl.skadnetwork"); AddToList("av6w8kgt66.skadnetwork");
                        AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork"); AddToList("cj5566h2ga.skadnetwork");
                        AddToList("cstr6suwn9.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork"); AddToList("f38h382jlk.skadnetwork");
                        AddToList("feyaarzu9v.skadnetwork"); AddToList("g28c52eehv.skadnetwork"); AddToList("ggvn48r87g.skadnetwork");
                        AddToList("glqzh8vgby.skadnetwork"); AddToList("gta9lk7p23.skadnetwork"); AddToList("hs6bdukanm.skadnetwork");
                        AddToList("k674qkevps.skadnetwork"); AddToList("kbd757ywx3.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork");
                        AddToList("ludvb6z3bs.skadnetwork"); AddToList("m8dbw4sv7c.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork");
                        AddToList("mtkv5xtk9e.skadnetwork"); AddToList("n9x2a789qt.skadnetwork"); AddToList("p78axxw29g.skadnetwork");
                        AddToList("ppxm28t8ap.skadnetwork"); AddToList("prcb7njmu6.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork");
                        AddToList("t38b2kh725.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork"); AddToList("uw77j35x4d.skadnetwork");
                        AddToList("v72qych5uu.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                        AddToList("xy9t38ct57.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork");
                        AddToList("zmvfpc5aq8.skadnetwork");
                        break;
                    case "APS": AddToList("p78axxw29g.skadnetwork"); break;
                    case "Chartboost":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork"); AddToList("275upjj5gd.skadnetwork");
                        AddToList("294l99pt4k.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                        AddToList("32z4fx6l9h.skadnetwork"); AddToList("3qcr597p9d.skadnetwork"); AddToList("3rd42ekr43.skadnetwork");
                        AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork"); AddToList("4468km3ulz.skadnetwork");
                        AddToList("44jx6755aq.skadnetwork"); AddToList("44n7hlldy6.skadnetwork"); AddToList("4dzt52r2t5.skadnetwork");
                        AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork"); AddToList("4w7y6s5ca2.skadnetwork");
                        AddToList("523jb4fst2.skadnetwork"); AddToList("54nzkqm89y.skadnetwork"); AddToList("578prtvx9j.skadnetwork");
                        AddToList("5a6flpkh64.skadnetwork"); AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork");
                        AddToList("5tjdwbrq8w.skadnetwork"); AddToList("6964rsfnh4.skadnetwork"); AddToList("6g9af3uyq4.skadnetwork");
                        AddToList("6p4ks3rnbw.skadnetwork"); AddToList("6xzpu9s2p8.skadnetwork"); AddToList("737z793b9f.skadnetwork");
                        AddToList("74b6s63p6l.skadnetwork"); AddToList("79pbpufp6p.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                        AddToList("7ug5zh24hu.skadnetwork"); AddToList("84993kbrcf.skadnetwork"); AddToList("8s468mfl3y.skadnetwork");
                        AddToList("97r2b46745.skadnetwork"); AddToList("9b89h5y424.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork");
                        AddToList("9rd848q2bz.skadnetwork"); AddToList("9t245vhmpl.skadnetwork"); AddToList("a7xqa6mtl2.skadnetwork");
                        AddToList("av6w8kgt66.skadnetwork"); AddToList("b9bk5wbcq9.skadnetwork"); AddToList("bxvub5ada5.skadnetwork");
                        AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork"); AddToList("cj5566h2ga.skadnetwork");
                        AddToList("cstr6suwn9.skadnetwork"); AddToList("dzg6xy7pwj.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork");
                        AddToList("ejvt5qm6ak.skadnetwork"); AddToList("f38h382jlk.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                        AddToList("feyaarzu9v.skadnetwork"); AddToList("g28c52eehv.skadnetwork"); AddToList("g2y4y55b64.skadnetwork");
                        AddToList("ggvn48r87g.skadnetwork"); AddToList("glqzh8vgby.skadnetwork"); AddToList("gta9lk7p23.skadnetwork");
                        AddToList("hdw39hrw9y.skadnetwork"); AddToList("hs6bdukanm.skadnetwork"); AddToList("k674qkevps.skadnetwork");
                        AddToList("kbd757ywx3.skadnetwork"); AddToList("kbmxgpxpgc.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork");
                        AddToList("lr83yxwka7.skadnetwork"); AddToList("ludvb6z3bs.skadnetwork"); AddToList("m8dbw4sv7c.skadnetwork");
                        AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork"); AddToList("mp6xlyr22a.skadnetwork");
                        AddToList("mtkv5xtk9e.skadnetwork"); AddToList("n6fk4nfna4.skadnetwork"); AddToList("n9x2a789qt.skadnetwork");
                        AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                        AddToList("pwa73g5rt2.skadnetwork"); AddToList("pwdxu55a5a.skadnetwork"); AddToList("qqp299437r.skadnetwork");
                        AddToList("r45fhb6rf7.skadnetwork"); AddToList("rvh3l7un93.skadnetwork"); AddToList("rx5hdcabgc.skadnetwork");
                        AddToList("s39g8k73mm.skadnetwork"); AddToList("s69wq72ugq.skadnetwork"); AddToList("su67r6k2v3.skadnetwork");
                        AddToList("t38b2kh725.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork"); AddToList("u679fj5vs4.skadnetwork");
                        AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork"); AddToList("w9q455wk68.skadnetwork");
                        AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork"); AddToList("x44k69ngh6.skadnetwork");
                        AddToList("x8uqf25wch.skadnetwork"); AddToList("xy9t38ct57.skadnetwork"); AddToList("y45688jllp.skadnetwork");
                        AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork"); AddToList("zmvfpc5aq8.skadnetwork");
                        AddToList("zq492l623r.skadnetwork");
                        break;
                    case "CSJ":
                        AddToList("238da6jt44.skadnetwork"); AddToList("x2jnk7ly8j.skadnetwork"); AddToList("22mmun2rn5.skadnetwork");
                        break;
                    case "Facebook": AddToList("v9wttpbfk9.skadnetwork"); AddToList("n38lu8286q.skadnetwork"); break;
                    case "Fyber":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork"); AddToList("252b5q8x7y.skadnetwork");
                        AddToList("2u9pt9hc89.skadnetwork"); AddToList("3qy4746246.skadnetwork"); AddToList("3sh42y64q3.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork");
                        AddToList("4pfyvq9l8r.skadnetwork"); AddToList("5a6flpkh64.skadnetwork"); AddToList("5l3tpt7t6e.skadnetwork");
                        AddToList("5lm9lj6jb7.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork");
                        AddToList("8s468mfl3y.skadnetwork"); AddToList("9g2aggbj52.skadnetwork"); AddToList("9rd848q2bz.skadnetwork");
                        AddToList("9t245vhmpl.skadnetwork"); AddToList("av6w8kgt66.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork");
                        AddToList("cg4yq2srnc.skadnetwork"); AddToList("dzg6xy7pwj.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork");
                        AddToList("f38h382jlk.skadnetwork"); AddToList("f73kdq92p3.skadnetwork"); AddToList("g28c52eehv.skadnetwork");
                        AddToList("hdw39hrw9y.skadnetwork"); AddToList("hs6bdukanm.skadnetwork"); AddToList("kbd757ywx3.skadnetwork");
                        AddToList("klf5c3l5u5.skadnetwork"); AddToList("m8dbw4sv7c.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork");
                        AddToList("ppxm28t8ap.skadnetwork"); AddToList("prcb7njmu6.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork");
                        AddToList("t38b2kh725.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork"); AddToList("uw77j35x4d.skadnetwork");
                        AddToList("v72qych5uu.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmZ9fp6w.skadnetwork");
                        AddToList("y45688jllp.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork");
                        AddToList("zq492l623r.skadnetwork");
                        break;
                    case "HyprMX": AddToList("nu4557a4je.skadnetwork"); break;
                    case "InMobi":
                        AddToList("2fnua5tdw4.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork"); AddToList("3qcr597p9d.skadnetwork");
                        AddToList("3rd42ekr43.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork");
                        AddToList("4pfyvq9l8r.skadnetwork"); AddToList("578prtvx9j.skadnetwork"); AddToList("5a6flpkh64.skadnetwork");
                        AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                        AddToList("7ug5zh24hu.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("97r2b46745.skadnetwork");
                        AddToList("9nlqeag3gk.skadnetwork"); AddToList("9rd848q2bz.skadnetwork"); AddToList("9t245vhmpl.skadnetwork");
                        AddToList("av6w8kgt66.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                        AddToList("cj5566h2ga.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                        AddToList("g28c52eehv.skadnetwork"); AddToList("ggvn48r87g.skadnetwork"); AddToList("glqzh8vgby.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("k674qkevps.skadnetwork"); AddToList("kbd757ywx3.skadnetwork");
                        AddToList("klf5c3l5u5.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mtkv5xtk9e.skadnetwork");
                        AddToList("n6fk4nfna4.skadnetwork"); AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork");
                        AddToList("prcb7njmu6.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork"); AddToList("t38b2kh725.skadnetwork");
                        AddToList("tl55sbb4fm.skadnetwork"); AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork");
                        AddToList("w9q455wk68.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                        AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork");
                        break;
                    case "Liftoff":
                        AddToList("7ug5zh24hu.skadnetwork");
                        break;
                    case "Ironsource":
                        AddToList("su67r6k2v3.skadnetwork"); AddToList("f7s53z58qe.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork");
                        AddToList("v72qych5uu.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                        AddToList("m8dbw4sv7c.skadnetwork"); AddToList("3rd42ekr43.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork");
                        AddToList("t38b2kh725.skadnetwork"); AddToList("f38h382jlk.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("ppxm28t8ap.skadnetwork"); AddToList("av6w8kgt66.skadnetwork"); AddToList("cp8zw746q7.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork"); AddToList("22mmun2rn5.skadnetwork");
                        AddToList("s39g8k73mm.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("3qy4746246.skadnetwork");
                        AddToList("k674qkevps.skadnetwork"); AddToList("kbmxgpxpgc.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork");
                        AddToList("a2p9lx4jpn.skadnetwork");
                        break;
                    case "Maio": AddToList("v4nxqhlyqp.skadnetwork"); break;
                    case "Mintegral":
                        AddToList("kbd757ywx3.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork");
                        AddToList("4pfyvq9l8r.skadnetwork"); AddToList("ydx93a7ass.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                        AddToList("p78axxw29g.skadnetwork"); AddToList("737z793b9f.skadnetwork"); AddToList("v72qych5uu.skadnetwork");
                        AddToList("6xzpu9s2p8.skadnetwork"); AddToList("ludvb6z3bs.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork");
                        AddToList("c6k4g5qg8m.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("523jb4fst2.skadnetwork");
                        AddToList("ggvn48r87g.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("f38h382jlk.skadnetwork");
                        AddToList("24t9a8vw3c.skadnetwork"); AddToList("hs6bdukanm.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                        AddToList("m8dbw4sv7c.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork"); AddToList("cj5566h2ga.skadnetwork");
                        AddToList("cstr6suwn9.skadnetwork"); AddToList("w9q455wk68.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                        AddToList("yclnxrl5pm.skadnetwork"); AddToList("4468km3ulz.skadnetwork"); AddToList("t38b2kh725.skadnetwork");
                        AddToList("k674qkevps.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork");
                        AddToList("9rd848q2bz.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork"); AddToList("4w7y6s5ca2.skadnetwork");
                        AddToList("feyaarzu9v.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork"); AddToList("9t245vhmpl.skadnetwork");
                        AddToList("n9x2a789qt.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("zmvfpc5aq8.skadnetwork");
                        AddToList("tl55sbb4fm.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork"); AddToList("5a6flpkh64.skadnetwork");
                        AddToList("8s468mfl3y.skadnetwork"); AddToList("glqzh8vgby.skadnetwork"); AddToList("av6w8kgt66.skadnetwork");
                        AddToList("klf5c3l5u5.skadnetwork"); AddToList("dzg6xy7pwj.skadnetwork"); AddToList("y45688jllp.skadnetwork");
                        AddToList("hdw39hrw9y.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("5l3tpt7t6e.skadnetwork"); AddToList("uw77j35x4d.skadnetwork"); AddToList("4dzt52r2t5.skadnetwork");
                        AddToList("mtkv5xtk9e.skadnetwork"); AddToList("gta9lk7p23.skadnetwork"); AddToList("5tjdwbrq8w.skadnetwork");
                        AddToList("3rd42ekr43.skadnetwork"); AddToList("g28c52eehv.skadnetwork"); AddToList("su67r6k2v3.skadnetwork");
                        AddToList("rx5hdcabgc.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork"); AddToList("32z4fx6l9h.skadnetwork");
                        AddToList("xy9t38ct57.skadnetwork"); AddToList("54nzkqm89y.skadnetwork"); AddToList("9b89h5y424.skadnetwork");
                        AddToList("pwa73g5rt2.skadnetwork"); AddToList("79pbpufp6p.skadnetwork"); AddToList("kbmxgpxpgc.skadnetwork");
                        AddToList("275upjj5gd.skadnetwork"); AddToList("rvh3l7un93.skadnetwork"); AddToList("qqp299437r.skadnetwork");
                        AddToList("294l99pt4k.skadnetwork"); AddToList("74b6s63p6l.skadnetwork"); AddToList("44n7hlldy6.skadnetwork");
                        AddToList("6p4ks3rnbw.skadnetwork"); AddToList("f73kdq92p3.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork");
                        AddToList("97r2b46745.skadnetwork"); AddToList("3qcr597p9d.skadnetwork"); AddToList("578prtvx9j.skadnetwork");
                        AddToList("n6fk4nfna4.skadnetwork"); AddToList("b9bk5wbcq9.skadnetwork"); AddToList("84993kbrcf.skadnetwork");
                        AddToList("24zw6aqk47.skadnetwork"); AddToList("pwdxu55a5a.skadnetwork"); AddToList("cs644xg564.skadnetwork");
                        AddToList("6964rsfnh4.skadnetwork"); AddToList("9vvzujtq5s.skadnetwork"); AddToList("a7xqa6mtl2.skadnetwork");
                        AddToList("r45fhb6rf7.skadnetwork"); AddToList("c3frkrj4fj.skadnetwork"); AddToList("6g9af3uyq4.skadnetwork");
                        AddToList("u679fj5vs4.skadnetwork"); AddToList("g2y4y55b64.skadnetwork"); AddToList("zq492l623r.skadnetwork");
                        AddToList("a8cz6cu7e5.skadnetwork");
                        break;
                    case "MyTarget":
                        AddToList("n9x2a789qt.skadnetwork"); AddToList("r26jy69rpl.skadnetwork");
                        break;
                    case "Nend": break;
                    case "Ogury": break;
                    case "Pangle":
                        AddToList("238da6jt44.skadnetwork"); AddToList("22mmun2rn5.skadnetwork");
                        break;
                    case "Smaato":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork"); AddToList("275upjj5gd.skadnetwork");
                        AddToList("294l99pt4k.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                        AddToList("32z4fx6l9h.skadnetwork"); AddToList("3l6bd9hu43.skadnetwork"); AddToList("3qcr597p9d.skadnetwork");
                        AddToList("3rd42ekr43.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("488r3q3dtq.skadnetwork");
                        AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork"); AddToList("523jb4fst2.skadnetwork");
                        AddToList("52fl2v3hgk.skadnetwork"); AddToList("578prtvx9j.skadnetwork"); AddToList("5a6flpkh64.skadnetwork");
                        AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("5tjdwbrq8w.skadnetwork");
                        AddToList("6g9af3uyq4.skadnetwork"); AddToList("6v7lgmsu45.skadnetwork"); AddToList("6xzpu9s2p8.skadnetwork");
                        AddToList("74b6s63p6l.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork");
                        AddToList("89z7zv988g.skadnetwork"); AddToList("8m87ys6875.skadnetwork"); AddToList("8s468mfl3y.skadnetwork");
                        AddToList("97r2b46745.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork"); AddToList("9rd848q2bz.skadnetwork");
                        AddToList("9t245vhmpl.skadnetwork"); AddToList("av6w8kgt66.skadnetwork"); AddToList("b9bk5wbcq9.skadnetwork");
                        AddToList("bxvub5ada5.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                        AddToList("cj5566h2ga.skadnetwork"); AddToList("cstr6suwn9.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork");
                        AddToList("ejvt5qm6ak.skadnetwork"); AddToList("f38h382jlk.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                        AddToList("feyaarzu9v.skadnetwork"); AddToList("g28c52eehv.skadnetwork"); AddToList("ggvn48r87g.skadnetwork");
                        AddToList("glqzh8vgby.skadnetwork"); AddToList("gta9lk7p23.skadnetwork"); AddToList("hb56zgv37p.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("k674qkevps.skadnetwork"); AddToList("kbd757ywx3.skadnetwork");
                        AddToList("kbmxgpxpgc.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork"); AddToList("m297p6643m.skadnetwork");
                        AddToList("m5mvw97r93.skadnetwork"); AddToList("m8dbw4sv7c.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork");
                        AddToList("mls7yz5dvl.skadnetwork"); AddToList("mp6xlyr22a.skadnetwork"); AddToList("mtkv5xtk9e.skadnetwork");
                        AddToList("n6fk4nfna4.skadnetwork"); AddToList("n9x2a789qt.skadnetwork"); AddToList("p78axxw29g.skadnetwork");
                        AddToList("ppxm28t8ap.skadnetwork"); AddToList("prcb7njmu6.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork");
                        AddToList("qqp299437r.skadnetwork"); AddToList("r45fhb6rf7.skadnetwork"); AddToList("rvh3l7un93.skadnetwork");
                        AddToList("rx5hdcabgc.skadnetwork"); AddToList("t38b2kh725.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork");
                        AddToList("u679fj5vs4.skadnetwork"); AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork");
                        AddToList("vcra2ehyfk.skadnetwork"); AddToList("w9q455wk68.skadnetwork"); AddToList("wg4vff78zm.skadnetwork");
                        AddToList("wzmmz9fp6w.skadnetwork"); AddToList("x44k69ngh6.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork");
                        AddToList("ydx93a7ass.skadnetwork"); AddToList("zmvfpc5aq8.skadnetwork");
                        break;
                    case "Tapjoy":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("238da6jt44.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork");
                        AddToList("252b5q8x7y.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork"); AddToList("3qy4746246.skadnetwork");
                        AddToList("3rd42ekr43.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("44n7hlldy6.skadnetwork");
                        AddToList("488r3q3dtq.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork");
                        AddToList("523jb4fst2.skadnetwork"); AddToList("52fl2v3hgk.skadnetwork"); AddToList("578prtvx9j.skadnetwork");
                        AddToList("5a6flpkh64.skadnetwork"); AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork");
                        AddToList("5tjdwbrq8w.skadnetwork"); AddToList("737z793b9f.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                        AddToList("7ug5zh24hu.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("97r2b46745.skadnetwork");
                        AddToList("9rd848q2bz.skadnetwork"); AddToList("9t245vhmpl.skadnetwork"); AddToList("9yg77x724h.skadnetwork");
                        AddToList("av6w8kgt66.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                        AddToList("cj5566h2ga.skadnetwork"); AddToList("cstr6suwn9.skadnetwork"); AddToList("dzg6xy7pwj.skadnetwork");
                        AddToList("ecpz2srf59.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                        AddToList("g28c52eehv.skadnetwork"); AddToList("ggvn48r87g.skadnetwork"); AddToList("glqzh8vgby.skadnetwork");
                        AddToList("gvmwg8q7h5.skadnetwork"); AddToList("hdw39hrw9y.skadnetwork"); AddToList("hs6bdukanm.skadnetwork");
                        AddToList("kbd757ywx3.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork"); AddToList("lr83yxwka7.skadnetwork");
                        AddToList("m8dbw4sv7c.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork");
                        AddToList("mtkv5xtk9e.skadnetwork"); AddToList("n66cz3y3bx.skadnetwork"); AddToList("n9x2a789qt.skadnetwork");
                        AddToList("nzq8sh4pbs.skadnetwork"); AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork");
                        AddToList("prcb7njmu6.skadnetwork"); AddToList("pu4na253f3.skadnetwork"); AddToList("s39g8k73mm.skadnetwork");
                        AddToList("t38b2kh725.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork"); AddToList("u679fj5vs4.skadnetwork");
                        AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork"); AddToList("v79kvwwj4g.skadnetwork");
                        AddToList("w9q455wk68.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                        AddToList("xy9t38ct57.skadnetwork"); AddToList("y45688jllp.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork");
                        AddToList("ydx93a7ass.skadnetwork"); AddToList("yrqqpx2mcb.skadnetwork"); AddToList("z4gj7hsk7h.skadnetwork");
                        AddToList("zmvfpc5aq8.skadnetwork");
                        break;
                    case "Unity":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("238da6jt44.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                        AddToList("32z4fx6l9h.skadnetwork"); AddToList("3qy4746246.skadnetwork"); AddToList("3rd42ekr43.skadnetwork");
                        AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork"); AddToList("4468km3ulz.skadnetwork");
                        AddToList("44jx6755aq.skadnetwork"); AddToList("488r3q3dtq.skadnetwork"); AddToList("4dzt52r2t5.skadnetwork");
                        AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork"); AddToList("578prtvx9j.skadnetwork");
                        AddToList("5a6flpkh64.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("5tjdwbrq8w.skadnetwork");
                        AddToList("7ug5zh24hu.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("9rd848q2bz.skadnetwork");
                        AddToList("9t245vhmpl.skadnetwork"); AddToList("av6w8kgt66.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork");
                        AddToList("cstr6suwn9.skadnetwork"); AddToList("f38h382jlk.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                        AddToList("f7s53z58qe.skadnetwork"); AddToList("glqzh8vgby.skadnetwork"); AddToList("hs6bdukanm.skadnetwork");
                        AddToList("kbd757ywx3.skadnetwork"); AddToList("lr83yxwka7.skadnetwork"); AddToList("m8dbw4sv7c.skadnetwork");
                        AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mp6xlyr22a.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork");
                        AddToList("prcb7njmu6.skadnetwork"); AddToList("s39g8k73mm.skadnetwork"); AddToList("t38b2kh725.skadnetwork");
                        AddToList("tl55sbb4fm.skadnetwork"); AddToList("v72qych5uu.skadnetwork"); AddToList("v79kvwwj4g.skadnetwork");
                        AddToList("w9q455wk68.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                        AddToList("x44k69ngh6.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork");
                        AddToList("zmvfpc5aq8.skadnetwork"); AddToList("a2p9lx4jpn.skadnetwork");
                        break;
                    case "Vungle":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork");
                        AddToList("2u9pt9hc89.skadnetwork"); AddToList("32z4fx6l9h.skadnetwork"); AddToList("3rd42ekr43.skadnetwork");
                        AddToList("3sh42y64q3.skadnetwork"); AddToList("424m5254lk.skadnetwork"); AddToList("4468km3ulz.skadnetwork");
                        AddToList("44jx6755aq.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork");
                        AddToList("523jb4fst2.skadnetwork"); AddToList("54nzkqm89y.skadnetwork"); AddToList("5a6flpkh64.skadnetwork");
                        AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("5tjdwbrq8w.skadnetwork");
                        AddToList("6xzpu9s2p8.skadnetwork"); AddToList("7953jerfzd.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                        AddToList("7ug5zh24hu.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("9b89h5y424.skadnetwork");
                        AddToList("9nlqeag3gk.skadnetwork"); AddToList("9rd848q2bz.skadnetwork"); AddToList("9t245vhmpl.skadnetwork");
                        AddToList("9yg77x724h.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                        AddToList("cj5566h2ga.skadnetwork"); AddToList("cstr6suwn9.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork");
                        AddToList("f38h382jlk.skadnetwork"); AddToList("f7s53z58qe.skadnetwork"); AddToList("feyaarzu9v.skadnetwork");
                        AddToList("g28c52eehv.skadnetwork"); AddToList("ggvn48r87g.skadnetwork"); AddToList("glqzh8vgby.skadnetwork");
                        AddToList("gta9lk7p23.skadnetwork"); AddToList("hs6bdukanm.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork");
                        AddToList("m8dbw4sv7c.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork");
                        AddToList("mp6xlyr22a.skadnetwork"); AddToList("mtkv5xtk9e.skadnetwork"); AddToList("n66cz3y3bx.skadnetwork");
                        AddToList("n9x2a789qt.skadnetwork"); AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork");
                        AddToList("prcb7njmu6.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork"); AddToList("t38b2kh725.skadnetwork");
                        AddToList("tl55sbb4fm.skadnetwork"); AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork");
                        AddToList("wg4vff78zm.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork"); AddToList("x44k69ngh6.skadnetwork");
                        AddToList("xy9t38ct57.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork");
                        AddToList("zmvfpc5aq8.skadnetwork");
                        break;
                    case "Yahoo":
                        AddToList("4fzdc2evr5.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("9rd848q2bz.skadnetwork");
                        AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork"); AddToList("e5fvkxwrpn.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork"); AddToList("uw77j35x4d.skadnetwork");
                        break;
                    case "Bigo":
                        AddToList("22mmun2rn5.skadnetwork"); AddToList("2U9PT9HC89.skadnetwork"); AddToList("3qy4746246.skadnetwork");
                        AddToList("3RD42EKR43.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("4468km3ulz.skadnetwork");
                        AddToList("44jx6755aq.skadnetwork"); AddToList("4FZDC2EVR5.skadnetwork"); AddToList("523jb4fst2.skadnetwork");
                        AddToList("5a6flpkh64.skadnetwork"); AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork");
                        AddToList("737z793b9f.skadnetwork"); AddToList("7953JERFZD.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                        AddToList("7UG5ZH24HU.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("97r2b46745.skadnetwork");
                        AddToList("9RD848Q2BZ.skadnetwork"); AddToList("9T245VHMPL.skadnetwork"); AddToList("av6w8kgt66.skadnetwork");
                        AddToList("bvpn9ufa9b.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                        AddToList("CJ5566H2GA.skadnetwork"); AddToList("F38H382JLK.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                        AddToList("ggvn48r87g.skadnetwork"); AddToList("gvmwg8q7h5.skadnetwork"); AddToList("hs6bdukanm.skadnetwork");
                        AddToList("kbd757ywx3.skadnetwork"); AddToList("KLF5C3L5U5.skadnetwork"); AddToList("M8DBW4SV7C.skadnetwork");
                        AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork"); AddToList("mtkv5xtk9e.skadnetwork");
                        AddToList("n66cz3y3bx.skadnetwork"); AddToList("n9x2a789qt.skadnetwork"); AddToList("nzq8sh4pbs.skadnetwork");
                        AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                        AddToList("pu4na253f3.skadnetwork"); AddToList("t38b2kh725.skadnetwork"); AddToList("u679fj5vs4.skadnetwork");
                        AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork"); AddToList("W9Q455WK68.skadnetwork");
                        AddToList("WZMMZ9FP6W.skadnetwork"); AddToList("XY9T38CT57.skadnetwork"); AddToList("YCLNXRL5PM.skadnetwork");
                        AddToList("z4gj7hsk7h.skadnetwork"); AddToList("wg4vff78zm.skadnetwork"); AddToList("ydx93a7ass.skadnetwork");
                        AddToList("4PFYVQ9L8R.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork"); AddToList("578prtvx9j.skadnetwork");
                        AddToList("8m87ys6875.skadnetwork"); AddToList("488r3q3dtq.skadnetwork"); AddToList("TL55SBB4FM.skadnetwork");
                        AddToList("zmvfpc5aq8.skadnetwork"); AddToList("6xzpu9s2p8.skadnetwork"); AddToList("a8cz6cu7e5.skadnetwork");
                        AddToList("glqzh8vgby.skadnetwork"); AddToList("feyaarzu9v.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("BD757YWX3.skadnetwork"); AddToList("s39g8k73mm.skadnetwork"); AddToList("33r6p7g8nc.skadnetwork");
                        AddToList("g28c52eehv.skadnetwork"); AddToList("52fl2v3hgk.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork");
                        AddToList("9nlqeag3gk.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork"); AddToList("gta9lk7p23.skadnetwork");
                        AddToList("5tjdwbrq8w.skadnetwork"); AddToList("6g9af3uyq4.skadnetwork"); AddToList("275upjj5gd.skadnetwork");
                        AddToList("rx5hdcabgc.skadnetwork"); AddToList("x44k69ngh6.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork");
                        AddToList("g69uk9uh2b.skadnetwork"); AddToList("zq492l623r.skadnetwork"); AddToList("9b89h5y424.skadnetwork");
                        AddToList("bxvub5ada5.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork");
                        AddToList("f38h382jlk.skadnetwork"); AddToList("cj5566h2ga.skadnetwork"); AddToList("cstr6suwn9.skadnetwork");
                        AddToList("wzmmz9fp6w.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork");
                        AddToList("9rd848q2bz.skadnetwork"); AddToList("9t245vhmpl.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork");
                        AddToList("2u9pt9hc89.skadnetwork"); AddToList("3rd42ekr43.skadnetwork"); AddToList("xy9t38ct57.skadnetwork");
                        AddToList("54nzkqm89y.skadnetwork"); AddToList("32z4fx6l9h.skadnetwork"); AddToList("79pbpufp6p.skadnetwork");
                        AddToList("kbmxgpxpgc.skadnetwork"); AddToList("rvh3l7un93.skadnetwork"); AddToList("qqp299437r.skadnetwork");
                        AddToList("294l99pt4k.skadnetwork"); AddToList("74b6s63p6l.skadnetwork"); AddToList("b9bk5wbcq9.skadnetwork");
                        AddToList("V72QYCH5UU.skadnetwork"); AddToList("44n7hlldy6.skadnetwork"); AddToList("6p4ks3rnbw.skadnetwork");
                        AddToList("g2y4y55b64.skadnetwork");
                        break;
                    case "Yandex":
                        AddToList("zq492l623r.skadnetwork"); AddToList("633vhxswh4.skadnetwork"); AddToList("tmhh9296z4.skadnetwork");
                        AddToList("vcra2ehyfk.skadnetwork"); AddToList("zh3b7bxvad.skadnetwork"); AddToList("xmn954pzmp.skadnetwork");
                        AddToList("79w64w269u.skadnetwork"); AddToList("488r3q3dtq.skadnetwork"); AddToList("d7g9azk84q.skadnetwork");
                        AddToList("nzq8sh4pbs.skadnetwork"); AddToList("866k9ut3g3.skadnetwork"); AddToList("2q884k2j68.skadnetwork");
                        AddToList("x8jxxk4ff5.skadnetwork"); AddToList("gfat3222tu.skadnetwork"); AddToList("pd25vrrwzn.skadnetwork");
                        AddToList("lr83yxwka7.skadnetwork"); AddToList("cp8zw746q7.skadnetwork"); AddToList("pwdxu55a5a.skadnetwork");
                        AddToList("c6k4g5qg8m.skadnetwork"); AddToList("s39g8k73mm.skadnetwork"); AddToList("wg4vff78zm.skadnetwork");
                        AddToList("g28c52eehv.skadnetwork"); AddToList("523jb4fst2.skadnetwork"); AddToList("294l99pt4k.skadnetwork");
                        AddToList("3qy4746246.skadnetwork"); AddToList("a8cz6cu7e5.skadnetwork"); AddToList("ggvn48r87g.skadnetwork");
                        AddToList("y755zyxw56.skadnetwork"); AddToList("qlbq5gtkt8.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork");
                        AddToList("67369282zy.skadnetwork"); AddToList("899vrgt9g8.skadnetwork"); AddToList("mj797d8u6f.skadnetwork");
                        AddToList("3sh42y64q3.skadnetwork"); AddToList("f38h382jlk.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork");
                        AddToList("mp6xlyr22a.skadnetwork"); AddToList("x44k69ngh6.skadnetwork"); AddToList("88k8774x49.skadnetwork");
                        AddToList("hs6bdukanm.skadnetwork"); AddToList("t3b3f7n3x8.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                        AddToList("c7g47wypnu.skadnetwork"); AddToList("52fl2v3hgk.skadnetwork"); AddToList("9vvzujtq5s.skadnetwork");
                        AddToList("m8dbw4sv7c.skadnetwork"); AddToList("9g2aggbj52.skadnetwork"); AddToList("m5mvw97r93.skadnetwork");
                        AddToList("z5b3gh5ugf.skadnetwork"); AddToList("dd3a75yxkv.skadnetwork"); AddToList("9nlqeag3gk.skadnetwork");
                        AddToList("cj5566h2ga.skadnetwork"); AddToList("h5jmj969g5.skadnetwork"); AddToList("dr774724x4.skadnetwork");
                        AddToList("t7ky8fmwkd.skadnetwork"); AddToList("fz2k2k5tej.skadnetwork"); AddToList("u679fj5vs4.skadnetwork");
                        AddToList("cs644xg564.skadnetwork"); AddToList("9b89h5y424.skadnetwork"); AddToList("w28pnjg2k4.skadnetwork");
                        AddToList("2rq3zucswp.skadnetwork"); AddToList("a7xqa6mtl2.skadnetwork"); AddToList("g2y4y55b64.skadnetwork");
                        AddToList("vc83br9sjg.skadnetwork"); AddToList("cstr6suwn9.skadnetwork"); AddToList("eqhxz8m8av.skadnetwork");
                        AddToList("7k3cvf297u.skadnetwork"); AddToList("w9q455wk68.skadnetwork"); AddToList("nu4557a4je.skadnetwork");
                        AddToList("v4nxqhlyqp.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork"); AddToList("7fmhfwg9en.skadnetwork");
                        AddToList("su67r6k2v3.skadnetwork"); AddToList("yclnxrl5pm.skadnetwork"); AddToList("7tnzynbdc7.skadnetwork");
                        AddToList("l6nv3x923s.skadnetwork"); AddToList("h8vml93bkz.skadnetwork"); AddToList("uzqba5354d.skadnetwork");
                        AddToList("8qiegk9qfv.skadnetwork"); AddToList("v79kvwwj4g.skadnetwork"); AddToList("xx9sdjej2w.skadnetwork");
                        AddToList("au67k4efj4.skadnetwork"); AddToList("t38b2kh725.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork");
                        AddToList("rx5hdcabgc.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork"); AddToList("qqp299437r.skadnetwork");
                        AddToList("zmvfpc5aq8.skadnetwork"); AddToList("9rd848q2bz.skadnetwork"); AddToList("79pbpufp6p.skadnetwork");
                        AddToList("dmv22haz9p.skadnetwork"); AddToList("y5ghdn5j9k.skadnetwork"); AddToList("n6fk4nfna4.skadnetwork");
                        AddToList("7rz58n8ntl.skadnetwork"); AddToList("v9wttpbfk9.skadnetwork"); AddToList("n38lu8286q.skadnetwork");
                        AddToList("feyaarzu9v.skadnetwork"); AddToList("7fbxrn65az.skadnetwork"); AddToList("47vhws6wlr.skadnetwork");
                        AddToList("ejvt5qm6ak.skadnetwork"); AddToList("b55w3d8y8z.skadnetwork"); AddToList("v7896pgt74.skadnetwork");
                        AddToList("5ghnmfs3dh.skadnetwork"); AddToList("275upjj5gd.skadnetwork"); AddToList("627r9wr2y5.skadnetwork");
                        AddToList("kbd757ywx3.skadnetwork"); AddToList("sczv5946wb.skadnetwork"); AddToList("8w3np9l82g.skadnetwork");
                        AddToList("hb56zgv37p.skadnetwork"); AddToList("9t245vhmpl.skadnetwork"); AddToList("nrt9jy4kw9.skadnetwork");
                        AddToList("7953jerfzd.skadnetwork"); AddToList("dn942472g5.skadnetwork"); AddToList("6v7lgmsu45.skadnetwork");
                        AddToList("cad8qz2s3j.skadnetwork"); AddToList("n9x2a789qt.skadnetwork"); AddToList("r26jy69rpl.skadnetwork");
                        AddToList("eh6m2bh4zr.skadnetwork"); AddToList("jb7bn6koa5.skadnetwork"); AddToList("fkak3gfpt6.skadnetwork");
                        AddToList("a2p9lx4jpn.skadnetwork"); AddToList("97r2b46745.skadnetwork"); AddToList("22mmun2rn5.skadnetwork");
                        AddToList("238da6jt44.skadnetwork"); AddToList("44jx6755aq.skadnetwork"); AddToList("b9bk5wbcq9.skadnetwork");
                        AddToList("k674qkevps.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork"); AddToList("24zw6aqk47.skadnetwork");
                        AddToList("4468km3ulz.skadnetwork"); AddToList("2tdux39lx8.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                        AddToList("8s468mfl3y.skadnetwork"); AddToList("3cgn6rq224.skadnetwork"); AddToList("glqzh8vgby.skadnetwork");
                        AddToList("av6w8kgt66.skadnetwork"); AddToList("klf5c3l5u5.skadnetwork"); AddToList("nfqy3847ph.skadnetwork");
                        AddToList("dticjx1a9i.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork"); AddToList("9wsyqb3ku7.skadnetwork");
                        AddToList("74b6s63p6l.skadnetwork"); AddToList("xy9t38ct57.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                        AddToList("qu637u8glc.skadnetwork"); AddToList("f73kdq92p3.skadnetwork"); AddToList("44n7hlldy6.skadnetwork");
                        AddToList("kbmxgpxpgc.skadnetwork"); AddToList("5l3tpt7t6e.skadnetwork"); AddToList("ecpz2srf59.skadnetwork");
                        AddToList("x5854y7y24.skadnetwork"); AddToList("f7s53z58qe.skadnetwork"); AddToList("x8uqf25wch.skadnetwork");
                        AddToList("uw77j35x4d.skadnetwork"); AddToList("6964rsfnh4.skadnetwork"); AddToList("gvmwg8q7h5.skadnetwork");
                        AddToList("6yxyv74ff7.skadnetwork"); AddToList("84993kbrcf.skadnetwork"); AddToList("54nzkqm89y.skadnetwork");
                        AddToList("pwa73g5rt2.skadnetwork"); AddToList("mlmmfzh3r3.skadnetwork"); AddToList("9yg77x724h.skadnetwork");
                        AddToList("n66cz3y3bx.skadnetwork"); AddToList("578prtvx9j.skadnetwork"); AddToList("4dzt52r2t5.skadnetwork");
                        AddToList("bvpn9ufa9b.skadnetwork"); AddToList("6qx585k4p6.skadnetwork"); AddToList("mtkv5xtk9e.skadnetwork");
                        AddToList("l93v5h6a4m.skadnetwork"); AddToList("rvh3l7un93.skadnetwork"); AddToList("gta9lk7p23.skadnetwork");
                        AddToList("5tjdwbrq8w.skadnetwork"); AddToList("r45fhb6rf7.skadnetwork"); AddToList("32z4fx6l9h.skadnetwork");
                        AddToList("e5fvkxwrpn.skadnetwork"); AddToList("8c4e2ghe7u.skadnetwork"); AddToList("axh5283zss.skadnetwork");
                        AddToList("3rd42ekr43.skadnetwork"); AddToList("5mv394q32t.skadnetwork"); AddToList("3qcr597p9d.skadnetwork");
                        AddToList("v72qych5uu.skadnetwork"); AddToList("ydx93a7ass.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork");
                        AddToList("5a6flpkh64.skadnetwork"); AddToList("4fzdc2evr5.skadnetwork"); AddToList("4w7y6s5ca2.skadnetwork");
                        AddToList("252b5q8x7y.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork"); AddToList("3l6bd9hu43.skadnetwork");
                        AddToList("4mn522wn87.skadnetwork"); AddToList("6g9af3uyq4.skadnetwork"); AddToList("6p4ks3rnbw.skadnetwork");
                        AddToList("6xzpu9s2p8.skadnetwork"); AddToList("737z793b9f.skadnetwork"); AddToList("89z7zv988g.skadnetwork");
                        AddToList("8m87ys6875.skadnetwork"); AddToList("8r8llnkz5a.skadnetwork"); AddToList("bxvub5ada5.skadnetwork");
                        AddToList("c3frkrj4fj.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork"); AddToList("dbu4b84rxf.skadnetwork");
                        AddToList("dkc879ngq3.skadnetwork"); AddToList("dzg6xy7pwj.skadnetwork"); AddToList("gta8lk7p23.skadnetwork");
                        AddToList("hdw39hrw9y.skadnetwork"); AddToList("hjevpa356n.skadnetwork"); AddToList("krvm3zuq6h.skadnetwork");
                        AddToList("ln5gz23vtd.skadnetwork"); AddToList("ludvb6z3bs.skadnetwork"); AddToList("m297p6643m.skadnetwork");
                        AddToList("p78axxw29g.skadnetwork"); AddToList("pu4na253f3.skadnetwork"); AddToList("s69wq72ugq.skadnetwork");
                        AddToList("t6d3zquu66.skadnetwork"); AddToList("vutu7akeur.skadnetwork"); AddToList("x2jnk7ly8j.skadnetwork");
                        AddToList("x5l83yy675.skadnetwork"); AddToList("y45688jllp.skadnetwork"); AddToList("yrqqpx2mcb.skadnetwork");
                        AddToList("z4gj7hsk7h.skadnetwork"); AddToList("wzmmZ9fp6w.skadnetwork"); AddToList("4pfyvq9L8r.skadnetwork");
                        AddToList("V72QYCH5UU.skadnetwork"); AddToList("2U9PT9HC89.skadnetwork"); AddToList("3RD42EKR43.skadnetwork");
                        AddToList("4FZDC2EVR5.skadnetwork"); AddToList("7953JERFZD.skadnetwork"); AddToList("7UG5ZH24HU.skadnetwork");
                        AddToList("9RD848Q2BZ.skadnetwork"); AddToList("9T245VHMPL.skadnetwork"); AddToList("CJ5566H2GA.skadnetwork");
                        AddToList("F38H382JLK.skadnetwork"); AddToList("KLF5C3L5U5.skadnetwork"); AddToList("M8DBW4SV7C.skadnetwork");
                        AddToList("W9Q455WK68.skadnetwork"); AddToList("WZMMZ9FP6W.skadnetwork"); AddToList("XY9T38CT57.skadnetwork");
                        AddToList("YCLNXRL5PM.skadnetwork"); AddToList("4PFYVQ9L8R.skadnetwork"); AddToList("TL55SBB4FM.skadnetwork");
                        AddToList("BD757YWX3.skadnetwork"); AddToList("33r6p7g8nc.skadnetwork"); AddToList("g69uk9uh2b.skadnetwork");
                        break;
                }
            }
            
#elif USE_APPLOVIN_ADS
            if (!string.IsNullOrEmpty(UnityEditor.AssetDatabase.AssetPathToGUID("Assets/MaxSdk/Mediation/Bigo/Editor/Dependencies.xml")))
            {
                AddToList("22mmun2rn5.skadnetwork"); AddToList("2U9PT9HC89.skadnetwork"); AddToList("3qy4746246.skadnetwork");
                AddToList("3RD42EKR43.skadnetwork"); AddToList("3sh42y64q3.skadnetwork"); AddToList("4468km3ulz.skadnetwork");
                AddToList("44jx6755aq.skadnetwork"); AddToList("4FZDC2EVR5.skadnetwork"); AddToList("523jb4fst2.skadnetwork");
                AddToList("5a6flpkh64.skadnetwork"); AddToList("5l3tpt7t6e.skadnetwork"); AddToList("5lm9lj6jb7.skadnetwork");
                AddToList("737z793b9f.skadnetwork"); AddToList("7953JERFZD.skadnetwork"); AddToList("7rz58n8ntl.skadnetwork");
                AddToList("7UG5ZH24HU.skadnetwork"); AddToList("8s468mfl3y.skadnetwork"); AddToList("97r2b46745.skadnetwork");
                AddToList("9RD848Q2BZ.skadnetwork"); AddToList("9T245VHMPL.skadnetwork"); AddToList("av6w8kgt66.skadnetwork");
                AddToList("bvpn9ufa9b.skadnetwork"); AddToList("c6k4g5qg8m.skadnetwork"); AddToList("cg4yq2srnc.skadnetwork");
                AddToList("CJ5566H2GA.skadnetwork"); AddToList("F38H382JLK.skadnetwork"); AddToList("f73kdq92p3.skadnetwork");
                AddToList("ggvn48r87g.skadnetwork"); AddToList("gvmwg8q7h5.skadnetwork"); AddToList("hs6bdukanm.skadnetwork");
                AddToList("kbd757ywx3.skadnetwork"); AddToList("KLF5C3L5U5.skadnetwork"); AddToList("M8DBW4SV7C.skadnetwork");
                AddToList("mlmmfzh3r3.skadnetwork"); AddToList("mls7yz5dvl.skadnetwork"); AddToList("mtkv5xtk9e.skadnetwork");
                AddToList("n66cz3y3bx.skadnetwork"); AddToList("n9x2a789qt.skadnetwork"); AddToList("nzq8sh4pbs.skadnetwork");
                AddToList("p78axxw29g.skadnetwork"); AddToList("ppxm28t8ap.skadnetwork"); AddToList("prcb7njmu6.skadnetwork");
                AddToList("pu4na253f3.skadnetwork"); AddToList("t38b2kh725.skadnetwork"); AddToList("u679fj5vs4.skadnetwork");
                AddToList("uw77j35x4d.skadnetwork"); AddToList("v72qych5uu.skadnetwork"); AddToList("W9Q455WK68.skadnetwork");
                AddToList("WZMMZ9FP6W.skadnetwork"); AddToList("XY9T38CT57.skadnetwork"); AddToList("YCLNXRL5PM.skadnetwork");
                AddToList("z4gj7hsk7h.skadnetwork"); AddToList("wg4vff78zm.skadnetwork");  AddToList("ydx93a7ass.skadnetwork");
                AddToList("4PFYVQ9L8R.skadnetwork"); AddToList("ejvt5qm6ak.skadnetwork"); AddToList("578prtvx9j.skadnetwork");
                AddToList("8m87ys6875.skadnetwork"); AddToList("488r3q3dtq.skadnetwork"); AddToList("TL55SBB4FM.skadnetwork");
                AddToList("zmvfpc5aq8.skadnetwork"); AddToList("6xzpu9s2p8.skadnetwork"); AddToList("a8cz6cu7e5.skadnetwork");
                AddToList("glqzh8vgby.skadnetwork"); AddToList("feyaarzu9v.skadnetwork"); AddToList("424m5254lk.skadnetwork");
                AddToList("BD757YWX3.skadnetwork"); AddToList("s39g8k73mm.skadnetwork"); AddToList("33r6p7g8nc.skadnetwork");
                AddToList("g28c52eehv.skadnetwork"); AddToList("52fl2v3hgk.skadnetwork"); AddToList("pwa73g5rt2.skadnetwork");
                AddToList("9nlqeag3gk.skadnetwork"); AddToList("24t9a8vw3c.skadnetwork"); AddToList("gta9lk7p23.skadnetwork");
                AddToList("5tjdwbrq8w.skadnetwork"); AddToList("6g9af3uyq4.skadnetwork"); AddToList("275upjj5gd.skadnetwork");
                AddToList("rx5hdcabgc.skadnetwork"); AddToList("x44k69ngh6.skadnetwork"); AddToList("2fnua5tdw4.skadnetwork");
                AddToList("g69uk9uh2b.skadnetwork"); AddToList("zq492l623r.skadnetwork"); AddToList("9b89h5y424.skadnetwork");
                AddToList("bxvub5ada5.skadnetwork");
                AddToList("4fzdc2evr5.skadnetwork"); AddToList("4pfyvq9l8r.skadnetwork"); AddToList("f38h382jlk.skadnetwork");
                AddToList("cj5566h2ga.skadnetwork"); AddToList("cstr6suwn9.skadnetwork"); AddToList("wzmmz9fp6w.skadnetwork");
                AddToList("yclnxrl5pm.skadnetwork"); AddToList("7ug5zh24hu.skadnetwork"); AddToList("9rd848q2bz.skadnetwork");
                AddToList("9t245vhmpl.skadnetwork"); AddToList("tl55sbb4fm.skadnetwork"); AddToList("2u9pt9hc89.skadnetwork");
                AddToList("3rd42ekr43.skadnetwork"); AddToList("xy9t38ct57.skadnetwork"); AddToList("54nzkqm89y.skadnetwork");
                AddToList("32z4fx6l9h.skadnetwork"); AddToList("79pbpufp6p.skadnetwork"); AddToList("kbmxgpxpgc.skadnetwork");
                AddToList("rvh3l7un93.skadnetwork"); AddToList("qqp299437r.skadnetwork"); AddToList("294l99pt4k.skadnetwork");
                AddToList("74b6s63p6l.skadnetwork"); AddToList("b9bk5wbcq9.skadnetwork"); AddToList("V72QYCH5UU.skadnetwork");
                AddToList("44n7hlldy6.skadnetwork"); AddToList("6p4ks3rnbw.skadnetwork"); AddToList("g2y4y55b64.skadnetwork");
            }
#endif

#if AMAZON_APS
            AddToList("p78axxw29g.skadnetwork");
#endif
            //add custom skadnetwork config
            {
                var assetFiles = UnityEditor.AssetDatabase.FindAssets(Application.identifier);
                if (!(assetFiles == null || assetFiles.Length == 0))
                {
                    string assetPath = null;
                    foreach (var udid in assetFiles)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(udid);
                        if (path.EndsWith(".skadnetwork"))
                        {
                            assetPath = path;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var lines = File.ReadAllLines(assetPath);
                        foreach (var str in lines)
                        {
                            if (string.IsNullOrEmpty(str)) continue;
                            if (!str.EndsWith("skadnetwork")) continue;
                            AddToList(str.Trim());
                        }
                    }
                }
            }
            return result.ToArray();
        }
        #endregion

        static EditorWindow GetEditorWindow()
        {
            var randomWindow = EditorWindow.focusedWindow != null ? EditorWindow.focusedWindow : EditorWindow.GetWindow<EditorWindow>("UnityEditor.GameView", false);
            if (randomWindow == null)
            {
                Debug.LogError("[ERROR]Could not find editor window");
            }
            return randomWindow;
        }

        #region Add Firebase
        class AddFirebaseData
        {
            public UnityEditor.PackageManager.Requests.AddRequest requestAddFirebase = null;
            public List<string> listFiles = new List<string>();
        }
        static AddFirebaseData addFirebaseData;

        [MenuItem("AFramework/Add Firebase")]
        public static void AddFirebase()
        {
            var firebaseFolder = GetAFrameworkPath() + "/Firebase/";
            addFirebaseData = new AddFirebaseData();
            addFirebaseData.listFiles.AddRange(new string[] {
                "file:../" + firebaseFolder + "com.google.external-dependency-manager.tgz",
                "file:../" + firebaseFolder + "com.google.firebase.app.tgz",
                "file:../" + firebaseFolder + "com.google.firebase.analytics.tgz",
                "file:../" + firebaseFolder + "com.google.firebase.crashlytics.tgz",
                "file:../" + firebaseFolder + "com.google.firebase.installations.tgz",
                "file:../" + firebaseFolder + "com.google.firebase.remote-config.tgz",
                "file:../" + firebaseFolder + "com.google.firebase.messaging.tgz",//don't know why but import messaging cause error
            });
            //DirectoryInfo d = new DirectoryInfo(firebaseFolder);
            //FileInfo[] Files = d.GetFiles("*.tgz");
            //foreach (FileInfo file in Files)
            //{
            //    addFirebaseData.listFiles.Add("file:../" + firebaseFolder + file.Name);
            //}

            //for (int i = 0; i < addFirebaseData.listFiles.Count; ++i)
            //{
            //    if (addFirebaseData.listFiles[i].Contains("com.google.firebase.app"))
            //    {
            //        var cache = addFirebaseData.listFiles[i];
            //        addFirebaseData.listFiles.RemoveAt(i);
            //        addFirebaseData.listFiles.Insert(0, cache);
            //    }
            //}
            //for (int i = 0; i < addFirebaseData.listFiles.Count; ++i)
            //{
            //    if (addFirebaseData.listFiles[i].Contains("com.google.external-dependency-manager"))
            //    {
            //        var cache = addFirebaseData.listFiles[i];
            //        addFirebaseData.listFiles.RemoveAt(i);
            //        addFirebaseData.listFiles.Insert(0, cache);
            //    }
            //}

            List<BuildTargetGroup> groupList = new List<BuildTargetGroup>();
            groupList.Add(BuildTargetGroup.iOS);
            groupList.Add(BuildTargetGroup.Android);

            List<string> firebaseFlags = new List<string>();
            firebaseFlags.Add("USE_FIREBASE");
            firebaseFlags.Add("USE_FIREBASE_ANALYTICS");
            firebaseFlags.Add("USE_FIREBASE_REMOTECONFIG");

            for (int i = 0; i < groupList.Count; ++i)
            {
                string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(groupList[i]);
                List<string> allDefines = new List<string>(definesString.Split(';'));

                bool hasChanged = false;
                for (int j = 0; j < firebaseFlags.Count; ++j)
                {
                    if (!allDefines.Contains(firebaseFlags[j]))
                    {
                        allDefines.Add(firebaseFlags[j]);
                        hasChanged = true;
                    }
                }

                if (hasChanged)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        groupList[i],
                        string.Join(";", allDefines.ToArray()));
                }
            }

            EditorApplication.update += UpdateAddFirebaseRequest;
        }

        static void UpdateAddFirebaseRequest()
        {
            if (EditorApplication.isCompiling) return;
            if (addFirebaseData.requestAddFirebase == null)
            {
                if (addFirebaseData.listFiles.Count == 0)
                {
                    EditorApplication.update -= UpdateAddFirebaseRequest;
                    return;
                }
                var nextFile = addFirebaseData.listFiles[0];
                addFirebaseData.listFiles.RemoveAt(0);
                Debug.LogWarning(nextFile);
                addFirebaseData.requestAddFirebase = UnityEditor.PackageManager.Client.Add(nextFile);
            }
            else if (addFirebaseData.requestAddFirebase.IsCompleted)
            {
                if (addFirebaseData.requestAddFirebase.Status == UnityEditor.PackageManager.StatusCode.Success)
                    Debug.Log("Installed: " + addFirebaseData.requestAddFirebase.Result.packageId);
                else if (addFirebaseData.requestAddFirebase.Status >= UnityEditor.PackageManager.StatusCode.Failure)
                    Debug.Log(addFirebaseData.requestAddFirebase.Error.message);
                addFirebaseData.requestAddFirebase = null;
            }
        }
        #endregion

        #region Play default scene
        public static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var lastLoadedScenePath = SessionState.GetString("lastLoadedScenePath", null);
                if (!string.IsNullOrEmpty(lastLoadedScenePath))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(lastLoadedScenePath);
                    SessionState.SetString("lastLoadedScenePath", "");
                }
            }
        }
        [MenuItem("AFramework/Play Default Scene #p")]
        public static void PlayDefaultScene()
        {
            if (EditorApplication.isPlaying == true)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            SessionState.SetString("lastLoadedScenePath", UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            EditorApplication.isPlaying = true;
        }
        #endregion
    }
}