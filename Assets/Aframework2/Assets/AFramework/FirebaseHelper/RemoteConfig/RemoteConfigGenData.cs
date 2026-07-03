using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework
{
    [CreateAssetMenu(menuName = "AFramework/RemoteConfigGenData")]
    public partial class RemoteConfigGenData : ScriptableObject
    {
        [SerializeField] string _saveClassName = "SaveGameData";
        [SerializeField] string _saveVarName = "_saveData";
        [SerializeField] public RemoteConfigInfo[] ConfigInfos;

        public enum ConfigDataType
        {
            INT,
            FLOAT,
            STRING,
            BOOL,
            CLASS,
            ENUM,
            LIST_INT,
            LIST_FLOAT,
            LIST_STRING,
            LIST_BOOL,
            LIST_CLASS,
            LIST_ENUM,
        }

        [Serializable]
        public class RemoteConfigInfo
        {
            [Serializable]
            public class DefaultData
            {
                public string android;
                public string ios;

                public string current
                {
                    get
                    {
#if UNITY_IOS
                        return ios;        
#else
                        return android;
#endif
                    }
                }
            }

            public string key;
            public ConfigDataType type;
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.ShowIf("IsClass")]
#endif
            public string className;
            public bool customUpdate = false;
            public DefaultData defaultData;

            public string CustomUpdateFunc
            {
                get { return "Update_" + key; }
            }
#if UNITY_EDITOR
            public bool IsClass()
            {
                return type == ConfigDataType.CLASS ||
                       type == ConfigDataType.LIST_CLASS ||
                       type == ConfigDataType.ENUM ||
                       type == ConfigDataType.LIST_ENUM;
            }

            public bool IsNullConvertable()
            {
                return type == ConfigDataType.INT
                    || type == ConfigDataType.FLOAT
                    || type == ConfigDataType.ENUM
                    || type == ConfigDataType.BOOL;
            }

            public bool IsSingle()
            {
                return type == ConfigDataType.INT ||
                       type == ConfigDataType.FLOAT ||
                       type == ConfigDataType.STRING ||
                       type == ConfigDataType.ENUM ||
                       type == ConfigDataType.BOOL ||
                       type == ConfigDataType.CLASS;
            }

            public bool CanConvetJson()
            {
                return type == ConfigDataType.CLASS ||
                       type == ConfigDataType.LIST_INT ||
                       type == ConfigDataType.LIST_FLOAT ||
                       type == ConfigDataType.LIST_STRING ||
                       type == ConfigDataType.LIST_BOOL ||
                       type == ConfigDataType.LIST_CLASS ||
                       type == ConfigDataType.LIST_ENUM;
            }

            public string KeyDefault
            {
                get { return key + "_df"; }
            }
            
            public string ConfigKey
            {
                get { return "config_" + key; }
            }

            public string FirebaseValueFunc
            {
                get {
                    switch (type)
                    {
                        case ConfigDataType.INT: return "GetLongValue()";
                        case ConfigDataType.FLOAT: return "GetDoubleValue()";
                        case ConfigDataType.BOOL: return "GetBooleanValue()";
                    }
                    return "GetStringValue()";
                }
            }

            public string FirebaseParseValue
            {
                get { 
                    switch (type)
                    {
                        case ConfigDataType.INT:
                        case ConfigDataType.FLOAT:
                        case ConfigDataType.BOOL:
                            return $"({TypeString}){ConfigKey}.{FirebaseValueFunc}";
                        case ConfigDataType.STRING:
                            return $"{ConfigKey}.{FirebaseValueFunc}";
                        case ConfigDataType.CLASS:
                            return $"Newtonsoft.Json.JsonConvert.DeserializeObject<{className}>({ConfigKey}.{FirebaseValueFunc})";
                        case ConfigDataType.ENUM:
                            return $"System.Enum.Parse<{className}>({ConfigKey}.{FirebaseValueFunc})";
                        case ConfigDataType.LIST_INT:
                        case ConfigDataType.LIST_FLOAT:
                        case ConfigDataType.LIST_BOOL:
                        case ConfigDataType.LIST_STRING:
                        case ConfigDataType.LIST_CLASS:
                        case ConfigDataType.LIST_ENUM:
                            return $"Newtonsoft.Json.JsonConvert.DeserializeObject<{TypeString}>({ConfigKey}.{FirebaseValueFunc})";
                        default:
                            Debug.LogError("TODO");
                            break;

                    }
                    return "null";
                }
            }

            public string ParseValue(string variableName)
            {
                switch (type)
                {
                    case ConfigDataType.INT: return $"int.Parse({variableName})";
                    case ConfigDataType.FLOAT: return $"float.Parse({variableName}, System.Globalization.CultureInfo.InvariantCulture)";
                    case ConfigDataType.STRING: return $"{variableName}";
                    case ConfigDataType.BOOL: return $"{variableName}.ToLower() is \"true\" or \"1\" or \"on\" ? true : false";

                    case ConfigDataType.ENUM: return $"System.Enum.Parse<{className}>({variableName})";
                    default:
                        return $"Newtonsoft.Json.JsonConvert.DeserializeObject<{TypeString}>({variableName})";
                }
                return "null";
            }

            public string ByteCrewParseValue
            {
                get
                {
                    return ParseValue(ConfigKey);
                }
            }

            public string SaveKey
            {
                get { return "current_" + key; }
            }

            public string UseRemoteSaveKey
            {
                get { return "useremote_" + key; }
            }

            public string TypeString
            { 
                get
                {
                    if (customUpdate) return "string";
                    switch (type)
                    {
                        case ConfigDataType.INT:
                            return "int";
                        case ConfigDataType.FLOAT:
                            return "float";
                        case ConfigDataType.STRING:
                            return "string";
                        case ConfigDataType.BOOL:
                            return "bool";
                        case ConfigDataType.CLASS:
                            return className;
                        case ConfigDataType.ENUM:
                            return className;

                        case ConfigDataType.LIST_INT:
                            return "List<int>";
                        case ConfigDataType.LIST_FLOAT:
                            return "List<float>";
                        case ConfigDataType.LIST_STRING:
                            return "List<string>";
                        case ConfigDataType.LIST_BOOL:
                            return "List<bool>";
                        case ConfigDataType.LIST_CLASS:
                            return $"List<{className}>";
                        case ConfigDataType.LIST_ENUM:
                            return $"List<{className}>";
                    }

                    return "string";
                }
            }

            public string TypeInput
            {
                get
                {
                    switch (type)
                    {
                        case ConfigDataType.INT: return "int";
                        case ConfigDataType.FLOAT: return "float";
                        case ConfigDataType.BOOL: return "bool";
                    }
                    return "string";
                }
            }
#endif
        }

        public RemoteConfigInfo GetConfig(string name)
        {
            for (int i = 0, count = ConfigInfos.Length; i < count; ++i)
            {
                var config = ConfigInfos[i];
                if (config.key == name)
                {
                    return config;
                }
            }
            return null;
        }
    }
}