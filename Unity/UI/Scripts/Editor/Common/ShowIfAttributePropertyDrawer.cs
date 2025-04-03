using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Modio.Unity.UI.Editor.Common
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfAttributePropertyDrawer : PropertyDrawer
    {
        static readonly Regex ArrayRegex = new Regex(@"(?<property>\w+)\.Array\.data\[(?<index>\d+)\]");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsShow(property)) EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => IsShow(property) ? EditorGUI.GetPropertyHeight(property, label) : -EditorGUIUtility.standardVerticalSpacing;

        bool IsShow(SerializedProperty property)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            MethodInfo predicate = fieldInfo.DeclaringType!.GetMethod(showIf.PredicateName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            object obj;
            var arrayMatch = ArrayRegex.Match(property.propertyPath);

            if (arrayMatch.Success)
            {
                FieldInfo arrayField = property.serializedObject.targetObject.GetType().GetField(arrayMatch.Groups["property"].Value, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
                var array = (Array)arrayField.GetValue(property.serializedObject.targetObject);

                obj = array.GetValue(int.Parse(arrayMatch.Groups["index"].Value));
            }
            else
                obj = property.serializedObject.targetObject;

            if (predicate != null) return (bool)predicate.Invoke(obj, null);

            Debug.LogError($@"No method named ""{showIf.PredicateName}"" exists in {fieldInfo.DeclaringType!.Name}");

            return false;
        }
    }
}
