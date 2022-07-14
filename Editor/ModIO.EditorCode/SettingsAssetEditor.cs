#if UNITY_EDITOR
using ModIO.Implementation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SettingsAsset))]
public class SettingsAssetEditor : Editor
{
	public override void OnInspectorGUI()
	{
		SettingsAsset myTarget = (SettingsAsset)target;

		base.OnInspectorGUI();
		
		EditorGUILayout.Space();
		
		GUIStyle labelStyle = new GUIStyle();
		labelStyle.alignment = TextAnchor.MiddleCenter;
		labelStyle.fontStyle = FontStyle.Bold;
		EditorGUILayout.LabelField("Server Settings", labelStyle);
		if(myTarget.serverSettings.gameId == 0 || string.IsNullOrWhiteSpace(myTarget.serverSettings.gameKey))
		{
			EditorGUILayout.HelpBox("Once you've created a game profile on mod.io (or test.mod.io) "
			                        + "you can input the game ID and Key below in order for the plugin "
			                        + "to retrieve mods and information associated to your game.", 
									MessageType.Info);
		}
		myTarget.serverSettings.serverURL = EditorGUILayout.TextField("Server URL", myTarget.serverSettings.serverURL);
		myTarget.serverSettings.gameId = (uint)EditorGUILayout.IntField("Game ID", (int)myTarget.serverSettings.gameId);
		myTarget.serverSettings.gameKey = EditorGUILayout.PasswordField("API Key", myTarget.serverSettings.gameKey);
		myTarget.serverSettings.languageCode = EditorGUILayout.TextField("Language code", myTarget.serverSettings.languageCode);
		
		EditorGUILayout.Space();
		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Insert URL for Test API"))
		{
			myTarget.serverSettings.serverURL = "https://api.test.mod.io/v1";
		}
		if(GUILayout.Button("Insert URL for Production API"))
		{
			myTarget.serverSettings.serverURL = "https://api.mod.io/v1";
		}
		EditorGUILayout.EndHorizontal();

		if(GUILayout.Button("Locate ID and API Key"))
		{
			if(myTarget.serverSettings.serverURL == "https://api.test.mod.io/v1")
			{
				Application.OpenURL("https://test.mod.io/apikey");
			}
			else
			{
				Application.OpenURL("https://mod.io/apikey");
			}
		}
		
		if(GUI.changed)
		{
			AssetDatabase.Refresh();
			EditorUtility.SetDirty(myTarget);
			AssetDatabase.SaveAssets();
		}
	}

}
#endif