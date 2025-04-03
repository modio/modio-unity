using System.Collections.Generic;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModioPanelManager : MonoBehaviour
    {
        readonly List<ModioPanelBase> _allPotentialPanels = new List<ModioPanelBase>();

        readonly List<ModioPanelBase> _openWindows = new List<ModioPanelBase>();
        static ModioPanelManager _instance;
        public ModioPanelBase CurrentFocusedPanel =>
            _openWindows.Count > 0 ? _openWindows[_openWindows.Count - 1] : null;

        public static ModioPanelManager GetInstance()
        {
            if (_instance != null) return _instance;

            //This can be omitted if you can guarantee there isn't one in your scene,
            // and you're relying on the following code to construct one.
            // If that can't be guaranteed, this avoids some rough race conditions.
            _instance = FindObjectOfType<ModioPanelManager>();

            if (_instance != null) return _instance;

            var gameObject = new GameObject("ModioPanelManager");
            _instance = gameObject.AddComponent<ModioPanelManager>();

            return _instance;
        }

        void Awake()
        {
            _instance = this;
        }

        public void OpenPanel(ModioPanelBase modioPanelBase)
        {
            if (_openWindows.Count > 0) _openWindows[_openWindows.Count - 1].OnLostFocus();

            _openWindows.Add(modioPanelBase);
            modioPanelBase.OnGainedFocus(ModioPanelBase.GainedFocusCause.OpeningFromClosed);
        }

        public void ClosePanel(ModioPanelBase modioPanelBase)
        {
            bool hadFocus = false;

            for (var i = _openWindows.Count - 1; i >= 0; i--)
            {
                if (_openWindows[i] == modioPanelBase)
                {
                    if (i == _openWindows.Count - 1) hadFocus = true;
                    _openWindows.RemoveAt(i);
                }
            }

            if (hadFocus)
            {
                modioPanelBase.OnLostFocus();

                if (_openWindows.Count > 0)
                    _openWindows[_openWindows.Count - 1]
                        .OnGainedFocus(ModioPanelBase.GainedFocusCause.RegainingFocusFromStackedPanel);
            }
        }

        /// <summary>
        /// Push a state that will prevent the current panel having focused
        /// Used for text input, so that we don't go navigating away from the input
        /// </summary>
        public void PushFocusSuppression()
        {
            if (_openWindows.Count > 0)
            {
                var panel = _openWindows[_openWindows.Count - 1];
                if (panel.HasFocus) panel.OnLostFocus();
            }
        }

        /// <summary>
        /// Pop any focus suppression from <see cref="PushFocusSuppression"/>
        /// </summary>
        public void PopFocusSuppression(ModioPanelBase.GainedFocusCause gainedFocusCause)
        {
            if (_openWindows.Count > 0)
            {
                var panel = _openWindows[_openWindows.Count - 1];
                if (!panel.HasFocus) panel.OnGainedFocus(gainedFocusCause);
            }
        }

        void LateUpdate()
        {
            if (_openWindows.Count > 0 && _openWindows[_openWindows.Count - 1].HasFocus)
                _openWindows[_openWindows.Count - 1].FocusedPanelLateUpdate();
        }

        public void RegisterPanel(ModioPanelBase modioPanelBase)
        {
            _allPotentialPanels.Add(modioPanelBase);
        }

        public static T GetPanelOfType<T>() where T : ModioPanelBase
        {
            var instance = GetInstance();

            foreach (ModioPanelBase panel in instance._allPotentialPanels)
            {
                if (panel is T typedPanel) return typedPanel;
            }

            return null;
        }
    }
}
