using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AFramework
{
    public class LocalFirebaseRemoteConfigParamUI : MonoBehaviour
    {
        public Text ParamName;
        public Toggle ForceToggle;
        public InputField ForceInput;
        public Button ForceVerifyBtn;
        public Text ParamValueText;
        public Button ShowContentBtn;

#if UNITY_EDITOR || LOCAL_FIREBASEREMOTECONFIG
        
#endif
    }
}