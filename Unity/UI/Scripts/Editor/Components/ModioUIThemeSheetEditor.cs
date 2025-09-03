using Modio.Unity.UI.Scripts.Themes;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Modio.Unity.UI.Editor.Components
{
    /// <summary>
    /// Editor that does very few changes; mostly just ensures we create new entries in _styles
    /// without any styleOptions (as they're serialized by reference and cause a mess when duplicated)
    /// </summary>
    [CustomEditor(typeof(ModioUIThemeSheet)), CanEditMultipleObjects,]
    public class ModioUIThemeSheetEditor : UnityEditor.Editor
    {
        ReorderableList _reorderableList;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_styles", "m_Script");
            
            SerializedProperty stylesProp = serializedObject.FindProperty("_styles");
            if (_reorderableList == null || _reorderableList.serializedProperty.serializedObject != stylesProp.serializedObject) _reorderableList = ConstructList(stylesProp);
            _reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        ReorderableList ConstructList(SerializedProperty stylesProp)
        {
            var modioStyleOptionsPropertyDrawer = new ModioStyleOptionsPropertyDrawer();

            return new ReorderableList(serializedObject, stylesProp)
            {
                elementHeightCallback = index =>
                {
                    SerializedProperty element = stylesProp.GetArrayElementAtIndex(index);

                    return modioStyleOptionsPropertyDrawer.GetPropertyHeight(element, GUIContent.none);
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty element = stylesProp.GetArrayElementAtIndex(index);
                    modioStyleOptionsPropertyDrawer.OnGUI(rect, element, new GUIContent(element.displayName, element.tooltip));
                },
                onAddCallback = list =>
                {
                    int oldCount = list.count;
                    ReorderableList.defaultBehaviours.DoAddButton(list);

                    //It's possible for the method above to cancel making a new element. Ensure that didn't happen
                    if (oldCount != list.count)
                    {
                        // StyleOptions is a SerializedReference array; make sure we don't copy values
                        // (as they'll change with the previous element and cause bad times)
                        SerializedProperty newProperty = list.serializedProperty.GetArrayElementAtIndex(list.count-1);
                        newProperty.FindPropertyRelative("_styleOptions").arraySize = 0;
                    }
                },
            };
        }
    }
}
