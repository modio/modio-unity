#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace ModIOBrowser
{
    [CustomEditor(typeof(ColorSetter))]
    public class ColorSetterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ColorSetter myTarget = (ColorSetter)target;

            if(GUILayout.Button("Refresh"))
            {
                myTarget.Refresh();
            }

            myTarget.type = (ColorSetterType)EditorGUILayout.EnumPopup("Palette", myTarget.type);
        }
    }
}

#endif

