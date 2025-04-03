using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Modio.Unity.UI.Input
{
    public class ModioUIInputPrompt : MonoBehaviour
    {
        [SerializeField] ModioUIInput.ModioAction _action;

        [FormerlySerializedAs("_text")]
        [SerializeField]
        TMP_Text _inputPromptText;
        [SerializeField] Image _textBackground;
        [SerializeField] Image _image;

        [SerializeField] bool _hideIfNoBindings;
        [SerializeField] bool _hideIfNoListener;
        [SerializeField] bool _hideIfController;
        [SerializeField] bool _hideIfNotController;
        [SerializeField] GameObject[] _additionalToHideIfNoBindings;
        Button _button;
        LayoutElement _layoutElement;
        bool _layoutElementIgnoreLayout;

        void Awake()
        {
            _button = GetComponent<Button>();
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement != null) _layoutElementIgnoreLayout = _layoutElement.ignoreLayout;
        }

        void OnEnable()
        {
            var inputPromptDisplayInfo = ModioUIInput.GetInputPromptDisplayInfo(_action);
            inputPromptDisplayInfo.OnUpdated += DisplayInfoUpdated;
            DisplayInfoUpdated(inputPromptDisplayInfo);
        }

        void OnDisable()
        {
            var inputPromptDisplayInfo = ModioUIInput.GetInputPromptDisplayInfo(_action);
            inputPromptDisplayInfo.OnUpdated -= DisplayInfoUpdated;
        }

        public void PressedAction()
        {
            ModioUIInput.PressedAction(_action);
        }

        void DisplayInfoUpdated(ModioUIInput.InputPromptDisplayInfo info)
        {
            if (_hideIfNoListener && !info.InputHasListeners)
            {
                SetElementsVisible(false, false);
            }
            else if (_hideIfController && ModioUIInput.IsUsingGamepad ||
                     _hideIfNotController && !ModioUIInput.IsUsingGamepad)
            {
                SetElementsVisible(false, false);
            }
            else if (info.Icons?.Count > 0)
            {
                SetElementsVisible(false, true);
                _image.sprite = info.Icons[0];
            }
            else if (info.TextPrompts?.Count > 0)
            {
                SetElementsVisible(true, false);
                if (_inputPromptText != null) _inputPromptText.text = info.TextPrompts[0];
            }
            else if (_hideIfNoBindings)
            {
                SetElementsVisible(false, false);
            }
            else if (!ModioUIInput.AnyBindingsExist)
            {
                SetElementsVisible(false, false);
            }
            else
            {
                if (_inputPromptText != null) _inputPromptText.text = "UNBOUND";

                SetElementsVisible(true, false);
            }

            void SetElementsVisible(bool textVisible, bool imageVisible)
            {
                if (_inputPromptText != null) _inputPromptText.gameObject.SetActive(textVisible);
                if (_textBackground != null) _textBackground.gameObject.SetActive(textVisible);
                if (_image != null) _image.gameObject.SetActive(imageVisible);

                var anyVisible = textVisible || imageVisible;

                if (_button != null) _button.interactable = anyVisible;
                if (_layoutElement != null) _layoutElement.ignoreLayout = _layoutElementIgnoreLayout || !anyVisible;

                foreach (GameObject additional in _additionalToHideIfNoBindings)
                {
                    additional.SetActive(anyVisible);
                }
            }
        }
    }
}
