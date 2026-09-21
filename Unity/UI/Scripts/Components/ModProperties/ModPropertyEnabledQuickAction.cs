using System;
using System.Threading.Tasks;
using Modio.Mods;
using Modio.Monetization;
using Modio.Unity.Settings;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Monetization;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyEnabledQuickAction : IModProperty, IPropertyMonoBehaviourEvents
    {
        
        [SerializeField] ModioUIButton _selectable;
        Mod _mod;
        bool _hasRegisteredHandler;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;

            UpdateHooks();
        }

        public void Start()
        {
        }

        public void OnDestroy()
        {
        }

        public void OnEnable()
        {
            _selectable.StateChanged += OnStateChanged;
        }

        public void OnDisable()
        {
            _selectable.StateChanged -= OnStateChanged;
            if (_hasRegisteredHandler)
            {
                ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.LibraryEnable, OnPressed);
                ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.LibraryDisable, OnPressed);
                _hasRegisteredHandler = false;
            }
        }

        void OnStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            UpdateHooks();
        }

        void UpdateHooks()
        {
            if (_hasRegisteredHandler) 
            {
                ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.LibraryEnable, OnPressed);
                ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.LibraryDisable, OnPressed);
                _hasRegisteredHandler = false;
            }

            if(_mod == null) return;

            if (_selectable.State is not (IModioUISelectable.SelectionState.Selected
                                          or IModioUISelectable.SelectionState.Pressed)
                || !_selectable.isActiveAndEnabled)
                return;

            if(_mod.File?.State != ModFileState.Installed || !_mod.IsSubscribed)
                return;

            var compUISettings = ModioClient.Settings.GetPlatformSettings<ModioComponentUISettings>();

            if (compUISettings is not { ShowEnableModToggle: true, })
            {
                return;
            }

            _hasRegisteredHandler = true;

            ModioUIInput.AddHandler(
                _mod.IsEnabled ? ModioUIInput.ModioAction.LibraryDisable : ModioUIInput.ModioAction.LibraryEnable,
                OnPressed
            );
        }

        void OnPressed()
        {
            _mod.SetIsEnabled(!_mod.IsEnabled);
        }
    }
}
