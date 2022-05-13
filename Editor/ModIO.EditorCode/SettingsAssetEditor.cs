#if UNITY_EDITOR
using ModIO.Implementation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SettingsAsset))]
public class SettingsAssetEditor : Editor
{
    private SerializedProperty serverURL;
    private SerializedProperty gameId;
    private SerializedProperty gameKey;
    private SerializedProperty languageCode;

    private void OnEnable()
    {
        //get references to SerializedProperties
        var serverSettingsProperty = serializedObject.FindProperty("serverSettings");
        serverURL = serverSettingsProperty.FindPropertyRelative("serverURL");
        gameId = serverSettingsProperty.FindPropertyRelative("gameId");
        gameKey = serverSettingsProperty.FindPropertyRelative("gameKey");
        languageCode = serverSettingsProperty.FindPropertyRelative("languageCode");
    }

    public override void OnInspectorGUI()
	{
        //Grab any changes to the original object data
        this.serializedObject.UpdateIfRequiredOrScript();

        SettingsAsset myTarget = (SettingsAsset)target;

		base.OnInspectorGUI();

		EditorGUILayout.Space();

		GUIStyle labelStyle = new GUIStyle();
		labelStyle.alignment = TextAnchor.MiddleCenter;
		labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;

		EditorGUILayout.LabelField("Server Settings", labelStyle);
		if(myTarget.serverSettings.gameId == 0 || string.IsNullOrWhiteSpace(myTarget.serverSettings.gameKey))
		{
			EditorGUILayout.HelpBox("Once you've created a game profile on mod.io (or test.mod.io) "
			                        + "you can input the game ID and Key below in order for the plugin "
			                        + "to retrieve mods and information associated to your game.",
									MessageType.Info);
		}

        EditorGUILayout.PropertyField(serverURL, new GUIContent("Server URL"));
        EditorGUILayout.PropertyField(gameId,new GUIContent("Game ID"));
        gameKey.stringValue = EditorGUILayout.PasswordField("API Key", gameKey.stringValue);
        EditorGUILayout.PropertyField(languageCode, new GUIContent("Language code"));


        EditorGUILayout.Space();
		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Insert URL for Test API"))
		{
            serverURL.stringValue = "https://api.test.mod.io/v1";

            //remove focus from other fields
            GUI.FocusControl(null);
        }
		if(GUILayout.Button("Insert URL for Production API"))
		{
            serverURL.stringValue = "https://api.mod.io/v1";
            //remove focus from other fields
            GUI.FocusControl(null);
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

        //Save the new values
        this.serializedObject.ApplyModifiedProperties();
    }
}
#endif
