using System;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.Implementation
{
    public class ExampleSettingsPanel : MonoBehaviour
    {
        [SerializeField] TMP_InputField gameIdInputField;
        [SerializeField] TMP_InputField apiKeyInputField;
        [SerializeField] TMP_InputField initUserInputField;
        [SerializeField] TextMeshProUGUI currentServerUrlText;
        [SerializeField] TextMeshProUGUI currentGameIdText;
        [SerializeField] Button[] buttons;

        Translation gameIdTranslation = null;
        Translation serverUrlTranslation = null;
        string urlToUse;

        public void ActivatePanel(bool isActive)
        {
            SetServerUrl(Settings.server.serverURL);
            
            Translation.Get(serverUrlTranslation, "Server Url: {text}", currentServerUrlText, Settings.server.serverURL);
            Translation.Get(gameIdTranslation, "Game id: {text}", currentGameIdText, Settings.server.gameId.ToString());

            gameObject.SetActive(isActive);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        public void SetServerUrl(string url)
        {
            urlToUse = url;
            Translation.Get(serverUrlTranslation, "Server Url: {text}", currentServerUrlText, urlToUse);
        }

        public async void SaveSettings()
        {
            try
            {
                foreach(var b in buttons)
                {
                    b.enabled = false;
                }

                if(ModIOUnity.IsInitialized())
                {
                    await ModIOUnityAsync.Shutdown();
                }

                var serverSettings = new ServerSettings(Settings.server);
                var buildSettings = new BuildSettings(Settings.build);

                if(gameIdInputField.text != String.Empty && uint.TryParse(gameIdInputField.text, out uint gameId))
                {
                    serverSettings.gameId = gameId;
                }

                if(apiKeyInputField.text != String.Empty)
                {
                    serverSettings.gameKey = apiKeyInputField.text;
                }

                serverSettings.serverURL = urlToUse;

                string user = string.IsNullOrWhiteSpace(initUserInputField.text) ? "User" : initUserInputField.text;
                ModIOUnity.InitializeForUser(user, serverSettings, buildSettings);

                Translation.Get(gameIdTranslation, "Game id: {text}", currentGameIdText, Settings.server.gameId.ToString());
                Translation.Get(serverUrlTranslation, "Server Url: {text}", currentServerUrlText, urlToUse);
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
            }
            finally
            {
                foreach(var b in buttons)
                {
                    b.enabled = true;
                }
            }

        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
