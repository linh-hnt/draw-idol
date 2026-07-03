using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OfflineEvent
{
    public enum eValueType
    {
        STRING,
        FLOAT,
        DOUBLE,
        DECIMAL,
        INT,
        LONG
    }

    public bool normal;
    public string eName;
    public string[] pName;
    public eValueType[] types;
    public string[] values;

    public Dictionary<string, object> GetParams()
    {
        var result = new Dictionary<string, object>();

        if (pName != null && pName.Length > 0)
        {
            int length = pName.Length;
            eValueType valueType = eValueType.STRING;
            bool parseResult = true;
            for (int i = 0; i < length; ++i)
            {
                parseResult = true;
                valueType = types[i];
                switch(valueType)
                {
                    case eValueType.FLOAT:
                        float floatCache = 0;
                        if (float.TryParse(values[i], out floatCache))
                        {
                            result[pName[i]] = floatCache;
                        }
                        else
                        {
                            parseResult = false;
                        }
                        break;
                    case eValueType.DOUBLE:
                        double doubleCache = 0;
                        if (double.TryParse(values[i], out doubleCache))
                        {
                            result[pName[i]] = doubleCache;
                        }
                        else
                        {
                            parseResult = false;
                        }
                        break;
                    case eValueType.DECIMAL:
                        decimal decimalCache = 0;
                        if (decimal.TryParse(values[i], out decimalCache))
                        {
                            result[pName[i]] = decimalCache;
                        }
                        else
                        {
                            parseResult = false;
                        }
                        break;
                    case eValueType.INT:
                        int intCache = 0;
                        if (int.TryParse(values[i], out intCache))
                        {
                            result[pName[i]] = intCache;
                        }
                        else
                        {
                            parseResult = false;
                        }
                        break;
                    case eValueType.LONG:
                        long longCache = 0;
                        if (long.TryParse(values[i], out longCache))
                        {
                            result[pName[i]] = longCache;
                        }
                        else
                        {
                            parseResult = false;
                        }
                        break;
                    default:
                        result[pName[i]] = values[i];
                        break;
                }

                if (!parseResult)
                {
                    result[pName[i]] = values[i];
                }
            }
        }

        return result;
    }

    public static OfflineEvent CreateEvent(bool isNormalEvent, string eventName, Dictionary<string, object> parameters)
    {
        var result = new OfflineEvent();
        result.normal = isNormalEvent;
        result.eName = eventName;
        if (parameters != null && parameters.Count > 0)
        {
            var leng = parameters.Count;
            result.pName = new string[leng];
            result.types = new eValueType[leng];
            result.values = new string[leng];
            int index = 0;
            foreach (var pair in parameters)
            {
                result.pName[index] = pair.Key;
                var pValue = pair.Value;
                result.values[index] = pValue.ToString();
                if (pValue is float)
                {
                    result.types[index] = eValueType.FLOAT;
                }
                else if (pValue is double)
                {
                    result.types[index] = eValueType.DOUBLE;
                }
                else if (pValue is decimal)
                {
                    result.types[index] = eValueType.DECIMAL;
                }
                else if (pValue is int)
                {
                    result.types[index] = eValueType.INT;
                }
                else if (pValue is long)
                {
                    result.types[index] = eValueType.LONG;
                }
                else
                {
                    result.types[index] = eValueType.STRING;
                }
                ++index;
            }
        }
        return result;
    }
}

[System.Serializable]
public class OfflineEventHandler
{
    const int MAX_EVENT = 50;
    public List<OfflineEvent> events = new List<OfflineEvent>();

    public void AddEvent(bool normal, string eName, Dictionary<string, object> dic)
    {
        if (events.Count >= MAX_EVENT)
        {
            events.RemoveAt(0);
        }
        events.Add(OfflineEvent.CreateEvent(normal, eName, dic));
    }
}

