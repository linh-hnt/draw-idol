using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AFramework.ExtensionMethods
{
    public static class TranformExtensions
    {
        public static T[] GetComponentsInChildrenFD<T>(this Transform trans)
        {
            List<T> list = new List<T>();
            T component;
            for (int i = 0; i < trans.childCount; i++)
            {
                component = trans.GetChild(i).GetComponent<T>();
                if (component != null)
                {
                    list.Add(component);
                }
            }

            return list.ToArray();
        }

        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            aName = aName.ToLower();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower() == aName)
                    return child;
                var result = child.FindDeepChild(aName);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static Transform FindDeepChildLower(this Transform aParent, string aName)
        {
            aName = aName.ToLower();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower() == aName)
                    return child;
                var result = child.FindDeepChildLower(aName);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static GameObject[] FindDeepChildsWithStartName(this Transform aParent, string startName)
        {
            startName = startName.ToLower();
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower().StartsWith(startName))
                {
                    result.Add(child.gameObject);
                }
                else
                {
                    var childResult = child.FindDeepChildsWithStartName(startName);
                    result.AddRange(childResult);
                }
            }

            return result.ToArray();
            ;
        }

        public static Transform FindDeepChildWithStartName(this Transform aParent, string startName)
        {
            startName = startName.ToLower();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower().StartsWith(startName))
                    return child;
                var result = child.FindDeepChildWithStartName(startName);
                if (result != null)
                    return result;
            }

            return null;
        } 
        public static Transform FindDeepChildWithContainName(this Transform aParent, string startName)
        {
            startName = startName.ToLower();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower().Contains(startName))
                    return child;
                var result = child.FindDeepChildWithContainName(startName);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static GameObject[] FindChildsSameDeep(this Transform trans, string startName, bool includeInactive)
        {
            Transform result = FindDeepChildWithStartName(trans, startName);
            List<GameObject> list = new List<GameObject>();
            if (result != null)
            {
                Transform aParent = result.parent;
                Transform obj;
                for (int i = 0; i < aParent.childCount; i++)
                {
                    obj = aParent.GetChild(i);
                    if ((!includeInactive || obj.gameObject.activeSelf) && obj.name.StartsWith(startName))
                    {
                        list.Add(obj.gameObject);
                    }
                }
            }

            return list.ToArray();
        }

        public static void SetActiveChilds(this Transform trans, bool value)
        {
            for (int i = 0, length = trans.childCount; i < length; ++i)
            {
                trans.GetChild(i).gameObject.SetActive(value);
            }
        }

        public static int GetChildCount(this Transform trans, bool includeInactive)
        {
            if (includeInactive)
            {
                return trans.childCount;
            }
            else
            {
                int count = 0;
                for (int i = 0; i < trans.childCount; ++i)
                {
                    if (trans.GetChild(i).gameObject.activeSelf)
                    {
                        ++count;
                    }
                }

                return count;
            }
        }
        
        public static void DeleteAllChilds(this Transform t)
        {
            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
                Object.DestroyImmediate(t.GetChild(0).gameObject);
        }
        
        public static void SetSizeByWidth(this RectTransform img, float width, float aspect)
        {
            img.sizeDelta = new Vector2(width, width * aspect);
        }

        public static void SetSizeByHeight(this RectTransform img, float height, float aspect)
        {
            img.sizeDelta = new Vector2(height / aspect, height);
        }

        public static bool IsVisible(this RectTransform target, RectTransform[] masks = null)
        {
            if (!target.gameObject.activeInHierarchy) return false;
            List<RectTransform> checkList = new List<RectTransform>();
            checkList.Add(AFramework.UI.CanvasManager.UIRectTrans);
            if (masks != null) checkList.AddRange(masks);

            Vector3[] aux = new Vector3[4]; //Cache
            //Get worldSpace corners and  then creates a Rect considering the first and the third values are diagonal
            target.GetWorldCorners(aux);
            Rect targetRect = new Rect(aux[0], (aux[2] - aux[0]));

            foreach (var check in checkList)
            {
                check.GetWorldCorners(aux);
                Rect checkRect = new Rect(aux[0], (aux[2] - aux[0]));
                if (!checkRect.Overlaps(targetRect))
                {
                    return false;
                }
            }
            return true;
        }
    }
}