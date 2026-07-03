using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework
{
#if UNITY_EDITOR || LOCAL_FIREBASEREMOTECONFIG
    using Newtonsoft.Json;
    
    [System.Serializable]
    public class FirebaseRemoteJson
    {
        [System.Serializable]
        public class FirebaseCondition
        {
            public string name;
            public string expression;
            [System.NonSerialized, JsonIgnore] public string expressionParsed;
            public string tagColor;

            [JsonIgnore] public Color color { get {
                    var name = string.IsNullOrEmpty(tagColor) ? string.Empty : tagColor.ToLowerInvariant();
                    Color result = Color.green;
                    if (name == "orange")
                    {
                        ColorUtility.TryParseHtmlString("#463A29", out result);
                        return result;
                    }
                    if (name == "deep_orange")
                    {
                        ColorUtility.TryParseHtmlString("#442C30", out result);
                        return result;
                    }
                    if (name == "pink")
                    {
                        ColorUtility.TryParseHtmlString("#3E1F40", out result);
                        return result;
                    }
                    if (name == "purple")
                    {
                        ColorUtility.TryParseHtmlString("#2C2252", out result);
                        return result;
                    }
                    if (name == "indigo")
                    {
                        ColorUtility.TryParseHtmlString("#182A52", out result);
                        return result;
                    }
                    if (name == "blue")
                    {
                        ColorUtility.TryParseHtmlString("#113A60", out result);
                        return result;
                    }
                    if (name == "cyan")
                    {
                        ColorUtility.TryParseHtmlString("#0A4257", out result);
                        return result;
                    }
                    if (name == "teal")
                    {
                        ColorUtility.TryParseHtmlString("#0A3A46", out result);
                        return result;
                    }
                    if (name == "green")
                    {
                        ColorUtility.TryParseHtmlString("#1A3F3A", out result);
                        return result;
                    }
                    if (name == "lime")
                    {
                        ColorUtility.TryParseHtmlString("#284439", out result);
                        return result;
                    }
                    if (name == "brown")
                    {
                        ColorUtility.TryParseHtmlString("#242B39", out result);
                        return result;
                    }
                    return result;
                } }
        }

        [System.Serializable]
        public class FirebaseParameter
        {
            public FirebaseParamValue defaultValue;
            public FirebaseParamValue forceValue;
            public System.Collections.Specialized.OrderedDictionary conditionalValues;
            [System.NonSerialized, JsonIgnore] public System.Collections.Specialized.OrderedDictionary conditionalValuesParsed;

            [System.Serializable]
            public class FirebaseParamValue
            {
                public string value;
            }

            public string GetValue(List<string> condition)
            {
                if (forceValue != null) return forceValue.value;
                if (condition != null && condition.Count > 0 && conditionalValuesParsed != null)
                {
                    foreach (string key in conditionalValuesParsed.Keys)
                    {
                        if (condition.Contains(key) && conditionalValuesParsed[key] != null)
                        {
                            return (conditionalValuesParsed[key] as FirebaseParamValue).value;
                        }
                    }
                }
                return defaultValue != null ? defaultValue.value : null;
            }
        }

        [System.Serializable]
        public class ParameterGroup
        {
            public Dictionary<string, FirebaseParameter> parameters;
        }

        public FirebaseCondition[] conditions;
        [JsonProperty] Dictionary<string, FirebaseParameter> parameters;
        [JsonProperty] Dictionary<string, ParameterGroup> parameterGroups;
        
        public List<string> selectedCondition;

        public int ConditionCount { get { return conditions != null ? conditions.Length : 0; } }
        public int ParameterCount { get { return parameters != null ? parameters.Count : 0; } }

        [JsonIgnore] Dictionary<string, FirebaseParameter> _allParameter;
        [JsonIgnore] public Dictionary<string, FirebaseParameter> allParameter
        {
            get
            {
                if (_allParameter == null)
                {
                    _allParameter = new Dictionary<string, FirebaseParameter>();
                    if (parameters != null)
                    {
                        foreach (var pair in parameters)
                        {
                            _allParameter.Add(pair.Key, pair.Value);
                        }
                    }

                    if (parameterGroups != null)
                    {
                        foreach (var group in parameterGroups)
                        {
                            if (group.Value == null || group.Value.parameters == null || group.Value.parameters.Count == 0) continue;
                            foreach (var pair in group.Value.parameters)
                            {
                                _allParameter.Add(pair.Key, pair.Value);
                            }
                        }
                    }
                }
                return _allParameter;
            }
        }

        public void Init()
        {
            foreach (var cond in conditions)
            {
                cond.expressionParsed = cond.expression;
                var index = cond.expressionParsed.IndexOf("app.id");
                if (index >= 0)
                {
                    var subIndex = cond.expressionParsed.IndexOf('\'', index + 1);
                    subIndex = cond.expressionParsed.IndexOf('\'', subIndex + 1);
                    var subStr = cond.expressionParsed.Substring(index, subIndex - index);
                    if (subStr.IndexOf("ios") >= 0) subStr = "IOS";
                    else subStr = "AND";
                    cond.expressionParsed = cond.expressionParsed.Remove(0, subIndex - index + 1);
                    cond.expressionParsed = cond.expressionParsed.Insert(0, subStr);
                }
                cond.expressionParsed = cond.expressionParsed.Replace("app.", "");
                cond.expressionParsed = cond.expressionParsed.Replace("device.", "");
                cond.expressionParsed = cond.expressionParsed.Replace("device.", "");
            }

            List<object> keyList = new();
            foreach (var param in allParameter.Values)
            {
                if (param.conditionalValues == null || param.conditionalValues.Count == 0) continue;
                keyList.Clear();
                foreach (var key in param.conditionalValues.Keys)
                {
                    keyList.Add(key);
                }
                param.conditionalValuesParsed = new();
                for (int i = 0; i < keyList.Count; ++i)
                {
                    var key = keyList[i];
                    var valObj = Newtonsoft.Json.JsonConvert.DeserializeObject<FirebaseParameter.FirebaseParamValue>(param.conditionalValues[key].ToString());
                    param.conditionalValuesParsed.Add(key, valObj);
                }
            }
        }

        public FirebaseParameter AddParam(string key)
        {
            if (!allParameter.ContainsKey(key))
            {
                var newParam = new FirebaseParameter();
                parameters.Add(key, newParam);
                allParameter.Add(key, newParam);
            }
            return allParameter[key];
        }

        public FirebaseParameter GetParam(string name)
        {
            foreach (var pair in allParameter)
            {
                if (pair.Key == name) return pair.Value;
            }
            return null;
        }

        public string GetValue(string key)
        {
            var param = GetParam(key);
            if (param == null) return null;
            return param.GetValue(selectedCondition);
        }

        public bool ConditionStatus(string name)
        {
            if (selectedCondition == null || selectedCondition.Count == 0) return false;
            return selectedCondition.Contains(name);
        }
    }

    public class  FirebaseLocalConfigDataHelper : IRemoteConfigData
    {
        string _rawData;
        public FirebaseLocalConfigDataHelper(string rawData)
        {
            _rawData = rawData;
        }

        public bool GetBooleanValue()
        {
            if (string.IsNullOrEmpty(_rawData)) return false;
            if (_rawData.ToLowerInvariant() is "on" or "true" or "1") return true;
            return false;
        }

        public IEnumerable<byte> GetByteArrayValue()
        {
            if (string.IsNullOrEmpty(_rawData))
            {
                return null;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<byte>>(_rawData);
        }

        public double GetDoubleValue()
        {
            double result = 0;
            if (double.TryParse(_rawData, out result))
            {
                return result;
            }
            return 0;
        }

        public long GetLongValue()
        {
            long result = 0;
            if (long.TryParse(_rawData, out result))
            {
                return result;
            }
            return 0;
        }

        public string GetStringValue()
        {
            return _rawData;
        }
    }
#endif
}