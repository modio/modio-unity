using System;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.Localization
{
    public class ModioUILocalizedText : MonoBehaviour
    {
        [SerializeField] string _key;

        [SerializeField] TMP_Text _tmpText;

        [SerializeField] TMP_Text[] _splitFormatArgs;

        object[] _args;
        string _initialKey;

        void Reset()
        {
            _tmpText = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            ModioUILocalizationManager.LanguageSet += UpdateText;
        }

        void OnDisable()
        {
            ModioUILocalizationManager.LanguageSet -= UpdateText;
        }

        public void SetFormatArgs(params object[] args)
        {
            _args = args;

            UpdateText();
        }

        void UpdateText()
        {
            if (!string.IsNullOrEmpty(_key))
            {
                var text = ModioUILocalizationManager.GetLocalizedText(_key);

                if (_splitFormatArgs?.Length > 0)
                {
                    var strings = text.Split(new[] { "{0}" }, StringSplitOptions.None);
                    _splitFormatArgs[0].text = strings.Length > 0 ? strings[0] : "";
                    if (_splitFormatArgs.Length > 2) _splitFormatArgs[2].text = strings.Length > 1 ? strings[1] : "";

                    if (_splitFormatArgs.Length > 1)
                        _splitFormatArgs[1].text = _args?.Length > 0 ? _args[0]?.ToString() : "";
                }
                else if (_tmpText != null)
                {
                    if (_args != null)
                    {
                        text = string.Format(text, _args);
                    }

                    _tmpText.text = text;
                }
            }
        }

        public bool SetKeyIfItExists(string key)
        {
            if (!string.IsNullOrEmpty(ModioUILocalizationManager.GetLocalizedText(key, false)))
            {
                SetKey(key);
                return true;
            }
            return false;
        }
        public void SetKey(string key)
        {
            if (string.IsNullOrEmpty(_initialKey))
                _initialKey = _key;
            _key = key;
            UpdateText();
        }

        public void ResetKey()
        {
            if (!string.IsNullOrEmpty(_initialKey)) SetKey(_initialKey);
        }

        public void SetKey(string key, params object[] args)
        {
            _args = args;
            SetKey(key);
        }
    }
}
