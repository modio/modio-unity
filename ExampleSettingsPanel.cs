using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIO.Implementation
{
    public class ExampleSettingsPanel : MonoBehaviour
    {
        [SerializeField] InputField gameIdInputField;
        [SerializeField] InputField apiKeyInputField;
        [SerializeField] Text currentServerUrlText;
        [SerializeField] Text currentGameIdText;
        [SerializeField] Button[] buttons;

        public void ActivatePanel(bool isActive)
        {
            currentServerUrlText.text = Settings.server.serverURL;
            currentGameIdText.text = Settings.server.gameId.ToString();

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

                var serverSettings = new ServerSettings();
                var buildSettings = new BuildSettings();

                if(gameIdInputField.text != String.Empty && uint.TryParse(gameIdInputField.text, out uint gameId))
                {
                    serverSettings.gameId = gameId;
                }

                if(apiKeyInputField.text != String.Empty)
                {
                    serverSettings.gameKey = apiKeyInputField.text;
                }

                serverSettings.serverURL = currentServerUrlText.text;

                var result = await ModIOUnityAsync.InitializeForUser("User", serverSettings, buildSettings);

                currentServerUrlText.text = Settings.server.serverURL;
                currentGameIdText.text = Settings.server.gameId.ToString();

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
    }
}
