using System;
using System.Threading.Tasks;
using Modio.Collections;
using Modio.Mods;
using Modio.Monetization;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Monetization;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertySubscriptionQuickAction : ICollectionProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] ModioUIButton _selectable;
        [SerializeField] bool _showPopupToSubscribe = true;
        
        ModCollection _collection;
        bool _hasRegisteredHandler;

        public void OnCollectionUpdate(ModCollection collection)
        {
            _collection = collection;

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
            if (_hasRegisteredHandler) ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.ContextualFastAction, OnPressed);
            _hasRegisteredHandler = false;
        }

        void OnStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            UpdateHooks();
        }

        void UpdateHooks()
        {
            if (_hasRegisteredHandler) ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.ContextualFastAction, OnPressed);
            _hasRegisteredHandler = false;

            if(_collection == null) return;

            if (_selectable.State is IModioUISelectable.SelectionState.Selected
                                     or IModioUISelectable.SelectionState.Pressed
                && _selectable.isActiveAndEnabled)
            {
                if (_collection.IsFollowed)
                {
                    AddHandler("modio_btn_unfollow");
                }
                else
                    AddHandler("modio_btn_follow");
            }

            void AddHandler(string localizedKey)
            {
                _hasRegisteredHandler = true;
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.ContextualFastAction, OnPressed, localizedKey);
            }
        }

        void OnPressed()
        {
            if (!_collection.IsFollowed)
            {
                if (_showPopupToSubscribe) 
                    ModioPanelManager.GetPanelOfType<ModCollectionSubscribePanel>()?.OpenPanel(_collection);
                
                var task = _collection.Follow();

                ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
            }
            else
            {
                ModioPanelManager.GetPanelOfType<ModCollectionUnsubscribePanel>()?.OpenPanel(_collection);
            }
        }
    }
}
