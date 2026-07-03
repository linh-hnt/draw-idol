using UnityEngine;

#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

// Defines an attribute that makes the array use enum values as labels.
// Use like this:
//      [NamedArray(typeof(eDirection))] public GameObject[] m_Directions;

public class NamedArrayAttribute : PropertyAttribute
{
#if UNITY_EDITOR
    public readonly string[] names;

    public NamedArrayAttribute(string[] names)
    { 
        this.names = names;
    }


    public NamedArrayAttribute (Type enumType)
    {
        this.names = System.Enum.GetNames(enumType);
    }
#else
    public NamedArrayAttribute (object enumType)
    {

    }
#endif
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(NamedArrayAttribute))]
public class NamedArrayDrawer : PropertyDrawer
{
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        try
        {
            int pos = int.Parse(property.propertyPath.Split('[', ']')[1]);
            EditorGUI.PropertyField(rect, property, new GUIContent(((NamedArrayAttribute)attribute).names[pos]));
        }
        catch
        {
            EditorGUI.PropertyField(rect, property, label);
        }
    }
}
#endif