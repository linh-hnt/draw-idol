using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework
{
    [System.Serializable]
    public class PlatformString
    {
        [SerializeField]
        string win32;
        [SerializeField]
        string android;
        [SerializeField]
        string ios;

        public string getString()
        {
#if UNITY_ANDROID
            return android;
#elif UNITY_IOS
            return ios;
#else
            return win32;
#endif
        }

        public PlatformString(PlatformString from)
        {
            win32 = from.win32;
            android = from.android;
            ios = from.ios;
        }
        
        public PlatformString() {}
    }
}