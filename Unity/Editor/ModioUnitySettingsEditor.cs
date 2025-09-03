using Modio.Editor.Common;
using Modio.Unity;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Modio.Editor.Unity
{
    [CustomEditor(typeof(ModioUnitySettings))]
    public class ModioUnitySettingsEditor : UnityEditor.Editor
    {
        static readonly string[] Exclude = { "m_Script", "_platformSettings", };
        
        protected ReorderableList Properties;

        protected ReorderableList GetReorderableReferenceArray<T>() => ReorderableReferenceArray.New<T>(serializedObject.FindProperty("_platformSettings"));
        
        void OnEnable() => Properties = GetReorderableReferenceArray<IModioServiceSettings>();
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, Exclude);

            EditorGUILayout.Space();

            GUILayout.Label("Platform Settings", EditorStyles.boldLabel);
            
            Properties.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
                ((ModioUnitySettings)serializedObject.targetObject).InvokeOnChanged();
        }
    }
}
