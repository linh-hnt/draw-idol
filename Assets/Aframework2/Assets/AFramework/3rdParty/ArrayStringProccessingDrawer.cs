using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework.ExtensionMethods;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace AFramework
{
    public class ArrayStringProccessingDrawer
    {
        public delegate string[] processingFunc(string[] input);

        SerializedProperty arrayProperty;
        SerializedProperty arraySizeProperty;

        List<string> myTempData = new List<string>();
        processingFunc getProcessing;
        processingFunc setProcessing;
        bool expanded = false;

        public ArrayStringProccessingDrawer(SerializedProperty property, processingFunc get, processingFunc set)
        {
            arrayProperty = property;
            getProcessing = get;
            setProcessing = set;
            expanded = arrayProperty.isExpanded;

            if (arrayProperty.isArray)
            {
                arraySizeProperty = arrayProperty.FindPropertyRelative("Array.size");
                var tempArr = new string[arraySizeProperty.intValue];

                for (var i = 0; i < arraySizeProperty.intValue; i++)
                {
                    tempArr[i] = arrayProperty.GetArrayElementAtIndex(i).stringValue;
                    //myTempData.Add((T)GetTargetObjectOfProperty(arrayProperty.GetArrayElementAtIndex(i)));
                }

                tempArr = getProcessing(tempArr);
                myTempData.AddRange(tempArr);
            }
        }

        public bool Draw()
        {
            bool changedExpand = false;
            bool changeData = false;

            GUILayout.BeginHorizontal();
            // Create a foldout
            expanded = UnityEditor.EditorGUILayout.Foldout(expanded, arrayProperty.displayName);
            if (expanded != arrayProperty.isExpanded)
            {
                arrayProperty.isExpanded = expanded;
                changedExpand = true;
            }

            int currentSize = myTempData.Count;
            var newSize = UnityEditor.EditorGUILayout.IntField(currentSize, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            // Resize if user input a new array length
            if (currentSize != newSize)
            {
                myTempData.Resize(newSize);
                changeData = true;
            }

            // Show values if foldout was opened.
            if (expanded)
            {
                // Creates a spacing between the input for array-size, and the array values.
                UnityEditor.EditorGUILayout.Space();
                for (var i = 0; i < newSize; ++i)
                {
                    var newValue = UnityEditor.EditorGUILayout.TextField(myTempData[i]);
                    if (newValue != myTempData[i])
                    {
                        myTempData[i] = newValue;
                        changeData = true;
                    }
                }
            }

            if (changeData)
            {
                var newArray = setProcessing(myTempData.ToArray());
                arraySizeProperty.arraySize = newSize;
                for (var i = 0; i < arraySizeProperty.intValue; i++)
                {
                    arrayProperty.GetArrayElementAtIndex(i).stringValue = newArray[i];
                }
            }

            return changedExpand || changeData;
        }

        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
    }
}
#endif