using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUIToggleOutputSplitter : MonoBehaviour
    {
        public Toggle.ToggleEvent onToggleOn = new Toggle.ToggleEvent();
        public Toggle.ToggleEvent onToggleOff = new Toggle.ToggleEvent();

        Toggle _toggle;
        bool _hasFiredEvent;

        void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(ToggleValueChanged);
        }

        void Start()
        {
            //Mimic existing toggle behaviour;
            if (_toggle.isOn && !_hasFiredEvent) ToggleValueChanged(true);
        }

        void ToggleValueChanged(bool isOn)
        {
            _hasFiredEvent = true;
            if (isOn) onToggleOn.Invoke(true);
            else onToggleOff.Invoke(false);
        }
    }
}
