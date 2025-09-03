using System.Collections.Generic;
using Modio.Unity.UI.Editor.Common;
using Modio.Unity.UI.Scripts.Themes;
using UnityEditor;
using UnityEngine;
using ReorderableList = UnityEditorInternal.ReorderableList;

namespace Modio.Unity.UI.Editor.Components
{
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
