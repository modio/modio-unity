using System;
using System.Collections.Generic;
using System.Linq;
using ModIOBrowser;
using ModIOBrowser.Implementation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIOBrowser
{
    class SelectionManager : SimpleMonoSingleton<SelectionManager>
    {
        public UiViews currentView { get; private set; } = UiViews.Browse;
        private UiViews previousView { get; set; } = UiViews.Nothing;

        private Dictionary<UiViews, List<GameObject>> selectionHistory = new Dictionary<UiViews, List<GameObject>>();
        private Dictionary<UiViews, GameObject> viewConfig;

        public List<SelectionViewConfigItem> defaultViews = new List<SelectionViewConfigItem>();

        protected override void Awake()
        {
            base.Awake();

            gameObject.SetActive(false);

            if(defaultViews.Any(x => x.viewType == UiViews.Nothing))
            {
                string error = $"Unable to set up a default view with the UiViews type {UiViews.Nothing}.";
                Debug.LogError(error);
                throw new UnityException(error);
            }

            viewConfig = defaultViews.ToDictionary(x => x.viewType, x => x.defaultSelectedObject);
        }

        public void Update()
        {
            if(!Browser.Instance.BrowserCanvas.activeSelf)
            {
                return;
            }

            if(currentView == UiViews.Nothing)
            {
                return;
            }

            if(EventSystem.current.currentSelectedGameObject != null)
            {
                if(CurrentViewHistory().LastOrDefault() != EventSystem.current.currentSelectedGameObject)
                {
                    CurrentViewHistory().Add(EventSystem.current.currentSelectedGameObject);
                }
            }
            else
            {
                Browser.SelectGameObject(CurrentViewHistory().Last());
            }
        }

        List<GameObject> CurrentViewHistory()
        {
            if(selectionHistory[currentView] == null)
            {
                return LazyInstantiateHistory(currentView);
            }

            return selectionHistory[currentView];
        }

        public void SelectMostRecentStillActivatedUiItem(bool force = false)
        {
            if(EventSystem.current.currentSelectedGameObject == null || force)
            {
                GameObject item = CurrentViewHistory().LastOrDefault(x => x.activeSelf);
                item = item == null ? viewConfig[currentView] : item;
                CurrentViewHistory().Clear();
                CurrentViewHistory().Add(item);

                Browser.SelectGameObject(item);
            }
        }

        void ForceSelectMostRecentStillActivatedUiItem()
        {
            SelectMostRecentStillActivatedUiItem(true);
        }

        public void SetNewViewDefaultSelection(UiViews view, Selectable selectable)
        {
            SelectionViewConfigItem v = GetViewConfigItem(view);
            v.defaultSelectedObject = selectable.gameObject;
            viewConfig[view] = selectable.gameObject;
            LazyInstantiateHistory(view);
            selectionHistory[view].Clear();
        }

        public void SelectPreviousView()
        {
            SelectView(previousView);
        }

        public void SelectView(UiViews view)
        {
            if(view == UiViews.Nothing)
            {
                throw new UnityException($"No views with the type '{UiViews.Nothing}' allowed.");
            }
            if(!defaultViews.Any(x => x.viewType == view))
            {
                throw new UnityException($"There is no configuration for the view {view}.");
            }

            SelectionViewConfigItem viewConfigItem = GetViewConfigItem(view);

            previousView = currentView;
            currentView = viewConfigItem.viewType;

            LazyInstantiateHistory(currentView);

            bool revertToDefaultSelection = (view != UiViews.Browse || CurrentViewHistory().Count() == 0);
            if(revertToDefaultSelection)
            {
                GameObject defaultObject = viewConfig[currentView];
                Browser.SelectGameObject(defaultObject);

                CurrentViewHistory().Clear();
                CurrentViewHistory().Add(defaultObject);
            }
            else
            {
                ForceSelectMostRecentStillActivatedUiItem();
            }
        }

        List<GameObject> LazyInstantiateHistory(UiViews view)
        {
            if(!selectionHistory.ContainsKey(view))
            {
                var list = new List<GameObject>();
                selectionHistory.Add(view, list);
                return list;
            }

            return selectionHistory[view];
        }

        SelectionViewConfigItem GetViewConfigItem(UiViews view)
        {
            SelectionViewConfigItem viewConfigItem = defaultViews.FirstOrDefault(x => x.viewType == view);
            if(viewConfigItem == null)
            {
                throw new NotImplementedException($"The configuration for the view '{view}' does not exist.");
            }

            return viewConfigItem;
        }
    }

}
