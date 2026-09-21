using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Editor.Common;
using Modio.Unity.UI.Scripts.Themes;
using Modio.Unity.UI.Scripts.Themes.Options;
using UnityEditor;
using UnityEngine;
using ReorderableList = UnityEditorInternal.ReorderableList;

namespace Modio.Unity.UI.Editor.Components
{

    [CustomPropertyDrawer(typeof(ModioUIThemeSheet.CommonThemeProperty<>))]
    public class  CommonThemePropertyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = position;
            float dropdownButtonWidth = 20;
            rect.width = EditorGUIUtility.labelWidth - dropdownButtonWidth;

            SerializedProperty nameProperty = property.FindPropertyRelative("Name");
            EditorGUI.PropertyField(rect, nameProperty, GUIContent.none);

            rect.xMin = rect.xMax;
            rect.xMax = rect.xMin + dropdownButtonWidth;

            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();

                string[] names = ModioUIThemeSheet.GetAllReferencedPropertyNames<Color>();
                
                foreach (string name in names) menu.AddItem(new GUIContent(name), false, OnSelected, name);

                var dropDownRect = rect;
                dropDownRect.xMin = position.xMin;
                menu.DropDown(dropDownRect);
            }
            
            rect.xMin = rect.xMax;
            rect.xMax = position.xMax;
            
            SerializedProperty resolvesAsProperty = property.FindPropertyRelative("ResolvesAs");
            EditorGUI.PropertyField(rect, resolvesAsProperty, GUIContent.none);
            return;

            void OnSelected(object userData)
            {
                var name = (string)userData;

                nameProperty.serializedObject.Update();
                
                nameProperty.stringValue = name;

                nameProperty.serializedObject.ApplyModifiedProperties();
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(CommonThemePropertyReference<>))]
    public class CommonThemePropertyReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            
            string[] names = ModioUIThemeSheet.GetAllReferencedPropertyNames<Color>();

            SerializedProperty nameProperty = property.FindPropertyRelative(nameof(CommonThemePropertyReference<Color>.Name));
            SerializedProperty colorProperty = property.FindPropertyRelative(nameof(CommonThemePropertyReference<Color>.ExplicitValue));
            int currentIndex = Array.IndexOf(names, nameProperty.stringValue);

            Rect rect = position;
            rect.xMax -= currentIndex == -1 ? rect.height * 5 : rect.height;

            var labels = new[] { new GUIContent("Explicit") }.Concat(names.Select(n => new GUIContent(n)));
            int newIndex = EditorGUI.Popup(rect, label, currentIndex + 1, labels.ToArray());

            rect.xMin = rect.xMax;
            rect.xMax = position.xMax;
            Color color = colorProperty.colorValue;

            bool enabled = GUI.enabled;

            if (!string.IsNullOrEmpty(nameProperty.stringValue))
            {
                GUI.enabled = false;
                color = ModioUIThemeSheet.ResolveReferencedProperty<Color>(nameProperty.stringValue);
            }

            if (EditorGUI.EndChangeCheck())
            {
                nameProperty.stringValue = newIndex > 0 ? names[newIndex-1] : "";
            }
            
            EditorGUI.BeginChangeCheck();
            
            color = EditorGUI.ColorField(rect, color);
            GUI.enabled = enabled;

            if (EditorGUI.EndChangeCheck())
            {
                colorProperty.colorValue = color;
            }
            
            EditorGUI.EndProperty();
        }
    }
    
    [CustomPropertyDrawer(typeof(Style))]
    public class ModioStyleOptionsPropertyDrawer : PropertyDrawer
    {
        Dictionary<string, ReorderableList> _styleOptionLists;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var list = EnsureStyleOptionLists(property);

            var target = property.FindPropertyRelative("_target");
            var extends = property.FindPropertyRelative("_extends");
            
            var rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width /= 2;
            rect.width -= 5;
            EditorGUIUtility.labelWidth = 50;

            var expandRect = rect;
            expandRect.width = EditorGUIUtility.labelWidth;
            
            property.isExpanded = EditorGUI.Foldout(expandRect, property.isExpanded, "", true);
            
            EditorGUI.PropertyField(rect, target);
            rect.x += position.width / 2 + 5;
            EditorGUI.PropertyField(rect, extends);
            EditorGUIUtility.labelWidth = 0;

            if (!property.isExpanded) return;

            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            list.DoList(position);
        }

        ReorderableList EnsureStyleOptionLists(SerializedProperty property)
        {
            _styleOptionLists ??= new Dictionary<string, ReorderableList>();

            var serializedProperty = property.FindPropertyRelative("_styleOptions");
            var path = serializedProperty.propertyPath;
            
            if (_styleOptionLists.TryGetValue(path, out ReorderableList yeet)) return yeet;
            
            _styleOptionLists[path] = ReorderableReferenceArray.New<IStyleOption>(serializedProperty);

            return _styleOptionLists[path];
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            
            var yeet = EnsureStyleOptionLists(property);
            
            return yeet.GetHeight() + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
