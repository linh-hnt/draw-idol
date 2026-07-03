using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AFramework
{
    public class LocalFirebaseRemoteConfigUI : MonoBehaviour
    {
        [Header("Load Config")]
        [SerializeField] string _jsonFolderUrl = "https://dev-file-config.funnii.net/config/filelist?path=BubbleClassicData/RemoteConfig";
        [SerializeField] Dropdown _jsonAvailable;
        [SerializeField] Button _refreshFolderBtn;
        [SerializeField] Button _loadFileBtn;

        [Header("Use/Reset Config")]
        [SerializeField] Button _applyBtn;
        [SerializeField] Button _resetBtn;
        [SerializeField] Button _exitBtn;

        [Header("UI Scroller")]
        [SerializeField] ScrollRect scroller;
        [SerializeField] Toggle _conditionTogglePrefab;
        [SerializeField] Transform conditionHolder;
        [SerializeField] LocalFirebaseRemoteConfigParamUI _paramPrefab;
        [SerializeField] Transform paramHolder;
        [SerializeField] Button _closeRemoteTextViewBtn;
        [SerializeField] GameObject _remoteTextScroll;
        [SerializeField] Text _contentTextView;

#if (USE_FIREBASE && USE_FIREBASE_REMOTECONFIG) && (UNITY_EDITOR || LOCAL_FIREBASEREMOTECONFIG)
        FirebaseRemoteJson _currentConfig;
        List<ConditionToggleObj> _allConditionList = new();
        List<LocalFirebaseRemoteConfigParamUI> _allParamList = new();
        string _downloadUrl;
        GameRemoteConfig _gameRemoteConfig;

        private void Awake()
        {
            _gameRemoteConfig = FindObjectOfType<GameRemoteConfig>();
            _conditionTogglePrefab.gameObject.SetActive(false);
            _paramPrefab.gameObject.SetActive(false);
            if (FirebaseService.FirebaseRemoteConfig.IsLocalConfig && _currentConfig == null)
            {
                _currentConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<FirebaseRemoteJson>(
                    PlayerPrefs.GetString(FirebaseService.FirebaseRemoteConfig.LOCAL_JSON));
                _currentConfig.Init();
            }

            _refreshFolderBtn.onClick.AddListener(() => {
                StartCoroutine(getListFiles(onJsonListCallback));
            });
            _refreshFolderBtn.onClick.Invoke();

            _loadFileBtn.onClick.AddListener(() => {
                StartCoroutine(loadSelectedJson());
            });

            _applyBtn.onClick.AddListener(() => {
                if (_currentConfig == null) return;
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_currentConfig);
                _gameRemoteConfig.UpdateLocalConfig(json);
                this.gameObject.SetActive(false);
            });

            _resetBtn.onClick.AddListener(() => {
                _currentConfig = null;
                PlayerPrefs.SetInt(FirebaseService.FirebaseRemoteConfig.LOCAL_STATUS, 0);
                PlayerPrefs.DeleteKey(FirebaseService.FirebaseRemoteConfig.LOCAL_JSON);
                _gameRemoteConfig.UpdateLocalConfig(null);
                this.gameObject.SetActive(false);
            });

            _exitBtn.onClick.AddListener(() => {
                this.gameObject.SetActive(false);
            });

            _closeRemoteTextViewBtn.onClick.AddListener(() => {
                _closeRemoteTextViewBtn.gameObject.SetActive(false);
                _remoteTextScroll.SetActive(false);
            });
        }

        public void OnEnable()
        {
            if (_gameRemoteConfig == null || _gameRemoteConfig.GenConfig == null)
            {
                Debug.LogError("Gen config is not available");
                return;
            }

            _closeRemoteTextViewBtn.gameObject.SetActive(false);
            _remoteTextScroll.SetActive(false);

            UpdateUI();
        }

        async void UpdateUI()
        {
            if (_currentConfig == null)
            {
                conditionHolder.gameObject.SetActive(false);
                paramHolder.gameObject.SetActive(false);
                return;
            }

            while (_allConditionList.Count < _currentConfig.ConditionCount)
            {
                var toggle = Instantiate(_conditionTogglePrefab, conditionHolder);
                ConditionToggleObj newToggle = new ConditionToggleObj(toggle);
                _allConditionList.Add(newToggle);
            }

            List<RemoteConfigGenData.RemoteConfigInfo> configInfoList = new List<RemoteConfigGenData.RemoteConfigInfo>(_gameRemoteConfig.GenConfig.ConfigInfos);
            if (configInfoList.Find(x => x.key == FirebaseService.FirebaseRemoteConfig.AF_CUSTOM_EVENT) == null)
            {
                configInfoList.Add(new RemoteConfigGenData.RemoteConfigInfo() {
                    key = FirebaseService.FirebaseRemoteConfig.AF_CUSTOM_EVENT,
                    type = RemoteConfigGenData.ConfigDataType.LIST_CLASS,
                    className = "CustomRuleEvent"
                });
            }
            while (_allParamList.Count < configInfoList.Count)
            {
                var item = Instantiate(_paramPrefab, paramHolder);
                _allParamList.Add(item);
            }

            //update condition
            int index = 0;
            for (int count = _currentConfig.ConditionCount; index < count; ++index)
            {
                var info = _currentConfig.conditions[index];
                var toggleObj = _allConditionList[index];

                toggleObj.Toggle.isOn = _currentConfig.ConditionStatus(info.name);
                toggleObj.Toggle.onValueChanged.RemoveAllListeners();
                toggleObj.Toggle.onValueChanged.AddListener((val) => {
                    if (_currentConfig.selectedCondition == null) _currentConfig.selectedCondition = new List<string>();
                    _currentConfig.selectedCondition.Remove(info.name);
                    if (val) _currentConfig.selectedCondition.Add(info.name);
                });
                toggleObj.Text.text = string.Format("{0}\n<size=23>{1}</size>", info.name, info.expressionParsed);
                toggleObj.Background.color = info.color;

                toggleObj.Obj.SetActive(true);
            }

            for (int count = _allConditionList.Count; index < count; ++index)
            {
                _allConditionList[index].Obj.SetActive(false);
            }

            conditionHolder.gameObject.SetActive(true);

            //update param
            index = 0;
            for (int count = configInfoList.Count; index < count; ++index)
            {
                var configInfo = configInfoList[index];
                var uiItem = _allParamList[index];
                UpdateParamItem(uiItem, configInfo, _currentConfig);
            }

            for (int count = _allParamList.Count; index < count; ++index)
            {
                _allParamList[index].gameObject.SetActive(false);
            }

            paramHolder.gameObject.SetActive(true);

            //refresh scroller
            scroller.gameObject.SetActive(false);
            await System.Threading.Tasks.Task.Delay(100);
            scroller.gameObject.SetActive(true);
        }

        void UpdateParamItem(LocalFirebaseRemoteConfigParamUI uiItem, RemoteConfigGenData.RemoteConfigInfo configInfo, FirebaseRemoteJson configRemote)
        {
            var param = configRemote.AddParam(configInfo.key);

            uiItem.ParamName.text = configInfo.key;

            uiItem.ForceToggle.isOn = param.forceValue != null;
            uiItem.ForceToggle.onValueChanged.RemoveAllListeners();
            uiItem.ForceToggle.onValueChanged.AddListener((val) => {
                param.forceValue = val ? new FirebaseRemoteJson.FirebaseParameter.FirebaseParamValue() { value = uiItem.ForceInput.text } : null;
                UpdateParamItem(uiItem, configInfo, configRemote);
            });

            uiItem.ForceInput.text = param.forceValue != null ? param.forceValue.value : string.Empty;
            uiItem.ForceInput.onValueChanged.RemoveAllListeners();
            uiItem.ForceInput.onValueChanged.AddListener((val) => {
                if (param.forceValue != null)
                    param.forceValue.value = val;
            });

            //uiItem.ForceInput
            uiItem.ForceVerifyBtn.onClick.RemoveAllListeners();
            uiItem.ForceVerifyBtn.onClick.AddListener(() =>
            {
                bool success = VerifyParam(uiItem.ForceInput.text, configInfo);
                uiItem.ForceInput.textComponent.color = success ? Color.black : Color.red;
            });

            bool hasHighlight = uiItem.ForceToggle.isOn;
            string str = string.Empty;
            string fullContentString = string.Empty;
            if (param.conditionalValuesParsed != null)
            {
                foreach (string key in param.conditionalValuesParsed.Keys)
                {
                    var keyObj = param.conditionalValuesParsed[key];
                    var val = (keyObj as FirebaseRemoteJson.FirebaseParameter.FirebaseParamValue).value;
                    bool success = VerifyParam(val, configInfo);
                    if (string.IsNullOrEmpty(val) && !success) val = "Empty is ERROR";
                    if (!hasHighlight && _currentConfig.ConditionStatus(key))
                    {
                        hasHighlight = true;
                        str += string.Format("<color=green>{0}</color>: <color={1}>{2}</color>\n", key, success ? "white" : "red", val);
                    }
                    else
                    {
                        str += string.Format("{0}: <color={1}>{2}</color>\n", key, success ? "white" : "red", val);
                    }

                    if (val.StartsWith("[") || val.StartsWith("{"))
                    {
                        var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(val);
                        val = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                    }
                    fullContentString += (key + ":--------------------------------\n" + val + "\n");
                }
            }
            if (param.defaultValue != null)
            {
                var val = param.defaultValue.value;
                bool success = VerifyParam(val, configInfo);
                if (string.IsNullOrEmpty(val) && !success) val = "Empty is ERROR";
                if (!hasHighlight)
                {
                    hasHighlight = true;
                    str += string.Format("<color=green>{0}</color>: <color={1}>{2}</color>\n", "Default:", success ? "white" : "red", val);
                }
                else
                {
                    str += string.Format("{0}: <color={1}>{2}</color>\n", "Default:", success ? "white" : "red", val);
                }

                if (val.StartsWith("[") || val.StartsWith("{"))
                {
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(val);
                    val = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                }
                fullContentString += ("Default:--------------------------------\n" + val + "\n");
            }

            //Local
            if (configInfo.defaultData != null)
            {
                var val = configInfo.defaultData.current;
                bool success = VerifyParam(val, configInfo);
                if (string.IsNullOrEmpty(val) && !success) val = "Empty is ERROR";

                if (hasHighlight)
                {
                    str += string.Format("{0}: <color={1}>{2}</color>\n", "Local: ", success ? "white" : "red", val);
                }
                else
                {
                    str += string.Format("<color=green>{0}</color>: <color={1}>{2}</color>\n", "Local: ", success ? "white" : "red", val);
                }

                if (val.StartsWith("[") || val.StartsWith("{"))
                {
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(val);
                    val = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                }
                fullContentString += ("Local:--------------------------------\n" + val + "\n");
            }
            uiItem.ParamValueText.text = str;
            fullContentString += "\n\n\n\n\n\n\n\n\n\n";

            var rect = uiItem.GetComponent<RectTransform>();
            var size = rect.sizeDelta;
            size.y = (str.Count(c => c.Equals('\n')) + 1) * 50 + 140;
            rect.sizeDelta = size;

            uiItem.gameObject.SetActive(true);

            uiItem.ShowContentBtn.onClick.RemoveAllListeners();
            uiItem.ShowContentBtn.onClick.AddListener(() =>
            {
                _closeRemoteTextViewBtn.gameObject.SetActive(true);
                _contentTextView.text = fullContentString;
                _remoteTextScroll.SetActive(true);
            });
        }

        bool VerifyParam(string text, AFramework.RemoteConfigGenData.RemoteConfigInfo info)
        {
            bool success = false;
            if (info.customUpdate)
            {
                string functionName = info.CustomUpdateFunc;
                var mInfo = _gameRemoteConfig.GetType().GetMethod(functionName, BindingFlags.Instance | BindingFlags.NonPublic);
                success = (bool)mInfo.Invoke(_gameRemoteConfig, new object[] { true, text });
            }
            else if (info.type == RemoteConfigGenData.ConfigDataType.INT)
            {
                if (string.IsNullOrEmpty(text)) return true;//use local
                int val;
                success = int.TryParse(text, out val);
            }
            else if (info.type == RemoteConfigGenData.ConfigDataType.FLOAT)
            {
                if (string.IsNullOrEmpty(text)) return true;//use local
                float val;
                success = float.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out val);
            }
            else if (info.type == RemoteConfigGenData.ConfigDataType.STRING)
            {
                success = true;
            }
            else if (info.type == RemoteConfigGenData.ConfigDataType.BOOL)
            {
                bool val;
                if (string.IsNullOrEmpty(text)) success = true;
                else if (text.ToLowerInvariant() is "true" or "false" or "on" or "off" or "1" or "0") success = true;
                else success = bool.TryParse(text, out val);
            }
            else if(info.type == RemoteConfigGenData.ConfigDataType.ENUM)
            {
                if (string.IsNullOrEmpty(text)) return true;//use local
                var type = GetType(info.className);
                object val;
                success = System.Enum.TryParse(type, text, out val);
            }
            else if (info.type == RemoteConfigGenData.ConfigDataType.CLASS)
            {
                try
                {
                    var type = GetType(info.className);
                    Newtonsoft.Json.JsonConvert.DeserializeObject(text, type);
                    success = true;
                }
                catch (System.Exception e) { success = false; }
            }
            else
            {
                try
                {
                    Type type;
                    if (info.type == RemoteConfigGenData.ConfigDataType.LIST_INT) type = typeof(int);
                    else if (info.type == RemoteConfigGenData.ConfigDataType.LIST_FLOAT) type = typeof(float);
                    else if (info.type == RemoteConfigGenData.ConfigDataType.LIST_STRING) type = typeof(string);
                    else if (info.type == RemoteConfigGenData.ConfigDataType.LIST_BOOL) type = typeof(bool);
                    else type = GetType(info.className);
                    Type typeList = typeof(List<>).MakeGenericType(type);
                    var parseResult = Newtonsoft.Json.JsonConvert.DeserializeObject(text, typeList);
                    success = true;
                }
                catch (System.Exception e) { success = false; }
            }
            return success;
        }

        IEnumerator getListFiles(Action<List<string>> callback)
        {
            UnityWebRequest web = UnityWebRequest.Get(_jsonFolderUrl);
            yield return web.SendWebRequest();
            if (web.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GetFilesResponse>(web.downloadHandler.text);
                _downloadUrl = response.data.url;
                callback?.Invoke(response.data.files);
            }
            else
            {
                callback?.Invoke(null);
            }
        }

        void onJsonListCallback(List<string> fileList)
        {
            _jsonAvailable.ClearOptions();
            if (fileList != null && fileList.Count > 0)
            {
                _jsonAvailable.AddOptions(fileList);
            }
        }

        IEnumerator loadSelectedJson()
        {
            int index = _jsonAvailable.value;
            if (index < 0 || index >= _jsonAvailable.options.Count)
            {
                UnityEngine.Debug.LogError("Invalid option");
                yield break;
            }
            var fileName = _jsonAvailable.options[index].text;
            UnityWebRequest web = UnityWebRequest.Get(_downloadUrl + "/" +fileName);
            yield return web.SendWebRequest();
            if (web.result == UnityWebRequest.Result.Success)
            {
                _currentConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<FirebaseRemoteJson>(web.downloadHandler.text);
                _currentConfig.Init();
                UpdateUI();
            }
            else
            {
                UnityEngine.Debug.LogError("Download fail " + web.error);
            }
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName.Replace('.', '+'));
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public class ConditionToggleObj
        {
            public GameObject Obj;
            public Image Background;
            public Toggle Toggle;
            public Text Text;

            public ConditionToggleObj(Toggle toggle)
            {
                Obj = toggle.gameObject;
                this.Toggle = toggle;
                Background = toggle.GetComponent<Image>();
                Text = toggle.transform.GetComponentInChildren<Text>();
            }
        }

        [Serializable]
        public class GetFilesResponse
        {
            public string statusCode;
            public string message;
            public GetFilesDataResponse data;
        }

        [Serializable]
        public class GetFilesDataResponse
        {
            public List<string> files;
            public string url;
        }
#endif
    }
}