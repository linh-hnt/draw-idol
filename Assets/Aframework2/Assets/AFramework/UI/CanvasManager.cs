using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
#if AF_ADDRESSABLES_UI
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AFramework.UI
{
    public enum eUILayer
    {
        Background = 0,
        Menu,
        Popup,
        AlwaysOnTop
    }

    public class CanvasManager : AFramework.SingletonMono<CanvasManager>
    {
        [SerializeField] GameObject BGPopupPrefab;
        public static System.Action<BaseUIMenu> EventOnMenuPushed;
        public static System.Action<BaseUIMenu> EventOnMenuPopped;

        static Canvas _UICanvas;

        public static Canvas UICanvas
        {
            get { return _UICanvas; }
        }

        public static float ScreenScale { get; protected set; }

        static RectTransform _UIRectTrans;
        public static RectTransform UIRectTrans { get { return _UIRectTrans; } }
        static RectTransform _AdsRectTrans;
        public static RectTransform AdsRectTrans { get { return _AdsRectTrans; } }

        static string DefaultDataPath;
        static bool sOnlyShowTopPopup;
        static Dictionary<string, Stack<BaseUIMenu>> UICached = new Dictionary<string, Stack<BaseUIMenu>>();
        static List<List<BaseUIMenu>> OpenedUIStack = new List<List<BaseUIMenu>>();
#if AF_ADDRESSABLES_UI
        static Dictionary<string, AsyncOperationHandle> ResourceHandleCached = new Dictionary<string, AsyncOperationHandle>();
#endif

#if UNITY_EDITOR
        static bool sFinishAwake = false;
#endif
        static GameObject sPopupBG = null;
        GraphicRaycaster uiRaycaster = null;

        protected virtual void Awake()
        {
            UICached.Clear();
            OpenedUIStack.Clear();
            uiRaycaster = this.GetComponent<GraphicRaycaster>();
            _UICanvas = this.GetComponent<Canvas>();
            _UIRectTrans = new GameObject("UI", typeof(RectTransform)).GetComponent<RectTransform>();
            _UIRectTrans.SetParent(this.transform);
            SetFullScreenRect(_UIRectTrans);
            _AdsRectTrans = Instantiate(_UIRectTrans, this.transform);
            _AdsRectTrans.name = "Ads";

            var layers = System.Enum.GetNames(typeof(eUILayer));
            for (int i = 0; i < layers.Length; ++i)
            {
                var newLayer = new GameObject(layers[i], typeof(RectTransform));
                newLayer.transform.SetParent(_UIRectTrans.transform);
                SetFullScreenRect(newLayer.GetComponent<RectTransform>());
                OpenedUIStack.Add(new List<BaseUIMenu>());

                if (i == (int)eUILayer.Popup)
                {
                    if (I.BGPopupPrefab != null)
                    {
                        sPopupBG = Instantiate(BGPopupPrefab, newLayer.transform);
                        sPopupBG.SetActive(false);
                    }
                }
            }

            ScreenScale = UICanvas.pixelRect.size.y / UICanvas.scaleFactor / 1080;

#if UNITY_EDITOR
            sFinishAwake = true;
#endif
        }

        void SetFullScreenRect(RectTransform target)
        {
            target.transform.localPosition = Vector3.zero;
            target.transform.localEulerAngles = Vector3.zero;
            target.transform.localScale = Vector3.one;
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        public static void SetAdsBannerSizeByRatio(bool top, float ratioByWidth)
        {
            SetAdsBannerSize(top, Mathf.CeilToInt(_AdsRectTrans.rect.width * ratioByWidth));
        }

        public static void SetAdsBannerSize(bool top, int height)
        {
            _UIRectTrans.offsetMin = new Vector2(_UIRectTrans.offsetMin.x, top ? 0 : height);
            _UIRectTrans.offsetMax = new Vector2(_UIRectTrans.offsetMin.x, top ? -height : 0);
            _AdsRectTrans.offsetMin = new Vector2(_AdsRectTrans.offsetMin.x, top ? _UIRectTrans.rect.height : 0);
            _AdsRectTrans.offsetMax = new Vector2(_AdsRectTrans.offsetMin.x, top ? 0 : -_UIRectTrans.rect.height);
        }

        public static void SetAdsBannerSize(bool top, int height, eUILayer layer)
        {
            var layerTrans = _UIRectTrans.GetChild((int)layer).GetComponent<RectTransform>();
            layerTrans.offsetMin = new Vector2(_UIRectTrans.offsetMin.x, top ? 0 : height);
            layerTrans.offsetMax = new Vector2(_UIRectTrans.offsetMin.x, top ? -height : 0);
            _AdsRectTrans.offsetMin = new Vector2(_AdsRectTrans.offsetMin.x, top ? _UIRectTrans.rect.height : 0);
            _AdsRectTrans.offsetMax = new Vector2(_AdsRectTrans.offsetMin.x, top ? 0 : -_UIRectTrans.rect.height);
        }

        public static void SetBannerBackgroundColor(Color input)
        {
            Image img = _AdsRectTrans.GetComponent<Image>();
            if (img == null)
            {
                img = _AdsRectTrans.gameObject.AddComponent<Image>();
            }

            img.color = input;
        }

        public static void SetBannerBackgroundSprite(Sprite input)
        {
            Image img = _AdsRectTrans.GetComponent<Image>();
            if (img == null)
            {
                img = _AdsRectTrans.gameObject.AddComponent<Image>();
            }

            img.sprite = input;
        }

#if !AF_ADDRESSABLES_UI
        public static BaseUIMenu Init(string dataPath, string defaultMenuIdentifier, bool onlyShowTopPopup = false)
        {
#if UNITY_EDITOR
            if (!sFinishAwake) Debug.LogError("[ERROR] CanvasManager priority is not set correctly!!!");
#endif
            sOnlyShowTopPopup = onlyShowTopPopup;
            DefaultDataPath = dataPath;
            if (string.IsNullOrEmpty(defaultMenuIdentifier)) return null;
            return Push(defaultMenuIdentifier, null);
        }

        public static BaseUIMenu TryCacheUI(string identifier)
        {
            bool is_new = false;
            if (!UICached.ContainsKey(identifier))
            {
                UICached[identifier] = new Stack<BaseUIMenu>();
                is_new = true;
            }
            else if (UICached[identifier].Count > 0)
            {
                return null;
            }

            var prefab = Resources.Load<BaseUIMenu>(DefaultDataPath + identifier);
            var cached = Instantiate(prefab, _UIRectTrans.GetChild((int)prefab.UILayer));
            cached.UIIdentifier = identifier;
            UICached[identifier].Push(cached);

#if UNITY_EDITOR
            if (!is_new && cached.IsUnique) Debug.LogError(string.Format("UI {0} is Unique!!!", identifier));
#endif

            return cached;
        }

        public static BaseUIMenu Push(string identifier, object[] initParams)
        {
            TryCacheUI(identifier);
            return PushNoneCache(identifier, initParams);
        }
#endif

        public static async Task<BaseUIMenu> InitAsync(string dataPath, string defaultMenuIdentifier, bool use_addressable = true)
        {
#if UNITY_EDITOR
            if (!sFinishAwake) Debug.LogError("[ERROR] CanvasManager priority is not set correctly!!!");
#endif
            DefaultDataPath = dataPath;
            return await PushAsync(defaultMenuIdentifier, null, use_addressable);
        }

        public static void InitAsync(string dataPath, string defaultMenuIdentifier, System.Action<BaseUIMenu> callback, bool use_addressable = true)
        {
#if UNITY_EDITOR
            if (!sFinishAwake) Debug.LogError("[ERROR] CanvasManager priority is not set correctly!!!");
#endif
            DefaultDataPath = dataPath;
            PushAsync(defaultMenuIdentifier, null, callback, use_addressable);
        }

        public static async Task<BaseUIMenu> TryCacheUIAsync(string identifier, bool use_addressable = true, bool deactive = true)
        {
            bool is_new = false;
            if (!UICached.ContainsKey(identifier))
            {
                UICached[identifier] = new Stack<BaseUIMenu>();
                is_new = true;
            }
            else if (UICached[identifier].Count > 0)
            {
                return null;
            }
            BaseUIMenu prefab = null;
            var load_path = DefaultDataPath + identifier;

#if AF_ADDRESSABLES_UI
            if (use_addressable)
            {
                if (!ResourceHandleCached.ContainsKey(load_path))
                {
                    var handle = Addressables.LoadAssetAsync<GameObject>(load_path);
                    await handle;
                    ResourceHandleCached[load_path] = handle;
                }

                prefab = (ResourceHandleCached[load_path].Result as GameObject).GetComponent<BaseUIMenu>();
            }
            else
#endif
            {
                var load_thread = Resources.LoadAsync<BaseUIMenu>(load_path);
                while (!load_thread.isDone)
                {
                    await Task.Yield();
                }
                prefab = load_thread.asset as BaseUIMenu;
            }

            var cached = Instantiate(prefab, _UIRectTrans.GetChild((int)prefab.UILayer));
            cached.UIIdentifier = identifier;
            UICached[identifier].Push(cached);

#if UNITY_EDITOR
            if (!is_new && cached.IsUnique) Debug.LogError(string.Format("UI {0} is Unique!!!", identifier));
#endif
            if (deactive) cached.gameObject.SetActive(false);
            return cached;
        }

        public static async Task<BaseUIMenu> PushAsync(string identifier, object[] initParams, bool use_addressable = true)
        {
            await TryCacheUIAsync(identifier, use_addressable, false);
            return PushNoneCache(identifier, initParams);
        }

        public static async void PushAsync(string identifier, object[] initParams, System.Action<BaseUIMenu> callback, bool use_addressable = true)
        {
            await TryCacheUIAsync(identifier, use_addressable, false);
            var menu = PushNoneCache(identifier, initParams);
            if (callback != null) callback.Invoke(menu);
        }

        public static void RemoveFromCache(string identifier)
        {
            var menu = GetMenu(identifier);
            if (menu.isActiveAndEnabled)
            {
                menu.Pop();
            }
            UICached.Remove(identifier);
            foreach (List<BaseUIMenu> list in OpenedUIStack)
            {
                list.Remove(menu);
            }
            Destroy(menu.gameObject);
        }
        
        public static BaseUIMenu PushNoneCache(string identifier, object[] initParams)
        {
            BaseUIMenu menu = UICached[identifier].Pop();
            if (menu.UILayer == eUILayer.Menu && OpenedUIStack[(int)eUILayer.Popup].Count > 0)
            {
                PopAllLayer(eUILayer.Popup);
            }
            ++menu.MenuOpenNumber;
            menu._isBeginShown = true;

            menu.gameObject.SetActive(true);
            menu.transform.SetAsLastSibling();
            BaseUIMenu lastMenuSameLayer = null;
            if (OpenedUIStack[(int)menu.UILayer].Count > 0)
            {
                lastMenuSameLayer = OpenedUIStack[(int)menu.UILayer][OpenedUIStack[(int)menu.UILayer].Count - 1];
            }

            OpenedUIStack[(int)menu.UILayer].Add(menu);

            if (menu.UILayer == eUILayer.Popup && sOnlyShowTopPopup)
            {
                if (lastMenuSameLayer != null)
                {
                    lastMenuSameLayer.gameObject.SetActive(false);
                }
                else if (sPopupBG != null)
                {
                    sPopupBG.SetActive(true);
                    Animator ani = sPopupBG.GetComponent<Animator>();
                    if (ani != null)
                    {
                        ani.Play("BackgroundScale");
                    }
                }
            }

            menu.Init(initParams);
            menu.ResetActiveTime();

            if (EventOnMenuPushed != null)
            {
                EventOnMenuPushed(menu);
            }

            menu._isBeginShown = false;

            return menu;
        }

        public static void PopTop(eUILayer layer)
        {
            if (OpenedUIStack[(int)layer].Count <= 0)
            {
                return;
            }

            var layerGroup = OpenedUIStack[(int)layer];
            BaseUIMenu menu = layerGroup[layerGroup.Count - 1];
            menu.Pop();
        }

        public static bool PopSelf(BaseUIMenu menu, bool destroy = false)
        {
            if (OpenedUIStack[(int)menu.UILayer].Count <= 0)
            {
                return true;
            }

            BaseUIMenu lastMenuSameLayer = null;
            if (OpenedUIStack[(int)menu.UILayer].Count > 1)
            {
                lastMenuSameLayer = OpenedUIStack[(int)menu.UILayer][OpenedUIStack[(int)menu.UILayer].Count - 2];
            }

            if (menu.UILayer == eUILayer.Popup && sOnlyShowTopPopup)
            {
                if (lastMenuSameLayer != null)
                {
                    lastMenuSameLayer.ShowWithoutInit();
                }
            }

            var layerGroup = OpenedUIStack[(int)menu.UILayer];
            var index = layerGroup.FindIndex((x) => x == menu);
            if (index >= 0)
            {
                if (menu.TrackMenu)// && menu.UILayer == eUILayer.Menu)
                    AFramework.Analytics.TrackingManager.I.TrackMenuActiveTime(menu.UIIdentifier, menu.MenuActiveTime);
                layerGroup.RemoveAt(index);


                if (lastMenuSameLayer == null && menu.UILayer == eUILayer.Popup && sPopupBG != null && sPopupBG.activeSelf)
                {
                    sPopupBG.SetActive(false);
                }

                if (destroy)
                {
                    Destroy(menu.gameObject);
                }
                else
                {
                    menu.gameObject.SetActive(false);
                    UICached[menu.UIIdentifier].Push(menu);
                }

                if (EventOnMenuPopped != null)
                {
                    EventOnMenuPopped(menu);
                }

                return true;
            }

            return false;
        }

        public static void StartHidePopup()
        {
            if (sOnlyShowTopPopup)
            {
                BaseUIMenu lastMenuSameLayer = null;
                if (OpenedUIStack[(int)eUILayer.Popup].Count > 1)
                {
                    lastMenuSameLayer = OpenedUIStack[(int)eUILayer.Popup][OpenedUIStack[(int)eUILayer.Popup].Count - 2];
                }

                if (lastMenuSameLayer != null)
                {
                    lastMenuSameLayer.ShowWithoutInit();
                }
            }

            if (OpenedUIStack[(int)eUILayer.Popup].Count == 1)
            {
                Animator ani = sPopupBG.GetComponent<Animator>();
                if (ani != null)
                {
                    ani.Play("BackgroundHide");
                }
            }
        }

        public static bool Pop(string identifier)
        {
            BaseUIMenu menu = null;
            for (int i = 0; i <= (int)eUILayer.AlwaysOnTop && menu == null; ++i)
            {
                menu = OpenedUIStack[i].Find((x) => x.UIIdentifier == identifier);
            }

            return menu != null ? PopSelf(menu) : false;
        }

        public static void PopAllLayer(eUILayer layer)
        {
            List<BaseUIMenu> popList = new List<BaseUIMenu>(OpenedUIStack[(int)layer].ToArray());
            for (int i = popList.Count - 1; i >= 0; --i)
            {
                BaseUIMenu menu = popList[i];
                menu.Pop();
            }
        }

        public static bool IsPopupShown()
        {
            return OpenedUIStack[(int)eUILayer.Popup].Count > 0;
        }

        public static BaseUIMenu GetTopPopup()
        {
            if (OpenedUIStack[(int)eUILayer.Popup].Count > 0)
            {
                return OpenedUIStack[(int)eUILayer.Popup][OpenedUIStack[(int)eUILayer.Popup].Count - 1];
            }

            return null;
        }

        public static BaseUIMenu GetCurrentMenu(eUILayer topLayer = eUILayer.AlwaysOnTop)
        {
            for (int i = (int)topLayer; i >= 0; --i)
            {
                if (OpenedUIStack[i].Count > 0)
                {
                    return OpenedUIStack[i][OpenedUIStack[i].Count - 1];
                }
            }

            return null;
        }

        public static BaseUIMenu GetCurrentMenuByLayer(eUILayer layer)
        {
            int i = (int)layer;
            if (OpenedUIStack[i].Count > 0)
            {
                return OpenedUIStack[i][OpenedUIStack[i].Count - 1];
            }

            return null;
        }

        public static BaseUIMenu IsSpecificUIShown(string identifier)
        {
            for (int i = 0; i < OpenedUIStack.Count; ++i)
            {
                var currentStack = OpenedUIStack[i];
                for (int j = 0; j < currentStack.Count; ++j)
                {
                    if (currentStack[j].UIIdentifier == identifier)
                    {
                        return currentStack[j];
                    }
                }
            }

            return null;
        }

        public static bool IsShow(string identifier)
        {
            return IsSpecificUIShown(identifier) != null;
        }

        public static int GetUIStackCount(eUILayer layer)
        {
            int i = (int)layer;
            return OpenedUIStack[i].Count;
        }

        public static BaseUIMenu GetMenu(string identifier, bool autoCreated = true)
        {
            var result = IsSpecificUIShown(identifier);
            if (result != null) return result;
            if (UICached.ContainsKey(identifier) && UICached[identifier].Count > 0)
            {
                result = UICached[identifier].Peek();
            }
            //else if (autoCreated)
            //{
            //    TryCacheUI(identifier);
            //    result = UICached[identifier].Peek();
            //}

            return result;
        }

        public static void AddUIToCache(BaseUIMenu menu)
        {
            if (menu.UIIdentifier == null)
                menu.UIIdentifier = menu.name;
            if (!UICached.ContainsKey(menu.UIIdentifier))
                UICached[menu.UIIdentifier] = new Stack<BaseUIMenu>();
            UICached[menu.UIIdentifier].Push(menu);
            menu.gameObject.SetActive(false);
        }

        public static void SetRenderCamera(Camera newCamera)
        {
            _UICanvas.worldCamera = newCamera;
        }

        float mLastKeyTime = -1;

        private void Update()
        {
            var topMenuLayer = GetCurrentMenuByLayer(eUILayer.Menu);
            if (topMenuLayer != null) topMenuLayer.UpdateActiveTime(Time.unscaledDeltaTime);

            if ((sSystemLoadingPopup == null || !sSystemLoadingPopup.activeSelf) && Application.isFocused && Input.anyKeyDown && mLastKeyTime < Time.unscaledTime)
            {
                mLastKeyTime = Time.unscaledTime + 0.15f;
                var topMenu = GetCurrentMenu();
                if (topMenu != null)
                {
                    if (Input.GetKey(KeyCode.Escape)) topMenu.HandleSafeChoice();
                    else if (Input.GetKey(KeyCode.Return)) topMenu.HandleNextChoice();
                    else topMenu.HandleOtherKeys();
                }
            }
        }

        public static GameObject sSystemLoadingPopup = null;

        public static void ShowSystemLoadingPopup(bool show)
        {
            if (sSystemLoadingPopup == null)
            {
                sSystemLoadingPopup = new GameObject("SystemLoadingPopup");
                sSystemLoadingPopup.transform.SetParent(UICanvas.transform);
                sSystemLoadingPopup.transform.localPosition = Vector3.zero;
                sSystemLoadingPopup.transform.localScale = Vector3.one;
                var rect = sSystemLoadingPopup.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.anchoredPosition = Vector2.zero;
                sSystemLoadingPopup.AddComponent<SystemLoadingPopup>();
            }

            sSystemLoadingPopup.SetActive(show);
        }
        public static bool IsSystemLoadingScreenShowing() { return sSystemLoadingPopup != null ? sSystemLoadingPopup.gameObject.activeSelf : false; }

        public static void DestroyAllUICanDestroy()
        {
            List<KeyValuePair<string, Stack<BaseUIMenu>>> listClear =
                new List<KeyValuePair<string, Stack<BaseUIMenu>>>();
            foreach (var group in UICached)
            {
                var list = new List<BaseUIMenu>();
                while (group.Value.Count > 0)
                {
                    var menu = group.Value.Pop();

                    var check = OpenedUIStack[(int)menu.UILayer].Contains(menu);
                    if (menu.CanDestroy && !check)
                    {
#if UNITY_EDITOR
                        Debug.Log("Destroy " + menu.UIIdentifier);
#endif
                        listClear.Add(group);
                        Destroy(menu.gameObject);
                    }
                    else
                    {
                        list.Add(menu);
                    }
                }

                foreach (var menu in list)
                    group.Value.Push(menu);
            }

            foreach (var pair in listClear)
            {
                if (pair.Value.Count <= 0)
                {
                    // Debug.Log("Destroy " + pair.Key);
                    UICached.Remove(pair.Key);

#if AF_ADDRESSABLES_UI
                    if (ResourceHandleCached.ContainsKey(pair.Key))
                    {
                        var handle = ResourceHandleCached[pair.Key];
                        Addressables.Release(handle);
                        ResourceHandleCached.Remove(pair.Key);
                    }
#endif
                }
            }
        }

        public static BaseUIMenu[] GetAllOpenedUI()
        {
            List<BaseUIMenu> result = new List<BaseUIMenu>();
            for (int i = 0; i < OpenedUIStack.Count; ++i)
            {
                var childList = OpenedUIStack[i];
                for (int j = 0; j < childList.Count; ++j)
                {
                    result.Add(childList[j]);
                }
            }
            return result.ToArray();
        }

        public static void OpenSystemPopupInfo(string title, string message, string ok, System.Action callback)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(title, message, ok);
            callback?.Invoke();
#else
            pingak9.NativeDialog.OpenDialog(title, message, ok, callback);
#endif
        }

        public static void OpenSystemPopupConfirm(string title, string message, string yes, string no, System.Action<bool> callback)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.DisplayDialog(title, message, yes, no))
            {
                callback?.Invoke(true);
            }
            else
            {
                callback?.Invoke(false);
            }
#else
            pingak9.NativeDialog.OpenDialog(title, message, yes, no, () => { callback?.Invoke(true); }, () => { callback?.Invoke(false); });
#endif
        }

        public void DisableTouch()
        {
            if (uiRaycaster != null) uiRaycaster.enabled = false;
        }

        public void EnableTouch()
        {
            if (uiRaycaster != null) uiRaycaster.enabled = true;
        }
    }
}