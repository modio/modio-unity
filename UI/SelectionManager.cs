using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ModIOBrowser;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

class SelectionManager : MonoBehaviour
{ 
    public static SelectionManager Instance;

    public UiViews startView;
    public UiViews currentView;

    private Dictionary<UiViews, List<GameObject>> selectionHistory = new Dictionary<UiViews, List<GameObject>>();
    private Dictionary<UiViews, GameObject> viewConfig;

    public List<SelectionViewConfigItem> defaultViews = new List<SelectionViewConfigItem>();

    private void Awake()
    {
        Instance = this;

        if(defaultViews.Any(x => x.viewType == UiViews.Nothing))
        {
            string error = $"Unable to set up a default view with the UiViews type {UiViews.Nothing}.";
            Debug.LogError(error);
            throw new UnityException(error);
        }

        viewConfig = defaultViews.ToDictionary(x => x.viewType, x => x.defaultSelectedObject);
        SelectView(startView);
    }

    public void Update()
    {        
        if(EventSystem.current.currentSelectedGameObject != null)
        {
            if(selectionHistory[currentView].LastOrDefault() != EventSystem.current.currentSelectedGameObject)
            {
                selectionHistory[currentView].Add(EventSystem.current.currentSelectedGameObject);
            }
        }
        else
        {
            Browser.SelectGameObject(selectionHistory[currentView].Last());
        }
    }

    public void SelectMostRecentStillActivatedUiItem(bool force = false)
    {
        if(EventSystem.current.currentSelectedGameObject == null || force)
        {
            GameObject item = selectionHistory[currentView].LastOrDefault(x => x.activeSelf);
            item = item == null ? viewConfig[currentView] : item;            
            selectionHistory[currentView].Clear();
            selectionHistory[currentView].Add(item);

            Browser.SelectGameObject(item);
        }
    }

    private void ForceSelectMostRecentStillActivatedUiItem()
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

    public void SelectView(UiViews view)
    {
        if(view == UiViews.Nothing)
        {
            throw new UnityException("No views with the type 'Error' allowed.");
        }
        else if(!defaultViews.Any(x => x.viewType == view))
        {
            throw new UnityException($"There is no configuration for the view {view}.");
        }

        SelectionViewConfigItem viewConfigItem = GetViewConfigItem(view);

        currentView = viewConfigItem.viewType;

        LazyInstantiateHistory(currentView);

        bool revertToDefaultSelection = (view != UiViews.Browse || selectionHistory[currentView].Count() == 0);
        if(revertToDefaultSelection)
        {
            GameObject defaultObject = viewConfig[currentView];
            Browser.SelectGameObject(defaultObject);

            selectionHistory[currentView].Clear();
            selectionHistory[currentView].Add(defaultObject);
        }
        else
        {
            ForceSelectMostRecentStillActivatedUiItem();
        }
    }

    private void LazyInstantiateHistory(UiViews view)
    {
        if(!selectionHistory.ContainsKey(view))
        {
            selectionHistory.Add(view, new List<GameObject>());
        }
    }

    private SelectionViewConfigItem GetViewConfigItem(UiViews view)
    {
        SelectionViewConfigItem viewConfigItem = defaultViews.FirstOrDefault(x => x.viewType == view);
        if(viewConfigItem == null)
        {
            throw new NotImplementedException($"The configuration for the view '{view}' does not exist.");
        }

        return viewConfigItem;
    }
}

