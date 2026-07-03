#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AFramework.ExtensionMethods
{
    public static class IEditorExtension
    {
#if UNITY_EDITOR
        [MenuItem("GameObject/---Apply Prefabs", false, 0)]
        public static void ApplyPrefab()
        {
            var prefabs = new List<GameObject>();
            foreach (var gameObject in Selection.gameObjects)
            {
                var rootPrefab = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
                if (!prefabs.Contains(rootPrefab))
                    prefabs.Add(rootPrefab);
            }

            foreach (var prefab in prefabs)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(prefab,
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab), InteractionMode.AutomatedAction);
            }
        }
#endif

        #region AsssetDatabase Helper

        public static void SetDirty(Object obj)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        public static void SaveAssetDatabase(Object obj, bool isRefresh = false)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            if (isRefresh)
                AssetDatabase.Refresh();
#endif
        }

        public static List<T> GetAllAssetAtPath<T>(string filter)
        {
#if UNITY_EDITOR
            string[] findAssets = AssetDatabase.FindAssets(filter);
            List<T> os = new List<T>();
            foreach (var findAsset in findAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(findAsset);
                os.Add((T) Convert.ChangeType(
                    AssetDatabase.LoadAssetAtPath(path, typeof(T)), typeof(T)));
            }

            return os;
#endif
            return null;
        }

        public static Sprite GetSpriteAtPath(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
            return null;
        }

        public static List<T> GetAllAssetAtPath<T>(string filter, string path)
        {
#if UNITY_EDITOR
            string[] findAssets = AssetDatabase.FindAssets(filter, new[] {path});
            List<T> os = new List<T>();
            foreach (var findAsset in findAssets)
            {
                os.Add((T) Convert.ChangeType(
                    AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(findAsset),
                        typeof(T)), typeof(T)));
            }

            return os;
#endif
            return null;
        }

        public static List<Object> GetAllAssetsAtPath(string path)
        {
#if UNITY_EDITOR
            string[] paths = {path};
            var assets = AssetDatabase.FindAssets(null, paths);
            var assetsObj = assets.Select(s =>
                AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(s))).ToList();
            return assetsObj;
#endif
            return null;
        }

        public static List<Sprite> GetAllSpriteAssetsAtPath(string path)
        {
#if UNITY_EDITOR
            string[] paths = {path};
            var assets = AssetDatabase.FindAssets("t:sprite", paths);
            var assetsObj = assets.Select(s =>
                AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(s))).ToList();
            return assetsObj;
#endif
            return null;
        }

        public static List<GameObject> GetAllGameObjectAssetsAtPath(string path)
        {
#if UNITY_EDITOR
            string[] paths = {path};
            var assets = AssetDatabase.FindAssets("t:Object", paths);
            var assetsObj = assets.Select(s =>
                AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(s))).ToList();
            return assetsObj;
#endif
            return null;
        }

        #endregion
    }
}