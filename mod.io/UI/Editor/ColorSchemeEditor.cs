#if UNITY_EDITOR

using UnityEditor;
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

			myTarget.Accent = EditorGUILayout.ColorField("Highlight", myTarget.Accent);
			myTarget.LightGrey1 = EditorGUILayout.ColorField("Inactive1", myTarget.LightGrey1);
			myTarget.LightGrey2 = EditorGUILayout.ColorField("Inactive2", myTarget.LightGrey2);
			myTarget.LightGrey3 = EditorGUILayout.ColorField("Inactive3", myTarget.LightGrey3);
			myTarget.Green = EditorGUILayout.ColorField("Positive Accent", myTarget.Green);
			myTarget.Red = EditorGUILayout.ColorField("Negative Accent", myTarget.Red);
			
			myTarget.LightMode = EditorGUILayout.Toggle("Light Mode", myTarget.LightMode);
			
			if(GUILayout.Button("Refresh Layout"))
			{
				myTarget.RefreshUI();
				EditorUtility.SetDirty(myTarget);
			}
			EditorGUILayout.Space();

			if(GUI.changed)
			{
				myTarget.RefreshUI();
				EditorUtility.SetDirty(myTarget);
			}
		}
	}
}

#endif