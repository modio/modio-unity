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
        [SerializeField] TextMeshProUGUI currentServerUrlText;
        [SerializeField] TextMeshProUGUI currentGameIdText;
        [SerializeField] Button[] buttons;

        private Translation gameIdTranslation = null;
        private Translation serverUrlTranslation = null;

        public void ActivatePanel(bool isActive)
        {
            Translation.Get(serverUrlTranslation, "Server Url: {text}", currentServerUrlText, Settings.server.serverURL);
            Translation.Get(gameIdTranslation, "Game id: {text}", currentGameIdText, Settings.server.gameId.ToString());

            this.gameObject.SetActive(isActive);
        }

        public void SetServerUrl(string url)
        {
            currentServerUrlText.text = url;
        }

        public async void SaveSettings()
        {
            try
            {
                foreach(var b in this.buttons)
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

                serverSettings.serverURL = currentServerUrlText.text;

                var result = ModIOUnity.InitializeForUser("User", serverSettings, buildSettings);

                Translation.Get(gameIdTranslation, "Game id: {text}", currentGameIdText, Settings.server.gameId.ToString());
                Translation.Get(serverUrlTranslation, "Server Url: {text}", currentServerUrlText, Settings.server.serverURL);
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
            }
            finally
            {
                foreach(var b in this.buttons)
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
