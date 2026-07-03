using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using AFramework.Analytics;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AFramework.UI
{
    public class BaseUIMenu : BaseUIComp
    {
        [SerializeField, HideInInspector]
        protected eUILayer _UILayer = eUILayer.Menu;
        public eUILayer UILayer { get { return _UILayer; } }
        [SerializeField, HideInInspector]
        protected bool _Unique = true;
        public bool IsUnique { get { return _Unique; } }
        [SerializeField, HideInInspector]
        protected bool _canDestroy = false;
        public bool CanDestroy { get { return _canDestroy; } }
        [SerializeField, HideInInspector]
        protected bool _trackMenu = false;
        public bool TrackMenu { get { return _trackMenu; } }

        public string UIIdentifier { get; set; }

        public object[] InitParamsCached { get; set; }

        CancellationTokenSource _deactiveCancellationTokenSource;
        internal protected bool _isBeginShown = false;

        public CancellationToken DeactiveCancellationToken
        {
            get
            {
                if (_deactiveCancellationTokenSource == null) _deactiveCancellationTokenSource = new CancellationTokenSource();
                return _deactiveCancellationTokenSource.Token;
            }
        }

        public virtual void Init(object[] initParams) {
            InitParamsCached = initParams;
#if USE_CHEAT && TJI_LOG_TMP_FONT
            Dictionary<string, TextMeshProUGUI> dict = transform.GetAllChildsAndPathRecursively<TextMeshProUGUI>();

            foreach (KeyValuePair<string, TextMeshProUGUI> e in dict)
            {
                Dictionary < string, object> param = new Dictionary<string, object> ();
                param["menu"] = e.Key;
                if (e.Value.font != null)
                {
                    param["font"] = e.Value.font.name;
                    Debug.LogError(e.Key + ": " + e.Value.font.name);
                    TrackingManager.I.TrackEventLocal("TMP_FONT", param);

                }    
                else
                {
                    param["font"] = "null";
                    Debug.LogError(e.Key + ": null");
                    TrackingManager.I.TrackEventLocal("TMP_FONT", param);
                }    
            }
#endif
        }

        public virtual void ShowWithoutInit()
        {
            if(!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public virtual void Pop()
        {
            AFramework.UI.CanvasManager.PopSelf(this);
            if (_deactiveCancellationTokenSource != null)
            {
                _deactiveCancellationTokenSource.Cancel();
                _deactiveCancellationTokenSource.Dispose();
                _deactiveCancellationTokenSource = null;
            }
        }

        public virtual void HandleSafeChoice()
        {
#if UNITY_EDITOR
            Debug.Log("Need to support for this menu " + UIIdentifier);
#endif
        }

        public virtual void HandleNextChoice()
        {
#if UNITY_EDITOR
            Debug.Log("Need to support for this menu " + UIIdentifier);
#endif
        }

        public virtual void HandleOtherKeys() { }

        #region MenuActiveTime
        public float MenuActiveTime { get; protected set; }
        public void UpdateActiveTime(float delta) { MenuActiveTime += delta; }
        public void ResetActiveTime() { MenuActiveTime = 0; }

        public int MenuOpenNumber { get; internal set; }
        #endregion
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(BaseUIMenu), true)]
    public class BaseUIMenuEditor : BaseUICompEditor
    {
        static protected bool UIConfigExpand = false;
        protected SerializedProperty PropUILayer;
        protected SerializedProperty PropUnique;
        protected SerializedProperty PropCanDestroy;
        protected SerializedProperty PropTrackMenu;

        protected virtual void OnEnable()
        {
            PropUILayer = serializedObject.FindProperty("_UILayer");
            PropUnique = serializedObject.FindProperty("_Unique");
            PropCanDestroy = serializedObject.FindProperty("_canDestroy");
            PropTrackMenu = serializedObject.FindProperty("_trackMenu");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UIConfigExpand = EditorGUILayout.Foldout(UIConfigExpand, new GUIContent("Config", "Config data"));
            if (UIConfigExpand)
            {
                EditorGUILayout.PropertyField(PropUILayer, new GUIContent("Default UILayer", "Default layer when UI is show"));
                EditorGUILayout.PropertyField(PropUnique, new GUIContent("Unique", "Menu is unique or not"));
                EditorGUILayout.PropertyField(PropCanDestroy, new GUIContent("Can Destroy", "Menu is destroy when load ingame"));
                EditorGUILayout.PropertyField(PropTrackMenu, new GUIContent("Track Menu", "Track event MENU when disable"));
            }
            //EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
#endif
}