#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    [CustomEditor(typeof(MultiTargetToggle))]
    public class MultiTargetToggleEditor : ToggleEditor
    {
        public override void OnInspectorGUI()
        {
            
            MultiTargetToggle myTarget = (MultiTargetToggle)target;

            if(myTarget.scheme == null)
            {
                myTarget.scheme = FindObjectOfType<ColorScheme>();
            }
            myTarget.scheme = (ColorScheme)EditorGUILayout.ObjectField("Color Scheme", myTarget.scheme, typeof(ColorScheme), true);

            base.OnInspectorGUI();
        
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = UnityEngine.FontStyle.Bold;
        
            Target removeTarget = null;
            foreach(var t in myTarget.extraTargets)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Added Target", labelStyle);
        
                t.transition = (MultiTargetTransition)EditorGUILayout.EnumPopup("Transition Type", t.transition);
        
                switch(t.transition)
                {
                    case MultiTargetTransition.None:
                        break;
                    case MultiTargetTransition.ColorTint:
                        t.target = (Graphic)EditorGUILayout.ObjectField("Target", t.target, typeof(Graphic), true);
                        ColorBlock colors = t.colors;
                        colors.normalColor = EditorGUILayout.ColorField("Normal", colors.normalColor);
                        colors.highlightedColor = EditorGUILayout.ColorField("Highlighted", colors.highlightedColor);
                        colors.pressedColor = EditorGUILayout.ColorField("Pressed", colors.pressedColor);
                        colors.disabledColor = EditorGUILayout.ColorField("Disabled", colors.disabledColor);
                        colors.colorMultiplier = EditorGUILayout.Slider("Color Multiplier", colors.colorMultiplier, 1f, 5f);
                        ;
                        colors.fadeDuration = EditorGUILayout.FloatField("Fade Duration", colors.fadeDuration);
                        t.colors = colors;
                        break;
                    case MultiTargetTransition.SpriteSwap:
                        t.target = (Graphic)EditorGUILayout.ObjectField("Target", t.target, typeof(Graphic), true);
                        SpriteState sprites = t.spriteState;
                        sprites.highlightedSprite = (Sprite)EditorGUILayout.ObjectField("Highlighted", sprites.highlightedSprite, typeof(Sprite), true);
                        sprites.pressedSprite = (Sprite)EditorGUILayout.ObjectField("Pressed", sprites.pressedSprite, typeof(Sprite), true);
                        sprites.disabledSprite = (Sprite)EditorGUILayout.ObjectField("Disabled", sprites.disabledSprite, typeof(Sprite), true);
                        t.spriteState = sprites;
                        break;
                    case MultiTargetTransition.Animation:
                        t.animator = (Animator)EditorGUILayout.ObjectField("Animator Controller", t.animator, typeof(Animator), true);
                        AnimationTriggers triggers = t.animationTriggers;
                        triggers.normalTrigger = EditorGUILayout.TextField("Normal Trigger", triggers.normalTrigger);
                        triggers.highlightedTrigger = EditorGUILayout.TextField("Highlighted Trigger", triggers.highlightedTrigger);
                        triggers.pressedTrigger = EditorGUILayout.TextField("Pressed Trigger", triggers.pressedTrigger);
                        triggers.disabledTrigger = EditorGUILayout.TextField("Disabled Trigger", triggers.disabledTrigger);
                        t.animationTriggers = triggers;
                        break;
                    case MultiTargetTransition.DisableEnable:
                        t.target = (Graphic)EditorGUILayout.ObjectField("Target", t.target, typeof(Graphic), true);
                        t.enableOnNormal = EditorGUILayout.Toggle("Normal", t.enableOnNormal);
                        t.enableOnHighlight = EditorGUILayout.Toggle("Highlighted", t.enableOnHighlight);
                        t.enableOnPressed = EditorGUILayout.Toggle("Pressed", t.enableOnPressed);
                        t.enableOnDisabled = EditorGUILayout.Toggle("Disabled", t.enableOnDisabled);
                        break;
                    case MultiTargetTransition.ColorScheme:
                        t.target = (Graphic)EditorGUILayout.ObjectField("Target", t.target, typeof(Graphic), true);
                        t.colorSchemeBlock.Normal = (ColorSetterType)EditorGUILayout.EnumPopup("Normal", t.colorSchemeBlock.Normal);
                        t.colorSchemeBlock.NormalColorAlpha = EditorGUILayout.Slider("Normal Alpha", t.colorSchemeBlock.NormalColorAlpha, 0f, 1f);
                        t.colorSchemeBlock.Highlighted = (ColorSetterType)EditorGUILayout.EnumPopup("Highlighted", t.colorSchemeBlock.Highlighted);
                        t.colorSchemeBlock.HighlightedColorAlpha = EditorGUILayout.Slider("Highlighted Alpha", t.colorSchemeBlock.HighlightedColorAlpha, 0f, 1f);
                        t.colorSchemeBlock.Pressed = (ColorSetterType)EditorGUILayout.EnumPopup("Pressed", t.colorSchemeBlock.Pressed);
                        t.colorSchemeBlock.PressedColorAlpha = EditorGUILayout.Slider("Pressed Alpha", t.colorSchemeBlock.PressedColorAlpha, 0f, 1f);
                        t.colorSchemeBlock.Disabled = (ColorSetterType)EditorGUILayout.EnumPopup("Disabled", t.colorSchemeBlock.Disabled);
                        t.colorSchemeBlock.DisabledColorAlpha = EditorGUILayout.Slider("Disabled Alpha", t.colorSchemeBlock.DisabledColorAlpha, 0f, 1f);
                        t.colorSchemeBlock.ColorMultiplier = EditorGUILayout.Slider("Color Multiplier", t.colorSchemeBlock.ColorMultiplier, 1f, 5f);
                        t.colorSchemeBlock.FadeDuration = EditorGUILayout.FloatField("Fade Duration", t.colorSchemeBlock.FadeDuration);
                        break;
                }
                if(GUILayout.Button("Remove"))
                {
                    removeTarget = t;
                }
            }
            if(removeTarget != null)
            {
                myTarget.extraTargets.Remove(removeTarget);
            }
        
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if(GUILayout.Button("Add Target"))
            {
                myTarget.extraTargets.Add(new Target());
            }
        
            if(GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }        
    }
}

#endif