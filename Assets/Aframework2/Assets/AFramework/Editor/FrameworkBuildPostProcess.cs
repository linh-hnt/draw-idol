using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_IPHONE || UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.iOS.Xcode.Extensions;
#endif
#endif

namespace AFramework
{
    public static class FrameworkBuildPostProcess
    {
#if UNITY_IPHONE || UNITY_IOS
        //const string AFrameworkResourcesDirectoryName = "FrameworkResources";
        const string localizeFileFolder = "I2Localization";//use I2Localize folder to avoid duplicate file
        const string localizeGroupName = "I2 Localization";//use I2Localize folder to avoid duplicate file
        private const string TargetUnityIphonePodfileLine = "target 'Unity-iPhone' do";

        [PostProcessBuild(10001)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            string projectPath = PBXProject.GetPBXProjectPath(path);
            PBXProject project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projectPath));
#if UNITY_2019_3_OR_NEWER
            var unityMainTargetGuid = project.GetUnityMainTargetGuid();
            var unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
#else
            var unityMainTargetGuid = project.TargetGuidByName("Unity-iPhone");
            var unityFrameworkTargetGuid = project.TargetGuidByName("Unity-iPhone");
#endif

            if (FrameworkGlobalConfig.Instance != null)
            {
                if (FrameworkGlobalConfig.Instance.AdsConfig != null)
                {
                    if (!string.IsNullOrEmpty(FrameworkGlobalConfig.Instance.AdsConfig.Platform.AppId))
                    {
                        string appId = FrameworkGlobalConfig.Instance.AdsConfig.Platform.AppId;
                        if (appId.Length == 0)
                        {
                            Debug.LogError("iOS AdMob app ID is empty. Please enter a valid app ID to run ads properly.");
                        }
                        else
                        {
                            plist.root.SetString("GADApplicationIdentifier", appId);

#if USE_TOPON_ADS
                            plist.root.SetBoolean("GADIsAdManagerApp", true);
#endif
                        }
                    }
#if USE_ADMOB || USE_MOPUB_ADS || USE_TOPON_ADS
                    if (!string.IsNullOrEmpty(FrameworkGlobalConfig.Instance.AdsConfig.Platform.ApplovingSDKKey))
                    {
                        string applovinSDKKey = FrameworkGlobalConfig.Instance.AdsConfig.Platform.ApplovingSDKKey;
                        if (applovinSDKKey.Length == 0)
                        {
                            Debug.LogError("iOS Applovin SDK Key is empty. Please enter a valid SDK Key to run ads properly.");
                        }
                        else
                        {
                            plist.root.SetString("AppLovinSdkKey", applovinSDKKey);
                        }
                    }
#endif
#if USE_TOPON_ADS
                    project.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");
                    project.SetBuildProperty(unityFrameworkTargetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
                    project.SetBuildProperty(unityFrameworkTargetGuid, "GCC_C_LANGUAGE_STANDARD", "gnu99");

                    project.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_LDFLAGS", "-ObjC");
                    project.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_LDFLAGS", "-fobjc-arc");
                    project.AddFileToBuild(unityFrameworkTargetGuid, project.AddFile("usr/lib/libxml2.tbd", "Libraries/libxml2.tbd", PBXSourceTree.Sdk));
                    project.AddFileToBuild(unityFrameworkTargetGuid, project.AddFile("usr/lib/libresolv.9.tbd", "Libraries/libresolv.9.tbd", PBXSourceTree.Sdk));
                    project.AddFileToBuild(unityFrameworkTargetGuid, project.AddFile("usr/lib/libz.tbd", "Libraries/libz.tbd", PBXSourceTree.Sdk));
                    project.AddFileToBuild(unityFrameworkTargetGuid, project.AddFile("usr/lib/libbz2.1.0.tbd", "Libraries/libbz2.1.0.tbd", PBXSourceTree.Sdk));
#endif
                }

#if USE_IRONSOURCE_ADS
                {
                    List<string> additional_framework = new List<string>();
                    if (FrameworkGlobalConfig.Instance.iOSAdditionalFramework != null)
                    {
                        additional_framework.AddRange(FrameworkGlobalConfig.Instance.iOSAdditionalFramework);
                    }
                    if (!additional_framework.Contains("AdSupport.framework"))
                    {
                        additional_framework.Add("AdSupport.framework");
                        FrameworkGlobalConfig.Instance.iOSAdditionalFramework = additional_framework.ToArray();
                    }
                }
#endif
                if (FrameworkGlobalConfig.Instance.iOSAdditionalFramework != null && FrameworkGlobalConfig.Instance.iOSAdditionalFramework.Length > 0)
                {
                    for (int x = 0; x < FrameworkGlobalConfig.Instance.iOSAdditionalFramework.Length; ++x)
                    {
                        if (!string.IsNullOrEmpty(FrameworkGlobalConfig.Instance.iOSAdditionalFramework[x]))
                        {
                            project.AddFrameworkToProject(unityFrameworkTargetGuid, FrameworkGlobalConfig.Instance.iOSAdditionalFramework[x], false);
                        }
                    }
                }

                if (FrameworkGlobalConfig.Instance.iOSPodFramework != null && FrameworkGlobalConfig.Instance.iOSPodFramework.Length > 0)
                {
                    EmbedDynamicLibrariesIfNeeded(path, project, unityMainTargetGuid, FrameworkGlobalConfig.Instance.iOSPodFramework);
                }

                if (FrameworkGlobalConfig.Instance.iosATT != null && !plist.root.values.ContainsKey("NSUserTrackingUsageDescription"))
                {
                    plist.root.SetString("NSUserTrackingUsageDescription", iOSATTConfig.ATTUsageDescription["en"]);
                }
#if USE_APPSFLYER_ANALYTICS
                plist.root.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com/");
#elif USE_IRONSOURCE_ADS
                plist.root.SetString("NSAdvertisingAttributionReportEndpoint", "https://postbacks-is.com/");
#endif

                if (FrameworkGlobalConfig.Instance.iOSPListCustomkey != null && FrameworkGlobalConfig.Instance.iOSPListCustomkey.Length > 0)
                {
                    var listKey = FrameworkGlobalConfig.Instance.iOSPListCustomkey;
                    for (int i = 0; i < listKey.Length; ++i)
                    {
                        plist.root.SetString(listKey[i].key, listKey[i].value);
                    }
                }

                if (FrameworkGlobalConfig.Instance.DisableBitcode)
                {
                    project.SetBuildProperty(unityMainTargetGuid, "ENABLE_BITCODE", "NO");


                    //Unity Tests
                    var target = project.TargetGuidByName(PBXProject.GetUnityTestTargetName());
                    project.SetBuildProperty(target, "ENABLE_BITCODE", "NO");


                    //Unity Framework
                    project.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");
                }

                if (FrameworkGlobalConfig.Instance.iOSIcon.AlternativeIcons != null && FrameworkGlobalConfig.Instance.iOSIcon.AlternativeIcons.Length > 0)
                {
                    SetAlternativeIcon(project, path, FrameworkGlobalConfig.Instance.iOSIcon.AlternativeIcons);
                }
            }

            if (plist.root.values.ContainsKey("NSAppTransportSecurity"))
            {
                plist.root.values.Remove("NSAppTransportSecurity");
            }
            plist.root.CreateDict("NSAppTransportSecurity").values.Add("NSAllowsArbitraryLoads", new PlistElementBoolean(true));

#if (USE_APPSFLYER_ANALYTICS && APPSFLYER_UNINSTALL_EVENT) || USE_REMOTE_NOTIFICATION
            var buildKey = "UIBackgroundModes";
            plist.root.CreateArray(buildKey).AddString("remote-notification");
#endif

            {
                var skadnetworkIds = FrameworkToolEditor.GetSKAdNetworkIDs();
                if (skadnetworkIds != null && skadnetworkIds.Length > 0)
                {
                    PlistElementArray arraySKAdNetworkItems = null;
                    if (plist.root.values.ContainsKey("SKAdNetworkItems"))
                    {
                        arraySKAdNetworkItems = plist.root["SKAdNetworkItems"] as PlistElementArray;
                    }
                    else
                    {
                        arraySKAdNetworkItems = plist.root.CreateArray("SKAdNetworkItems");
                    }
                    for (int i = 0; i < skadnetworkIds.Length; ++i)
                    {
                        arraySKAdNetworkItems.AddDict().values.Add("SKAdNetworkIdentifier", new PlistElementString(skadnetworkIds[i]));
                    }
                }
            }



            File.WriteAllText(plistPath, plist.WriteToString());

#if !USE_TOPON_ADS
            var searchFacebook = AssetDatabase.FindAssets("Facebook.Unity");
            if (searchFacebook.Length > 0)
#endif
            {
                //Fixed Facebook SDK 7.21.2 issue for iOS < 12.2
                project.AddBuildProperty(unityFrameworkTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");
            }
//#if FACEBOOK_AUDIENCENETWORK
            project.SetBuildProperty(unityMainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.SetBuildProperty(unityFrameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
//#endif

#if USE_GAME_CENTER
            {
                System.Reflection.BindingFlags nonPublicInstanceBinding = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", null, unityMainTargetGuid);
                var managerType = typeof(ProjectCapabilityManager);
                var projectField = managerType.GetField("project", nonPublicInstanceBinding);
                var targetGuidField = managerType.GetField("m_TargetGuid", nonPublicInstanceBinding);
                var entitlementFilePathField = managerType.GetField("m_EntitlementFilePath", nonPublicInstanceBinding);
                var getOrCreateEntitlementDocMethod = managerType.GetMethod("GetOrCreateEntitlementDoc", nonPublicInstanceBinding);

                if (projectField != null && targetGuidField != null && entitlementFilePathField != null && getOrCreateEntitlementDocMethod != null)
                {
                    var entitlementFilePath = entitlementFilePathField.GetValue(manager) as string;
                    var entitlementDoc = getOrCreateEntitlementDocMethod.Invoke(manager, new object[] { }) as PlistDocument;
                    if (entitlementDoc != null)
                    {
                        var plistBoolean = new PlistElementBoolean(true);
                        entitlementDoc.root["com.apple.developer.game-center"] = plistBoolean;
                    }

                    var pbxProject = projectField.GetValue(manager) as PBXProject;
                    if (pbxProject != null)
                    {
                        var mainTargetGuid = targetGuidField.GetValue(manager) as string;
                        project.AddCapability(mainTargetGuid, PBXCapabilityType.GameCenter, entitlementFilePath, false);
                    }
                    manager.WriteToFile();
                }
            }
#endif

            File.WriteAllText(projectPath, project.WriteToString());

            if (FrameworkGlobalConfig.Instance.iosATT != null)
            {
                UnityEditor.iOS_I2Loc.Xcode.PBXProject customPBXProject = new UnityEditor.iOS_I2Loc.Xcode.PBXProject();
                string projPath = PBXProject.GetPBXProjectPath(path);
                customPBXProject.ReadFromFile(projPath);
                customPBXProject.RemoveLocalizationVariantGroup(localizeGroupName);

                var resourcesDirectoryPath = Path.Combine(path, localizeFileFolder);
                RemoveDuplicateI2Localize(resourcesDirectoryPath, "zh.lproj");
                RemoveDuplicateI2Localize(resourcesDirectoryPath, "zh-CN.lproj");
                RemoveDuplicateI2Localize(resourcesDirectoryPath, "zh-SG.lproj");
                RemoveDuplicateI2Localize(resourcesDirectoryPath, "zh-HK.lproj");
                RemoveDuplicateI2Localize(resourcesDirectoryPath, "zh-MO.lproj");
                RemoveDuplicateI2Localize(resourcesDirectoryPath, "zh-TW.lproj");

                foreach (var pair in iOSATTConfig.ATTUsageDescription)
                {
                    LocalizeUserTrackingDescriptionIfNeeded(pair.Value, pair.Key, path, customPBXProject);
                }
                var allFiles = Directory.GetFiles(resourcesDirectoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    customPBXProject.AddLocalization(file, file, localizeGroupName);
                }
                customPBXProject.WriteToFile(projPath);
            }

#if USE_REMOTE_NOTIFICATION
            {
                var entitlementsFileName = project.GetBuildPropertyForAnyConfig(unityMainTargetGuid, "CODE_SIGN_ENTITLEMENTS");
                if (entitlementsFileName == null)
                {
                    var bundleIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
                    entitlementsFileName = string.Format("{0}.entitlements", bundleIdentifier.Substring(bundleIdentifier.LastIndexOf(".") + 1));
                }

                var capManager = new ProjectCapabilityManager(projectPath, entitlementsFileName, "Unity-iPhone");
                capManager.AddPushNotifications(false);
                capManager.WriteToFile();
            }
#endif
        }

        private static void LocalizeUserTrackingDescriptionIfNeeded(string localizedUserTrackingDescription, string localeCode, string buildPath, UnityEditor.iOS_I2Loc.Xcode.PBXProject project)
        {
            // Use the legacy resources directory name if the build is being appended (the "Resources" directory already exists if it is an incremental build).
            var resourcesDirectoryName = localizeFileFolder;
            var resourcesDirectoryPath = Path.Combine(buildPath, resourcesDirectoryName);
            var localeSpecificDirectoryName = localeCode + ".lproj";
            var localeSpecificDirectoryPath = Path.Combine(resourcesDirectoryPath, localeSpecificDirectoryName);
            var infoPlistStringsFilePath = Path.Combine(localeSpecificDirectoryPath, "InfoPlist.strings");

            // Create intermediate directories as needed.
            if (!Directory.Exists(resourcesDirectoryPath))
            {
                Directory.CreateDirectory(resourcesDirectoryPath);
            }

            if (!Directory.Exists(localeSpecificDirectoryPath))
            {
                Directory.CreateDirectory(localeSpecificDirectoryPath);
            }

            var localizedDescriptionLine = "\"NSUserTrackingUsageDescription\" = \"" + localizedUserTrackingDescription + "\";\n";
            // File already exists, update it in case the value changed between builds.
            if (File.Exists(infoPlistStringsFilePath))
            {
                var output = new List<string>();
                var lines = File.ReadAllLines(infoPlistStringsFilePath);
                var keyUpdated = false;
                foreach (var line in lines)
                {
                    if (line.Contains("NSUserTrackingUsageDescription"))
                    {
                        output.Add(localizedDescriptionLine);
                        keyUpdated = true;
                    }
                    else
                    {
                        output.Add(line);
                    }
                }

                if (!keyUpdated)
                {
                    output.Add(localizedDescriptionLine);
                }

                File.WriteAllText(infoPlistStringsFilePath, string.Join("\n", output.ToArray()) + "\n");
            }
            // File doesn't exist, create one.
            else
            {
                File.WriteAllText(infoPlistStringsFilePath, "/* Localized versions of Info.plist keys - Generated by AFramework */\n" + localizedDescriptionLine);
            }

            //var localeSpecificDirectoryRelativePath = Path.Combine(resourcesDirectoryName, localeSpecificDirectoryName);
            //var guid = project.AddFolderReference(localeSpecificDirectoryRelativePath, localeSpecificDirectoryRelativePath);
            //project.AddLocalization(infoPlistStringsFilePath, infoPlistStringsFilePath, localizeGroupName);
        }

        static void RemoveDuplicateI2Localize(string dir, string fromLang)
        {
            string delete_path = Path.Combine(dir, fromLang);
            if (!Directory.Exists(delete_path)) return;
            string toLang = "zh-Hans.lproj";
            if (fromLang == "zh-HK.lproj" || fromLang == "zh-MO.lproj" || fromLang == "zh-TW.lproj")
            {
                toLang = "zh-Hant.lproj";
            }

            
            string create_path = Path.Combine(dir, toLang);
            if (Directory.Exists(create_path))
            {
                Directory.Delete(delete_path, true);
                return;
            }
            Directory.Move(delete_path, create_path);
        }

        private static void EmbedDynamicLibrariesIfNeeded(string buildPath, PBXProject project, string targetGuid, string[] DynamicLibrariesToEmbed)
        {
            // Check that the Pods directory exists (it might not if a publisher is building with Generate Podfile setting disabled in EDM).
            var podsDirectory = Path.Combine(buildPath, "Pods");
            if (!Directory.Exists(podsDirectory)) return;

            var dynamicLibraryPathsPresentInProject = new List<string>();
            foreach (var dynamicLibraryToSearch in DynamicLibrariesToEmbed)
            {
                // both .framework and .xcframework are directories, not files
                var directories = Directory.GetDirectories(podsDirectory, dynamicLibraryToSearch, SearchOption.AllDirectories);
                if (directories.Length <= 0) continue;

                var dynamicLibraryAbsolutePath = directories[0];
                var index = dynamicLibraryAbsolutePath.LastIndexOf("Pods");
                var relativePath = dynamicLibraryAbsolutePath.Substring(index);
                dynamicLibraryPathsPresentInProject.Add(relativePath);
            }

            if (dynamicLibraryPathsPresentInProject.Count <= 0) return;

#if UNITY_2019_3_OR_NEWER
            // Embed framework only if the podfile does not contain target `Unity-iPhone`.
            //if (!ContainsUnityIphoneTargetInPodfile(buildPath))
            {
                foreach (var dynamicLibraryPath in dynamicLibraryPathsPresentInProject)
                {
                    var fileGuid = project.AddFile(dynamicLibraryPath, dynamicLibraryPath);
                    project.AddFileToEmbedFrameworks(targetGuid, fileGuid);
                }
            }
#else
            string runpathSearchPaths;
#if UNITY_2018_2_OR_NEWER
            runpathSearchPaths = project.GetBuildPropertyForAnyConfig(targetGuid, "LD_RUNPATH_SEARCH_PATHS");
#else
            runpathSearchPaths = "$(inherited)";          
#endif
            runpathSearchPaths += string.IsNullOrEmpty(runpathSearchPaths) ? "" : " ";

            // Check if runtime search paths already contains the required search paths for dynamic libraries.
            if (runpathSearchPaths.Contains("@executable_path/Frameworks")) return;

            runpathSearchPaths += "@executable_path/Frameworks";
            project.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", runpathSearchPaths);
#endif
        }

        //#if UNITY_2019_3_OR_NEWER
        //        private static bool ContainsUnityIphoneTargetInPodfile(string buildPath)
        //        {
        //            var podfilePath = Path.Combine(buildPath, "Podfile");
        //            if (!File.Exists(podfilePath)) return false;

        //            var lines = File.ReadAllLines(podfilePath);
        //            return lines.Any(line => line.Contains(TargetUnityIphonePodfileLine));
        //        }
        //#endif

#region Alternative Icon
        private static void SetAlternativeIcon(PBXProject pbxProject, string pathToBuiltProject, AFramework.iOSAlternateIcon.AlternateIcon[] iconData)
        {
            if (iconData == null || iconData.Length == 0) return;
            var imagesXcassetsDirectoryPath = Path.Combine(pathToBuiltProject, "Unity-iPhone", "Images.xcassets");

            var iconNames = new List<string>();
            foreach (var alternateIcon in iconData)
            {
                if (string.IsNullOrEmpty(alternateIcon.iconName)) continue;
                iconNames.Add(alternateIcon.iconName);
                var iconDirectoryPath = Path.Combine(imagesXcassetsDirectoryPath, $"{alternateIcon.iconName}.appiconset");
                Directory.CreateDirectory(iconDirectoryPath);

                var contentsJsonPath = Path.Combine(iconDirectoryPath, "Contents.json");
                var contentsJson = ContentsJsonText;
                contentsJson = contentsJson.Replace("iPhoneContents", PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneOnly || PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneAndiPad ? ContentsiPhoneJsonText : string.Empty);
                contentsJson = contentsJson.Replace("iPadContents", PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPadOnly || PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneAndiPad ? ContentsiPadJsonText : string.Empty);
                File.WriteAllText(contentsJsonPath, contentsJson, System.Text.Encoding.UTF8);

                var iconTexture = alternateIcon.source != null ? alternateIcon.source : Resources.Load<Texture2D>(alternateIcon.sourcePath);
                if (PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneOnly || PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneAndiPad)
                {
                    SaveIcon(iconTexture, null, 40, Path.Combine(iconDirectoryPath, "iPhoneNotification40px.png"));
                    SaveIcon(iconTexture, null, 60, Path.Combine(iconDirectoryPath, "iPhoneNotification60px.png"));
                    SaveIcon(iconTexture, null, 58, Path.Combine(iconDirectoryPath, "iPhoneSettings58px.png"));
                    SaveIcon(iconTexture, null, 87, Path.Combine(iconDirectoryPath, "iPhoneSettings87px.png"));
                    SaveIcon(iconTexture, null, 80, Path.Combine(iconDirectoryPath, "iPhoneSpotlight80px.png"));
                    SaveIcon(iconTexture, null, 120, Path.Combine(iconDirectoryPath, "iPhoneSpotlight120px.png"));
                    SaveIcon(iconTexture, null, 120, Path.Combine(iconDirectoryPath, "iPhoneApp120px.png"));
                    SaveIcon(iconTexture, null, 180, Path.Combine(iconDirectoryPath, "iPhoneApp180px.png"));
                }

                if (PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPadOnly || PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneAndiPad)
                {
                    SaveIcon(iconTexture, null, 20, Path.Combine(iconDirectoryPath, "iPadNotifications20px.png"));
                    SaveIcon(iconTexture, null, 40, Path.Combine(iconDirectoryPath, "iPadNotifications40px.png"));
                    SaveIcon(iconTexture, null, 29, Path.Combine(iconDirectoryPath, "iPadSettings29px.png"));
                    SaveIcon(iconTexture, null, 58, Path.Combine(iconDirectoryPath, "iPadSettings58px.png"));
                    SaveIcon(iconTexture, null, 40, Path.Combine(iconDirectoryPath, "iPadSpotlight40px.png"));
                    SaveIcon(iconTexture, null, 80, Path.Combine(iconDirectoryPath, "iPadSpotlight80px.png"));
                    SaveIcon(iconTexture, null, 76, Path.Combine(iconDirectoryPath, "iPadApp76px.png"));
                    SaveIcon(iconTexture, null, 152, Path.Combine(iconDirectoryPath, "iPadApp152px.png"));
                    SaveIcon(iconTexture, null, 167, Path.Combine(iconDirectoryPath, "iPadProApp167px.png"));
                }

                SaveIcon(iconTexture, null, 1024, Path.Combine(iconDirectoryPath, "appStore1024px.png"));
            }

            var targetGuid = pbxProject.GetUnityMainTargetGuid();
            pbxProject.SetBuildProperty(targetGuid, "ASSETCATALOG_COMPILER_INCLUDE_ALL_APPICON_ASSETS", "YES");

            var joinedIconNames = string.Join(" ", iconNames);
            pbxProject.SetBuildProperty(targetGuid, "ASSETCATALOG_COMPILER_ALTERNATE_APPICON_NAMES", joinedIconNames);
        }

        private static void SaveIcon(Texture2D sourceTexture, Texture2D manualTexture, int size, string savePath)
        {
            if (manualTexture == null)
            {
                var iconTexture = new Texture2D(0, 0);
                iconTexture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sourceTexture)));

                if (iconTexture.width != size || iconTexture.height != size)
                {
                    var renderTexture = new RenderTexture(size, size, 24);
                    var tmpRenderTexture = RenderTexture.active;
                    RenderTexture.active = renderTexture;
                    Graphics.Blit(iconTexture, renderTexture);
                    var resizedTexture = new Texture2D(size, size);
                    resizedTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    resizedTexture.Apply();
                    RenderTexture.active = tmpRenderTexture;
                    renderTexture.Release();
                    iconTexture = resizedTexture;
                }

                var pngBytes = iconTexture.EncodeToPNG();
                File.WriteAllBytes(savePath, pngBytes);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(manualTexture);
                File.Copy(path, savePath, true);
            }
        }

