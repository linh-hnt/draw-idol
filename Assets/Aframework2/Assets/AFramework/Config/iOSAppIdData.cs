using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class iOSAppIdData
{
    public string packageId;
    public string appId;

    public iOSAppIdData() { packageId = ""; appId = string.Empty; }
    public iOSAppIdData(string _packageId, string _appId)
    {
        packageId = _packageId;
        appId = _appId;
    }
}
