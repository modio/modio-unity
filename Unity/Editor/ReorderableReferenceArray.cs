using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Modio.Editor.Common
{
    /// <summary>
    /// COPIED FROM ModioUI.Editor.Common.ReorderableReferenceArray
    /// TODO: Unify them
    /// </summary>
    internal static class ReorderableReferenceArray
    {
        static readonly float Padding = EditorGUIUtility.singleLineHeight * 0.5f;

        /// <param name="removeClassNamePrefix">Removes the most common prefix among all implementations of <see cref="T"/>, wherever their names are displayed.</param>
        /// <param name="nameHintPropertyName">The name of a <see cref="GameObject"/> or <see cref="Component"/> property. The assigned <see cref="GameObject"/>'s name will be appended to each item's title in the list.</param>
        public static ReorderableList New<T>(SerializedProperty serializedProperty, bool removeClassNamePrefix = true, string nameHintPropertyName = null)
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes().Where(type => !type.IsAbstract && typeof(T).IsAssignableFrom(type))).OrderBy(type => type.Name).ToArray();
            int prefixLength = removeClassNamePrefix ? GetPrefixLength(types) : 0;
            
            return new ReorderableList(serializedProperty.serializedObject, serializedProperty, true, false, true, true)
            {
                elementHeightCallback = index => EditorGUI.GetPropertyHeight(serializedProperty.GetArrayElementAtIndex(index), true) + Padding,
                drawElementBackgroundCallback = DrawElementBackgroundCallback,
                drawElementCallback = DrawElementCallback,
                onAddDropdownCallback = AddDropdownCallback,
            };
            
            void AddDropdownCallback(Rect buttonRect, ReorderableList list)
            {
                var menu = new GenericMenu();

                foreach (Type type in types)
                    menu.AddItem(
                        new GUIContent(ObjectNames.NicifyVariableName(type.Name.Substring(prefixLength))),
                        false,
                        () =>
                        {
                            list.serializedProperty.arraySize += 1;

                            var element = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
                            element.managedReferenceValue = Activator.CreateInstance(type);
                            element.isExpanded = true;

                            serializedProperty.serializedObject.ApplyModifiedProperties();
                        }
                    );

                menu.ShowAsContext();
            }

            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                //Foldout Arrow
                rect.width -= 10;
                rect.x += 10;
                
                SerializedProperty element = serializedProperty.GetArrayElementAtIndex(index);

                string heading = GetElementHeading(element, prefixLength, nameHintPropertyName);
                EditorGUI.PropertyField(rect,element,new GUIContent(heading), true);
                
            }

            void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                rect.y -= Padding * 0.5f;

                if (index % 2 == 0 || isActive || isFocused || serializedProperty.arraySize == 0)
                    ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused, true);
                else
                    EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.15f));
            }
            

            static int GetPrefixLength(IReadOnlyList<Type> sortedTypes)
            {
                if (sortedTypes.Count == 0) return 0;
                string shortest = sortedTypes[0].Name;
                string longest = sortedTypes[sortedTypes.Count - 1].Name;

                for (int i = 0; i < longest.Length; i++)
                    if (shortest[i] != longest[i])
                        return i;

                return 0;
            }

            static string GetElementHeading(SerializedProperty element, int prefixLength, string nameHintPropertyName)
            {
                string typeName = ObjectNames.NicifyVariableName(element.managedReferenceFullTypename.Substring(element.managedReferenceFullTypename.LastIndexOf('.') + prefixLength + 1));

                if (string.IsNullOrEmpty(nameHintPropertyName)) return typeName;

                SerializedProperty property = element.FindPropertyRelative(nameHintPropertyName);

                if (property?.objectReferenceValue == null) return typeName;

                var gameObject = property.objectReferenceValue switch
                {
                    GameObject go => go,
                    Component component => component.gameObject,
                    _ => null,
                };

                return gameObject == null ? typeName : $"{gameObject.name} ({typeName})";
            }
        }
    }
}
