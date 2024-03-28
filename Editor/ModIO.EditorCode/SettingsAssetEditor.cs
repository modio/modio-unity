#if UNITY_EDITOR
using System.Text.RegularExpressions;
using ModIO.Implementation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SettingsAsset))]
public class SettingsAssetEditor : Editor
{
	SerializedProperty serverURL;
	SerializedProperty gameId;
	SerializedProperty gameKey;
	SerializedProperty languageCode;
	int previousGameId = 0;

	void OnEnable()
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
        serializedObject.UpdateIfRequiredOrScript();

        SettingsAsset myTarget = (SettingsAsset)target;

		base.OnInspectorGUI();

		EditorGUILayout.Space();

		GUIStyle labelStyle = new GUIStyle();
		labelStyle.alignment = TextAnchor.MiddleCenter;
		labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;

		EditorGUILayout.LabelField("Server Settings", labelStyle);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(gameId,new GUIContent("Game ID"));
        gameKey.stringValue = EditorGUILayout.PasswordField("API Key", gameKey.stringValue);

        if(myTarget.serverSettings.gameId == 0 || string.IsNullOrWhiteSpace(myTarget.serverSettings.gameKey))
        {
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Once you've created a game profile on mod.io (or test.mod.io), enter your game ID and API key above in order for the plugin to retrieve mods and information associated with your game.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Locate ID and API Key");

            if (GUILayout.Button("test.mod.io"))
            {
                SetURLTest();
                Application.OpenURL("https://test.mod.io/apikey");
            }

            if (GUILayout.Button("mod.io"))
            {
                SetURLProduction();
                Application.OpenURL("https://mod.io/apikey");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        } else {
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serverURL, new GUIContent("Server URL"));
            EditorGUILayout.PropertyField(languageCode, new GUIContent("Language code"));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Insert URL for Test API"))
                SetURLTest();

            if (GUILayout.Button("Insert URL for Production API"))
                SetURLProduction();

            EditorGUILayout.EndHorizontal();
        }

        // If the gameId has been changed, update the url
		if (gameId.intValue != previousGameId)
        {
            if (IsURLProduction(serverURL.stringValue))
                serverURL.stringValue = GetURLProduction(gameId.intValue);

			previousGameId = gameId.intValue;
		}

        //Save the new values
        serializedObject.ApplyModifiedProperties();

        return;

        void SetURLProduction()
        {
            serverURL.stringValue = GetURLProduction(gameId.intValue);
            GUI.FocusControl(null);
        }

        void SetURLTest()
        {
            serverURL.stringValue = GetURLTest(gameId.intValue);
            GUI.FocusControl(null);
        }
    }

    internal static string GetURLProduction(int gameId) => $"https://g-{gameId}.modapi.io/v1";
    static string GetURLTest(int gameId) => "https://api.test.mod.io/v1";

    static bool IsURLProduction(string url) => Regex.IsMatch(url, @"https:\/\/g-\d*.modapi.io\/v1");
}
#endif
