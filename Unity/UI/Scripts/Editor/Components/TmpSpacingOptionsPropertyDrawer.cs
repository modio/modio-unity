using Modio.Unity.UI.Scripts.Themes.Options;
using UnityEditor;
using UnityEngine;

namespace Modio.Unity.UI.Editor.Components
{
    [CustomPropertyDrawer(typeof(TmpSpacingWrapper))]
    public class TmpSpacingOptionsPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent k_SpacingOptionsLabel = new GUIContent("Spacing Options (em)", "Spacing adjustments between different elements of the text. Values are in font units where a value of 1 equals 1/100em.");
        static readonly GUIContent k_CharacterSpacingLabel = new GUIContent("Character");
        static readonly GUIContent k_WordSpacingLabel = new GUIContent("Word");
        static readonly GUIContent k_LineSpacingLabel = new GUIContent("Line");
        static readonly GUIContent k_ParagraphSpacingLabel = new GUIContent("Paragraph");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var charSpaceProperty = property.FindPropertyRelative("CharacterSpacing");
            var wordSpaceProperty = property.FindPropertyRelative("WordSpacing");
            var lineSpaceProperty = property.FindPropertyRelative("LineSpacing");
            var paraSpaceProperty = property.FindPropertyRelative("ParagraphSpacing");
            
            // CHARACTER, LINE & PARAGRAPH SPACING
            EditorGUI.BeginChangeCheck();
            
            Rect rect = position;

            rect.height = EditorGUIUtility.singleLineHeight;
            
            EditorGUI.PrefixLabel(rect, k_SpacingOptionsLabel);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float currentLabelWidth = EditorGUIUtility.labelWidth;
            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth - 3f) / 2f;

            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, charSpaceProperty, k_CharacterSpacingLabel);
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, wordSpaceProperty, k_WordSpacingLabel);
            
            rect.x = position.x;
            rect.width = position.width;
            rect.y += EditorGUIUtility.singleLineHeight;
            
            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth -3f) / 2f;
            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, lineSpaceProperty, k_LineSpacingLabel);
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, paraSpaceProperty, k_ParagraphSpacingLabel);

            EditorGUIUtility.labelWidth = currentLabelWidth;
            EditorGUI.indentLevel = oldIndent;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight * 2f;
    }
}
