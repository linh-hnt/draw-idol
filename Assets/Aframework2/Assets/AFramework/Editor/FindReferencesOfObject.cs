using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

using UnityEngine;

public class FindReferencesOfObject : EditorWindow
{
    public static FindReferencesOfObject I;
    public List<GameObject> GameObjects = new List<GameObject>();
    private Vector2 scrollPos;

    [MenuItem("GameObject/---Find References Of Object", false, 0)]
    public static void Find()
    {
        Init();
        I.DoFindRefOfObject(Selection.activeGameObject, true);
    }

    static void Init()
    {
        var w = GetWindow<FindReferencesOfObject>();
        I = w;
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Find"))
        {
            if (Selection.activeGameObject != null)
            {
                DoFindRefOfObject(Selection.activeGameObject, false);
            }
            else
            {
                Debug.LogError("No target object");
            }
        }

        if (GUILayout.Button("Find Deep"))
        {
            if (Selection.activeGameObject != null)
            {
                DoFindRefOfObject(Selection.activeGameObject, true);
            }
            else
            {
                Debug.LogError("No target object");
            }
        }

        GUILayout.EndHorizontal();
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        foreach (var gameObject in GameObjects)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping", GUILayout.ExpandWidth(false)))
                Selection.activeGameObject = gameObject;
            GUILayout.Label(gameObject.name);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    void DoFindRefOfObject(GameObject obj, bool isDeep)
    {
        GameObjects.Clear();
        var transforms = isDeep
            ? Resources.FindObjectsOfTypeAll<Transform>().ToList()
            : FindObjectsOfType<Transform>().ToList();
#if UNITY_2021_2_OR_NEWER
        var prefab = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
        var prefab = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
        if (prefab != null)
        {
            transforms = prefab.prefabContentsRoot.transform.GetComponentsInChildren<Component>()
                .Select(s => s.transform).ToList();
            transforms.AddRange(prefab.prefabContentsRoot.transform.GetComponents<Component>()
                .Select(s => s.transform).ToList());
        }

        foreach (var transform in transforms)
        {
            var components = transform.GetComponents<Component>();
            foreach (var component in components)
            {
                var fields = component.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                                           System.Reflection.BindingFlags.NonPublic |
                                                           System.Reflection.BindingFlags
                                                               .Instance);

                foreach (var fieldInfo in fields)
                {
                    var value = fieldInfo.GetValue(component);
                    var c = value as Component;
                    if (c != null)
                    {
                        if (c.gameObject == obj)
                            if (!GameObjects.Contains(component.gameObject))
                                GameObjects.Add(component.gameObject);
                    }
                    else if (value is GameObject)
                    {
                        if (value == obj)
                            if (!GameObjects.Contains(component.gameObject))
                                GameObjects.Add(component.gameObject);
                    }
                }
            }
        }
    }
}