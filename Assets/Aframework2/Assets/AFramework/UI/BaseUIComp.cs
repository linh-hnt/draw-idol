using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using AFramework.ExtensionMethods;
#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Linq;

#endif

namespace AFramework.UI
{
    public class BaseUIComp : MonoBehaviour
    {
#if UNITY_EDITOR
        // This method is called once when we add component do game object
        public virtual void AutoReference()
        {
            bool hasChange = false;
            // Magic of reflection
            // For each field in your class/component we are looking only for those that are empty/null
            foreach (var field in GetFieldInfosIncludingBaseClasses(this.GetType(),
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    )
            {
                if (field.IsStatic || field.IsNotSerialized) continue;

                if (field.FieldType.IsArray) //array type
                {
                    try
                    {
                        System.Array array;
                        if (field.FieldType.GetElementType() == typeof(GameObject))
                        {
                            GameObject[] holder = transform.FindDeepChildsWithStartName(field.Name);

                            array = System.Array.CreateInstance(typeof(GameObject), holder.Length);
                            for (int i = 0; i < holder.Length; i++)
                            {
                                array.SetValue(holder[i].gameObject, i);
                            }
                        }
                        else
                        {
                            var data = transform.GetComponentsInChildren(field.FieldType.GetElementType(), true)
                                .ToList();
                            for (int i = 0; i < data.Count; i++)
                            {
                                if (!data[i].name.StartsWith(field.Name))
                                {
                                    data.RemoveAt(i);
                                    --i;
                                }
                            }

                            array = System.Array.CreateInstance(field.FieldType.GetElementType(), data.Count);
                            for (int i = 0; i < data.Count; i++)
                            {
                                array.SetValue(data[i], i);
                            }
                        }

                        if (array.Length > 0)
                        {
                            field.SetValue(this, array);
                        }

                        hasChange = true;
                    }
                    catch (System.Exception e)
                    {
                    }

                    continue;
                }
                else if (field.FieldType.IsGenericType &&
                         field.FieldType.GetGenericTypeDefinition() == typeof(List<>)) //list type
                {
                    try
                    {
                        var listType = field.FieldType.GetGenericArguments()[0];
                        if (listType == typeof(GameObject))
                        {
                            GameObject[] holder = transform.FindDeepChildsWithStartName(field.Name);
                            field.SetValue(this, new List<GameObject>(holder));
                        }
                        else
                        {
                            System.Type genericListType =
                                typeof(List<>).MakeGenericType(field.FieldType.GetGenericArguments()[0]);
                            var list = (IList)System.Activator.CreateInstance(genericListType);
                            var data = transform.GetComponentsInChildren(listType, true);
                            for (int i = 0, length = data.Length; i < length; i++)
                            {
                                if (data[i].name.StartsWith(field.Name))
                                {
                                    list.Add(data[i]);
                                }
                            }

                            if (list.Count > 0)
                            {
                                field.SetValue(this, list);
                            }
                        }

                        hasChange = true;
                    }
                    catch (System.Exception e)
                    {
                    }

                    continue;
                }

                // Now we are looking for object (self or child) that have same name as a field
                Transform obj;
                /*if (transform.name == field.Name)
                {
                    obj = transform;
                }
                else*/
                {
                    obj = transform
                        .FindDeepChildLower(field
                            .Name); // Or you need to implement recursion to looking into deeper childs
                }

                // If we find object that have same name as field we are trying to get component that will be in type of a field and assign it
                if (obj != null)
                {
                    if (field.FieldType == typeof(GameObject))
                    {
                        field.SetValue(this, obj.gameObject);
                    }
                    else
                    {
                        field.SetValue(this, obj.GetComponent(field.FieldType));
                    }

                    hasChange = true;
                }
            }

            if (hasChange)
            {
                EditorUtility.SetDirty(this);
            }
        }
        
        public static FieldInfo[] GetFieldInfosIncludingBaseClasses(Type type, BindingFlags bindingFlags)
        {
            FieldInfo[] fieldInfos = type.GetFields(bindingFlags);

            // If this class doesn't have a base, don't waste any time
            if (type.BaseType == typeof(object))
            {
                return fieldInfos;
            }
            else
            {
                // Otherwise, collect all types up to the furthest base class
                var fieldInfoList = new List<FieldInfo>(fieldInfos);
                while (type.BaseType != typeof(object))
                {
                    type = type.BaseType;
                    fieldInfos = type.GetFields(bindingFlags);

                    // Look for fields we do not have listed yet and merge them into the main list
                    for (int index = 0; index < fieldInfos.Length; ++index)
                    {
                        bool found = false;

                        for (int searchIndex = 0; searchIndex < fieldInfoList.Count; ++searchIndex)
                        {
                            bool match =
                                (fieldInfoList[searchIndex].DeclaringType == fieldInfos[index].DeclaringType) &&
                                (fieldInfoList[searchIndex].Name == fieldInfos[index].Name);

                            if (match)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            fieldInfoList.Add(fieldInfos[index]);
                        }
                    }
                }

                return fieldInfoList.ToArray();
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(BaseUIComp), true)]
    public class BaseUICompEditor :
#if ODIN_INSPECTOR
        Sirenix.OdinInspector.Editor.OdinEditor
#else
Editor
#endif
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            OnCustomInspectorGUI();
        }

        protected virtual void OnCustomInspectorGUI()
        {
            if (GUILayout.Button("Auto Reference"))
            {
                foreach (BaseUIComp gameObject in targets)
                    gameObject.AutoReference();
            }
        }
    }
#endif
}