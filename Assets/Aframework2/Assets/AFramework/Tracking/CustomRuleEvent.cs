using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomRuleEvent
{
    public enum eCompareType
    {
        None,
        Equal,
        More,
        Less,
        Equal_More,
        Equal_Less,
        Inside,
        Outside,
    }

    [SerializeField] public string eventName;
    [SerializeField] string paramName;
    [SerializeField] string paramValue;
    [SerializeField] string comparisonMethod;
    [SerializeField] string appsflyerEventName;

    delegate string process_event_method(Dictionary<string, object> parameters);

    eCompareType compareType = eCompareType.None;
    int[] intValueList;
    string[] stringValueList;
    process_event_method callbackMethod;

    public bool Build()
    {
        if (string.IsNullOrEmpty(eventName)) return false;
        if (string.IsNullOrEmpty(appsflyerEventName)) return false;
        if (string.IsNullOrEmpty(paramName))
        {
            callbackMethod = ProcessEventNameOnly;
            return true;
        }
        if (string.IsNullOrEmpty(paramValue))
        {
            callbackMethod = ProcessEventNameAndParamName;
            return true;
        }
        if (string.IsNullOrEmpty(comparisonMethod))
        {
            //paramValue valid but comparisonMethod not valid
            return false;
        }

        switch (comparisonMethod)
        {
            case "=":
                compareType = eCompareType.Equal;
                break;
            case ">":
                compareType = eCompareType.More;
                break;
            case "<":
                compareType = eCompareType.Less;
                break;
            case ">=":
                compareType = eCompareType.Equal_More;
                break;
            case "<=":
                compareType = eCompareType.Equal_Less;
                break;
            case "><":
                compareType = eCompareType.Inside;
                break;
            case "<>":
                compareType = eCompareType.Outside;
                break;
            default://not support comparison type
                return false;
        }

        stringValueList = paramValue.Split(';');
        if (stringValueList == null || stringValueList.Length == 0) return false;

        if (compareType != eCompareType.Equal)//should be compare number
        {
            intValueList = new int[stringValueList.Length];
            for (int i = 0; i < stringValueList.Length; ++i)
            {
                int parsedValue;
                if (int.TryParse(stringValueList[i], out parsedValue))
                {
                    intValueList[i] = parsedValue;
                }
                else
                {
                    return false;//invalid number
                }
            }

            if (compareType == eCompareType.Inside || compareType == eCompareType.Outside)
            {
                if (intValueList.Length != 2)
                {
                    return false;//invalid data for Inside and Outside check method
                }
                if (intValueList[0] > intValueList[1])
                {
                    var temp = intValueList[0];
                    intValueList[0] = intValueList[1];
                    intValueList[1] = temp;
                }
            }
        }
        callbackMethod = ProcessEventNameAndParamNameAndValue;

        return true;
    }

    public string ProcessEvent(Dictionary<string, object> parameters)
    {
        return callbackMethod(parameters);
    }

    string ProcessEventNameOnly(Dictionary<string, object> parameters)
    {
        return appsflyerEventName;
    }

    string ProcessEventNameAndParamName(Dictionary<string, object> parameters)
    {
        if (parameters == null) return null;
        if (parameters.ContainsKey(paramName)) return appsflyerEventName;
        return null;
    }

    string ProcessEventNameAndParamNameAndValue(Dictionary<string, object> parameters)
    {
        if (parameters == null) return null;
        if (!parameters.ContainsKey(paramName)) return null;

        switch (compareType)
        {
            case eCompareType.Equal:
                {
                    string targetValue = parameters[paramName].ToString();
                    for (int i = 0; i < stringValueList.Length; ++i)
                    {
                        if (stringValueList[i] == targetValue) return appsflyerEventName;
                    }
                }
                break;
            case eCompareType.More:
                {
                    if (intValueList == null || intValueList.Length == 0) return null;
                    int targetValue;
                    if (!int.TryParse(parameters[paramName].ToString(), out targetValue))
                    {
                        return null;
                    }
                    for (int i = 0; i < intValueList.Length; ++i)
                    {
                        if (targetValue > intValueList[i])
                        {
                            return appsflyerEventName;
                        }
                    }
                }
                break;
            case eCompareType.Less:
                {
                    if (intValueList == null || intValueList.Length == 0) return null;
                    int targetValue;
                    if (!int.TryParse(parameters[paramName].ToString(), out targetValue))
                    {
                        return null;
                    }
                    for (int i = 0; i < intValueList.Length; ++i)
                    {
                        if (targetValue < intValueList[i])
                        {
                            return appsflyerEventName;
                        }
                    }
                }
                break;
            case eCompareType.Equal_More:
                {
                    if (intValueList == null || intValueList.Length == 0) return null;
                    int targetValue;
                    if (!int.TryParse(parameters[paramName].ToString(), out targetValue))
                    {
                        return null;
                    }
                    for (int i = 0; i < intValueList.Length; ++i)
                    {
                        if (targetValue >= intValueList[i])
                        {
                            return appsflyerEventName;
                        }
                    }
                }
                break;
            case eCompareType.Equal_Less:
                {
                    if (intValueList == null || intValueList.Length == 0) return null;
                    int targetValue;
                    if (!int.TryParse(parameters[paramName].ToString(), out targetValue))
                    {
                        return null;
                    }
                    for (int i = 0; i < intValueList.Length; ++i)
                    {
                        if (targetValue <= intValueList[i])
                        {
                            return appsflyerEventName;
                        }
                    }
                }
                break;
            case eCompareType.Inside:
                {
                    if (intValueList == null || intValueList.Length != 2) return null;
                    int targetValue;
                    if (!int.TryParse(parameters[paramName].ToString(), out targetValue))
                    {
                        return null;
                    }
                    if (targetValue > intValueList[0] && targetValue < intValueList[1])
                    {
                        return appsflyerEventName;
                    }
                }
                break;
            case eCompareType.Outside:
                {
                    if (intValueList == null || intValueList.Length != 2) return null;
                    int targetValue;
                    if (!int.TryParse(parameters[paramName].ToString(), out targetValue))
                    {
                        return null;
                    }
                    if (targetValue < intValueList[0] || targetValue > intValueList[1])
                    {
                        return appsflyerEventName;
                    }
                }
                break;
            default:
                return null;
        }
        return null;
    }
}