        private const string ContentsJsonText = @"{
  ""images"" : [
	iPhoneContents
	iPadContents
	{
	  ""filename"" : ""appStore1024px.png"",
	  ""idiom"" : ""ios-marketing"",
	  ""scale"" : ""1x"",
	  ""size"" : ""1024x1024""
	}
  ],
  ""info"" : {
    ""author"" : ""xcode"",
    ""version"" : 1
  }
}
";
	
		private const string ContentsiPhoneJsonText = @"{
	  ""filename"" : ""iPhoneNotification40px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""2x"",
	  ""size"" : ""20x20""
	},
	{
	  ""filename"" : ""iPhoneNotification60px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""3x"",
	  ""size"" : ""20x20""
	},
	{
	  ""filename"" : ""iPhoneSettings58px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""2x"",
	  ""size"" : ""29x29""
	},
	{
	  ""filename"" : ""iPhoneSettings87px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""3x"",
	  ""size"" : ""29x29""
	},
	{
	  ""filename"" : ""iPhoneSpotlight80px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""2x"",
	  ""size"" : ""40x40""
	},
	{
	  ""filename"" : ""iPhoneSpotlight120px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""3x"",
	  ""size"" : ""40x40""
	},
	{
	  ""filename"" : ""iPhoneApp120px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""2x"",
	  ""size"" : ""60x60""
	},
	{
	  ""filename"" : ""iPhoneApp180px.png"",
	  ""idiom"" : ""iphone"",
	  ""scale"" : ""3x"",
	  ""size"" : ""60x60""
	},
";

		private const string ContentsiPadJsonText = @"{
	  ""filename"" : ""iPadNotifications20px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""1x"",
	  ""size"" : ""20x20""
	},
	{
	  ""filename"" : ""iPadNotifications40px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""2x"",
	  ""size"" : ""20x20""
	},
	{
	  ""filename"" : ""iPadSettings29px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""1x"",
	  ""size"" : ""29x29""
	},
	{
	  ""filename"" : ""iPadSettings58px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""2x"",
	  ""size"" : ""29x29""
	},
	{
	  ""filename"" : ""iPadSpotlight40px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""1x"",
	  ""size"" : ""40x40""
	},
	{
	  ""filename"" : ""iPadSpotlight80px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""2x"",
	  ""size"" : ""40x40""
	},
	{
	  ""filename"" : ""iPadApp76px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""1x"",
	  ""size"" : ""76x76""
	},
	{
	  ""filename"" : ""iPadApp152px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""2x"",
	  ""size"" : ""76x76""
	},
	{
	  ""filename"" : ""iPadProApp167px.png"",
	  ""idiom"" : ""ipad"",
	  ""scale"" : ""2x"",
	  ""size"" : ""83.5x83.5""
	},
";
#endregion
#endif
    }
}