using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class GameObjectExtensions
    {
        public static void SetLayerRecursively(this GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            for (int i = 0, length = obj.transform.childCount; i < length; ++i)
            {
                obj.transform.GetChild(i).gameObject.SetLayerRecursively(newLayer);
            }
        }

        public static void SetActiveChilds(this GameObject obj, bool value)
        {
            for (int i = 0, length = obj.transform.childCount; i < length; ++i)
            {
                obj.transform.GetChild(i).gameObject.SetActive(value);
            }
        }
    }
}