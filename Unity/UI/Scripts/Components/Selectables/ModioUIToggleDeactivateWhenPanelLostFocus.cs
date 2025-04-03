using Modio.Unity.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUIToggleDeactivateWhenPanelLostFocus : MonoBehaviour
    {
        [SerializeField] ModioPanelBase _panel;

        Toggle _toggle;

        void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }

        void OnValueChanged(bool isOn)
        {
            if (_panel == null) return;
            _panel.OnHasFocusChanged -= PanelChangedFocus;

            if (isOn)
            {
                _panel.OnHasFocusChanged += PanelChangedFocus;
            }
        }

        void OnDestroy()
        {
            if (_panel != null) _panel.OnHasFocusChanged -= PanelChangedFocus;
        }

        void PanelChangedFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _toggle.isOn = false;
            }
        }
    }
}
