using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Components.Selectables.Transitions;
using Modio.Unity.UI.Editor.Common;
using UnityEditor;
using UnityEditorInternal;

namespace Modio.Unity.UI.Editor.Components.Selectables
{
    [CustomEditor(typeof(ModioUISelectableTransitions))]
    public class ModioUISelectableTransitionsEditor : UnityEditor.Editor
    {
        bool _isToggle;
        ReorderableList _transitions;

        void OnEnable()
        {
            _isToggle = ((ModioUISelectableTransitions)target).GetComponentInParent<ModioUIToggle>();
            _transitions = ReorderableReferenceArray.New<ISelectableTransition>(serializedObject.FindProperty("_transitions"), nameHintPropertyName: "_target");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Show the toggleFilter if it's any non-default value (as it's probably on a shared component)
            SerializedProperty toggleFilterProperty = serializedObject.FindProperty("_toggleFilter");
            if (_isToggle || toggleFilterProperty.intValue != (int)ModioUISelectableTransitions.ToggleFilter.Any)
            {
                EditorGUILayout.PropertyField(toggleFilterProperty);
                if (!_isToggle)
                {
                    var willBeUsed = toggleFilterProperty.intValue == (int)ModioUISelectableTransitions.ToggleFilter.OnlyOff;
                    EditorGUILayout.HelpBox($"ToggleFilter set on non-toggle; this transition will likely {(willBeUsed ? "ALWAYS" : "NEVER")} be used"
                                            + $"\n(Unless this is a common prefab to be used on Toggles and Non-Toggles interchangeably)"
                        , MessageType.Info);
                }
                EditorGUILayout.Space();
            }

            _transitions.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
