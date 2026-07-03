using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AFramework.ExtensionMethods;
using UnityEditor;
using UnityEngine;

public class MonoBehaviourHelper : MonoBehaviour
{
    [MenuItem("CONTEXT/MonoBehaviour/AutoRef")]
    static void AutoRef(MenuCommand menuCommand)
    {
        var obj = (menuCommand.context);
        var mono = obj.GetType();
        AutoReference(obj);
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

    public static void AutoReference<T>(T target)
    {
        bool hasChange = false;
        // Magic of reflection
        // For each field in your class/component we are looking only for those that are empty/null
        Transform t = (target as MonoBehaviour).transform;
        foreach (var field in GetFieldInfosIncludingBaseClasses(target.GetType(),
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                )
        {
            string searchName = field.Name.Replace("m_", string.Empty);

            if (field.IsStatic || field.IsNotSerialized) continue;

            if (field.FieldType.IsArray) //array type
            {
                try
                {
                    System.Array array;
                    if (field.FieldType.GetElementType() == typeof(GameObject))
                    {
                        GameObject[] holder = t.transform.FindDeepChildsWithStartName(searchName);

                        array = System.Array.CreateInstance(typeof(GameObject), holder.Length);
                        for (int i = 0; i < holder.Length; i++)
                        {
                            array.SetValue(holder[i].gameObject, i);
                        }
                    }
                    else
                    {
                        var data = t.transform.GetComponentsInChildren(field.FieldType.GetElementType(), true)
                            .ToList();
                        for (int i = 0; i < data.Count; i++)
                        {
                            if (!data[i].name.StartsWith(searchName))
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
                        field.SetValue(target, array);
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
                        GameObject[] holder = t.transform.FindDeepChildsWithStartName(field.Name);
                        field.SetValue(target, new List<GameObject>(holder));
                    }
                    else
                    {
                        System.Type genericListType =
                            typeof(List<>).MakeGenericType(field.FieldType.GetGenericArguments()[0]);
                        var list = (IList)System.Activator.CreateInstance(genericListType);
                        var data = t.transform.GetComponentsInChildren(listType, true);
                        for (int i = 0, length = data.Length; i < length; i++)
                        {
                            if (data[i].name.StartsWith(searchName))
                            {
                                list.Add(data[i]);
                            }
                        }

                        if (list.Count > 0)
                        {
                            field.SetValue(target, list);
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
                obj = t.transform
                    .FindDeepChildLower(searchName); // Or you need to implement recursion to looking into deeper childs
            }

            // If we find object that have same name as field we are trying to get component that will be in type of a field and assign it
            if (obj != null)
            {
                if (field.FieldType == typeof(GameObject))
                {
                    field.SetValue(target, obj.gameObject);
                }
                else
                {
                    field.SetValue(target, obj.GetComponent(field.FieldType));
                }

                hasChange = true;
            }
        }

        if (hasChange)
        {
            EditorUtility.SetDirty(t.gameObject);
        }
    }
}