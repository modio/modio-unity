using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Modio.Unity.UI.Editor.Components
{
    [CustomPropertyDrawer(typeof(FontStyles))]
    public class TmpFontStylePropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent k_FontStyleLabel = new GUIContent("Font Style", "Styles to apply to the text such as Bold or Italic.");

        static readonly GUIContent k_BoldLabel = new GUIContent("B", "Bold");
        static readonly GUIContent k_ItalicLabel = new GUIContent("I", "Italic");
        static readonly GUIContent k_UnderlineLabel = new GUIContent("U", "Underline");
        static readonly GUIContent k_StrikethroughLabel = new GUIContent("S", "Strikethrough");
        static readonly GUIContent k_LowercaseLabel = new GUIContent("ab", "Lowercase");
        static readonly GUIContent k_UppercaseLabel = new GUIContent("AB", "Uppercase");
        static readonly GUIContent k_SmallcapsLabel = new GUIContent("SC", "Smallcaps");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect rect = position;
            int v1, v2, v3, v4, v5, v6, v7;

            var fontStyleProperty = property;

            if (EditorGUIUtility.wideMode)
            {
                EditorGUI.BeginProperty(rect, k_FontStyleLabel, fontStyleProperty);

                EditorGUI.PrefixLabel(rect, k_FontStyleLabel);

                int styleValue = fontStyleProperty.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 7f);
                
                v1 = EditorToggle(rect, (styleValue & 1) == 1, k_BoldLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
                rect.x += rect.width;
                v2 = EditorToggle(rect, (styleValue & 2) == 2, k_ItalicLabel, TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
                rect.x += rect.width;
                v3 = EditorToggle(rect, (styleValue & 4) == 4, k_UnderlineLabel, TMP_UIStyleManager.alignmentButtonMid) ? 4 : 0; // Underline
                rect.x += rect.width;
                v7 = EditorToggle(rect, (styleValue & 64) == 64, k_StrikethroughLabel, TMP_UIStyleManager.alignmentButtonRight) ? 64 : 0; // Strikethrough
                rect.x += rect.width;

                int selected = 0;

                EditorGUI.BeginChangeCheck();
                v4 = EditorToggle(rect, (styleValue & 8) == 8, k_LowercaseLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 8 : 0; // Lowercase
                if (EditorGUI.EndChangeCheck() && v4 > 0)
                {
                    selected = v4;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v5 = EditorToggle(rect, (styleValue & 16) == 16, k_UppercaseLabel, TMP_UIStyleManager.alignmentButtonMid) ? 16 : 0; // Uppercase
                if (EditorGUI.EndChangeCheck() && v5 > 0)
                {
                    selected = v5;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v6 = EditorToggle(rect, (styleValue & 32) == 32, k_SmallcapsLabel, TMP_UIStyleManager.alignmentButtonRight) ? 32 : 0; // Smallcaps
                if (EditorGUI.EndChangeCheck() && v6 > 0)
                {
                    selected = v6;
                }

                if (selected > 0)
                {
                    v4 = selected == 8 ? 8 : 0;
                    v5 = selected == 16 ? 16 : 0;
                    v6 = selected == 32 ? 32 : 0;
                }

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.BeginProperty(rect, k_FontStyleLabel, fontStyleProperty);

                EditorGUI.PrefixLabel(rect, k_FontStyleLabel);

                int styleValue = fontStyleProperty.intValue;
                
                rect.height += 20f;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                rect.width = Mathf.Max(25f, rect.width / 4f);

                v1 = EditorToggle(rect, (styleValue & 1) == 1, k_BoldLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
                rect.x += rect.width;
                v2 = EditorToggle(rect, (styleValue & 2) == 2, k_ItalicLabel, TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
                rect.x += rect.width;
                v3 = EditorToggle(rect, (styleValue & 4) == 4, k_UnderlineLabel, TMP_UIStyleManager.alignmentButtonMid) ? 4 : 0; // Underline
                rect.x += rect.width;
                v7 = EditorToggle(rect, (styleValue & 64) == 64, k_StrikethroughLabel, TMP_UIStyleManager.alignmentButtonRight) ? 64 : 0; // Strikethrough
                
                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 4f);

                int selected = 0;

                EditorGUI.BeginChangeCheck();
                v4 = EditorToggle(rect, (styleValue & 8) == 8, k_LowercaseLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 8 : 0; // Lowercase
                if (EditorGUI.EndChangeCheck() && v4 > 0)
                {
                    selected = v4;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v5 = EditorToggle(rect, (styleValue & 16) == 16, k_UppercaseLabel, TMP_UIStyleManager.alignmentButtonMid) ? 16 : 0; // Uppercase
                if (EditorGUI.EndChangeCheck() && v5 > 0)
                {
                    selected = v5;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v6 = EditorToggle(rect, (styleValue & 32) == 32, k_SmallcapsLabel, TMP_UIStyleManager.alignmentButtonRight) ? 32 : 0; // Smallcaps
                if (EditorGUI.EndChangeCheck() && v6 > 0)
                {
                    selected = v6;
                }

                if (selected > 0)
                {
                    v4 = selected == 8 ? 8 : 0;
                    v5 = selected == 16 ? 16 : 0;
                    v6 = selected == 32 ? 32 : 0;
                }

                EditorGUI.EndProperty();
            }

            if (EditorGUI.EndChangeCheck())
            {
                fontStyleProperty.intValue = v1 + v2 + v3 + v4 + v5 + v6 + v7;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;
        
        static bool EditorToggle(Rect position, bool value, GUIContent content, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(content, FocusType.Keyboard, position);
            var evt = Event.current;

            // Toggle selected toggle on space or return key
            if (GUIUtility.keyboardControl == id && evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
            {
                value = !value;
                evt.Use();
                GUI.changed = true;
            }

            if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = id;
                EditorGUIUtility.editingTextField = false;
                HandleUtility.Repaint();
            }

            return GUI.Toggle(position, id, value, content, style);
        }
    }
}
