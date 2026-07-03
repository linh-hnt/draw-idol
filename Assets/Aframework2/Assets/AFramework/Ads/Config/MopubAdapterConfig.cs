using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AFramework.Ads
{
    //[CreateAssetMenu(menuName = "ScriptableObject/AFramework/Ads/MopubAdapterConfig")]
    public class MopubAdapterConfig : BaseAdapterConfig
    {
        public GameObject MopubAdapterObj;

#if USE_MOPUB_ADS
#if UNITY_EDITOR
        const string MopubManagerObjSuffix = "_mopubmanager";
        public void BuildMopubAdapterObj()
        {
            var currentPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            {
                int lastCharIndex = currentPath.LastIndexOf('/');
                if (lastCharIndex < 0) lastCharIndex = currentPath.LastIndexOf('\\');
                currentPath = currentPath.Substring(0, lastCharIndex + 1);
            }

            bool isDirty = false;
            if (this.MopubAdapterObj == null || this.MopubAdapterObj.GetComponent<MoPubManager>() == null)
            {
                GenerateMopubObject(currentPath);
                isDirty = true;
            }

            // Find all NetworkConfig-derived classes.
            var availableConfigs = System.AppDomain.CurrentDomain.GetAssemblies()
                          .SelectMany(a => a.GetTypes(), (a, t) => t)
                          .Where(t => t.IsSubclassOf(typeof(MoPubNetworkConfig)) && !t.IsAbstract)
                          .OrderBy(t => t.Name)
                          .ToArray();
            if (!isDirty)
            {
                var currentConfigs = this.MopubAdapterObj.GetComponents<MoPubNetworkConfig>();
                if (currentConfigs == null || currentConfigs.Length != availableConfigs.Length)
                {
                    GenerateMopubObject(currentPath);
                    isDirty = true;
                }
                else
                {
                    bool different = false;
                    var allComponents = this.MopubAdapterObj.GetComponents<MonoBehaviour>();
                    for (int i = 0; i < allComponents.Length && !different; ++i)
                    {
                        if (allComponents[i] == null) different = true;
                    }

                    for (int i = 0; i < currentConfigs.Length && !different; ++i)
                    {
                        bool found = false;
                        var currentConfigType = currentConfigs[i].GetType();
                        for (int j = 0; j < availableConfigs.Length; ++j)
                        {
                            if (currentConfigType == availableConfigs[j])
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found) different = true;
                    }
                    if (different)
                    {
                        GenerateMopubObject(currentPath);
                        isDirty = true;
                    }
                }
            }

            foreach(var config in availableConfigs)
            {
                if (this.MopubAdapterObj.GetComponent(config) == null)
                {
                    this.MopubAdapterObj.AddComponent(config);
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }

        void GenerateMopubObject(string path)
        {
            if (this.MopubAdapterObj != null)
            {
                var deletePath = UnityEditor.AssetDatabase.GetAssetPath(this.MopubAdapterObj);
                this.MopubAdapterObj = null;
                UnityEditor.AssetDatabase.DeleteAsset(deletePath);
            }

            GameObject tempObj = new GameObject(this.name + MopubManagerObjSuffix, typeof(MoPubManager));
            tempObj.transform.position = Vector3.zero;
            tempObj.transform.localScale = Vector3.one;
            var localPath = path + tempObj.name + ".prefab";
            localPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(localPath);
            this.MopubAdapterObj = UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(tempObj, localPath, UnityEditor.InteractionMode.AutomatedAction);
            DestroyImmediate(tempObj);
        }
#endif
#endif
    }
}