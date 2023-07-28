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

        string urlToUse;

        public void ActivatePanel(bool isActive)
        {
            SetServerUrl(Settings.server.serverURL);

            currentServerUrlText.text = $"Server Url: {Settings.server.serverURL}";
            currentGameIdText.text = $"Game Id: {Settings.server.gameId}";

            gameObject.SetActive(isActive);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        public void SetProductionUrl()
        {
            urlToUse = $"https://g-{Settings.server.gameId}.modapi.io/v1";
            currentServerUrlText.text = $"Server Url: {urlToUse}";
        }

        public void SetTestUrl()
        {
            urlToUse = "https://api.test.mod.io/v1";
            currentServerUrlText.text = $"Server Url: {urlToUse}";
        }

        public void SetServerUrl(string url)
        {
            urlToUse = url;
            currentServerUrlText.text = $"Server Url: {urlToUse}";
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
                var buildSettings = new BuildSettings();

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

                currentServerUrlText.text = $"Server Url: {urlToUse}";
                currentGameIdText.text = $"Game Id: {Settings.server.gameId}";
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
