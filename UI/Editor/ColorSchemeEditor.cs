#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ModIOBrowser
{


    [CustomEditor(typeof(ColorScheme))]
	internal class ColorSchemeEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			ColorScheme myTarget = (ColorScheme)target;
			
			if(GUILayout.Button("Restore Default Colors"))
			{
				myTarget.SetColorsToDefault();
				myTarget.RefreshUI();
			}
			
			myTarget.Dark1 = EditorGUILayout.ColorField("Dark1", myTarget.Dark1);
			myTarget.Dark2 = EditorGUILayout.ColorField("Dark2", myTarget.Dark2);
			myTarget.Dark3 = EditorGUILayout.ColorField("Dark3", myTarget.Dark3);
			myTarget.White = EditorGUILayout.ColorField("White", myTarget.White);

			myTarget.Highlight = EditorGUILayout.ColorField("Highlight", myTarget.Highlight);
			myTarget.Inactive1 = EditorGUILayout.ColorField("Inactive1", myTarget.Inactive1);
			myTarget.Inactive2 = EditorGUILayout.ColorField("Inactive2", myTarget.Inactive2);
			myTarget.Inactive3 = EditorGUILayout.ColorField("Inactive3", myTarget.Inactive3);
			myTarget.PositiveAccent = EditorGUILayout.ColorField("Positive Accent", myTarget.PositiveAccent);
			myTarget.NegativeAccent = EditorGUILayout.ColorField("Negative Accent", myTarget.NegativeAccent);
			
			myTarget.LightMode = EditorGUILayout.Toggle("Light Mode", myTarget.LightMode);
			
			if(GUILayout.Button("Refresh Layout"))
			{
				myTarget.RefreshUI();
				EditorSceneManager.MarkSceneDirty(myTarget.gameObject.scene);
				EditorUtility.SetDirty(myTarget);
			}
			EditorGUILayout.Space();

			if(GUI.changed)
			{
				myTarget.RefreshUI();
				EditorSceneManager.MarkSceneDirty(myTarget.gameObject.scene);
				EditorUtility.SetDirty(myTarget);
			}
		}
	}
}

#endif
