#if UNITY_EDITOR
using System.Text.RegularExpressions;
using ModIO.Implementation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof( SettingsAsset ))]
public class SettingsAssetEditor : Editor
{
    SerializedProperty gameId;
    SerializedProperty gameKey;
    SerializedProperty languageCode;
    int previousGameId;
    SerializedProperty serverURL;
    SerializedProperty useCommandLineArgumentOverrides;
    SerializedProperty _showMonetizationUIProperty;
    SerializedProperty _showEnabledModToggleProperty;
    SerializedProperty _fallbackToEmailProperty;

    void OnEnable()
    {
        //get references to SerializedProperties
        SerializedProperty serverSettingsProperty = serializedObject.FindProperty("serverSettings");
        serverURL = serverSettingsProperty.FindPropertyRelative("serverURL");
        gameId = serverSettingsProperty.FindPropertyRelative("gameId");
        gameKey = serverSettingsProperty.FindPropertyRelative("gameKey");
        languageCode = serverSettingsProperty.FindPropertyRelative("languageCode");
        useCommandLineArgumentOverrides = serverSettingsProperty.FindPropertyRelative("useCommandLineArgumentOverrides");

        var uiSettingsProperty = serializedObject.FindProperty("uiSettings");

        _showMonetizationUIProperty = uiSettingsProperty.FindPropertyRelative("ShowMonetizationUI");
        _fallbackToEmailProperty = serverSettingsProperty.FindPropertyRelative("fallbackToEmailAuth");
        _showEnabledModToggleProperty = uiSettingsProperty.FindPropertyRelative("ShowEnabledModToggle");
    }

    public override void OnInspectorGUI()
    {
        //Grab any changes to the original object data
        serializedObject.UpdateIfRequiredOrScript();

        SettingsAsset myTarget = (SettingsAsset)target;

        DrawPropertiesExcluding(serializedObject, "m_Script");

        EditorGUILayout.Space();

        GUIStyle labelStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = Color.white,
            },
        };

        EditorGUILayout.LabelField("Server Settings", labelStyle);

        EditorGUILayout.Space();

        EditorGUILayout.DelayedIntField(gameId, new GUIContent("Game ID"));

        using (EditorGUI.ChangeCheckScope passwordChange = new EditorGUI.ChangeCheckScope())
        {
            string tempPassword = EditorGUILayout.PasswordField("API Key", gameKey.stringValue);
            if (passwordChange.changed)
                gameKey.stringValue = tempPassword;
        }

        EditorGUILayout.PropertyField(_fallbackToEmailProperty);
        if (_fallbackToEmailProperty.boolValue)
        {
            EditorGUILayout.HelpBox("This may cause certification failures on platforms", MessageType.Warning);
        }

        if (myTarget.serverSettings.gameId == 0 || string.IsNullOrWhiteSpace(myTarget.serverSettings.gameKey))
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
        }
        else
        {
            EditorGUILayout.Space();

            EditorGUILayout.DelayedTextField(serverURL, new GUIContent("Server URL"));
            EditorGUILayout.DelayedTextField(languageCode, new GUIContent("Language code"));
            EditorGUILayout.PropertyField(useCommandLineArgumentOverrides, new GUIContent("Use Command Line Argument Override"));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Insert URL for Staging API"))
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

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Settings", labelStyle);

        EditorGUILayout.PropertyField(_showMonetizationUIProperty);
        EditorGUILayout.PropertyField(_showEnabledModToggleProperty);

        //Save the new values
        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(serializedObject?.targetObject);

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

    static string GetURLTest(int gameId) => "https://api-staging.moddemo.io/v1";

    static bool IsURLProduction(string url) => Regex.IsMatch(url, @"https:\/\/g-\d*.modapi.io\/v1");
}
#endif
