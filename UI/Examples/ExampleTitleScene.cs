using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModIOBrowser;
using ModIOBrowser.Implementation;
using TMPro;
using ModIO.Implementation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_PS5 || UNITY_PS4
using Sony.NP;
using PSNSample;
#endif

namespace Plugins.mod.io.UI.Examples
{

    public class ExampleTitleScene : MonoBehaviour
    {
        [SerializeField] Selectable DefaultSelection;
        [SerializeField] private ExampleSettingsPanel exampleSettingsPanel;
        public string verticalControllerInput = "Vertical";
        public List<string> mouseInput = new List<string>();
        public MultiTargetDropdown languageSelectionDropdown;

        void Start()
        {
            OpenTitle();

            languageSelectionDropdown.gameObject.SetActive(false);
            StartCoroutine(SetupTranslationDropDown());
        }

        IEnumerator SetupTranslationDropDown()
        {
            while(!TranslationManager.SingletonIsInstantiated())
            {
                yield return new WaitForSeconds(0.1f);
            }

            languageSelectionDropdown.gameObject.SetActive(true);
            languageSelectionDropdown.ClearOptions();

            languageSelectionDropdown.AddOptions(Enum.GetNames(typeof(TranslatedLanguages))
                .Select(x => new TMP_Dropdown.OptionData(x.ToString()))
                .ToList());

            languageSelectionDropdown.value = (int)TranslationManager.Instance.SelectedLanguage;
        }

        public void OnTranslationDropdownChange()
        {
            TranslationManager.Instance.ChangeLanguage((TranslatedLanguages)languageSelectionDropdown.value);
        }

        public void OpenMods()
        {
            //Browser is now opened via BrowserSpawnIn.SpawnIn, also connected to the button which activates this
            gameObject.transform.parent.gameObject.SetActive(false);
        }

        public void OpenSettings()
        {
            exampleSettingsPanel.ActivatePanel(true);
        }

        public void OpenTitle()
        {
            //Browser.Instance.gameObject needs to stay on so that translations, glyphsettings etc
            //can initialize
            gameObject.transform.parent.gameObject.SetActive(true);
            DefaultSelection.Select();
        }

        public void Quit()
        {
            Application.Quit();
        }

        public void DeselectOtherTitles()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if(Input.GetAxis(verticalControllerInput) != 0f)
            {
                //Hide mouse
                Cursor.lockState = CursorLockMode.Locked;

                if(EventSystem.current.currentSelectedGameObject == null)
                {
                    DefaultSelection.Select();
                }
            }
            else if(mouseInput.Any(x => Input.GetAxis(x) != 0))
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}
