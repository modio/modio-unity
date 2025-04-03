using Modio.Extensions;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Navigation;
using Modio.Unity.UI.Panels.Authentication;
using Modio.Unity.UI.Search;
using Modio.Users;
using UnityEngine;
using UnityEngine.Events;
using Task = System.Threading.Tasks.Task;

namespace Modio.Unity.UI.Panels
{
    public class ModBrowserPanel : ModioPanelBase
    {
        [SerializeField] ModioInputFieldSelectionWrapper _searchField;

        [SerializeField] UnityEvent _openingPanelFromClosed;

        static bool _isWaitingBeforeAuthFlow;

        protected override void Start()
        {
            base.Start();

            if (!ModioUILocalizationManager.LocalizationExists)
            {
                //Can't use Logger before the plugin is setup
                Debug.LogWarning($"Your scene doesn't appear to have a ModioUILocalizationManager or custom localization handler. Consider adding the 'ModioUI_Localisation' prefab to your scene");
            }
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Search, OpenSearch);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Filter, OpenFilter);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Sort,   OpenSort);

            ModioUISearch.Default.OnSearchUpdatedUnityEvent.AddListener(HookUpCancelOrClearFilter);

            base.OnGainedFocus(selectionBehaviour);

            //must happen after base.OnGainedFocus to unhook the regular Cancel listener
            HookUpCancelOrClearFilter();
            
            if (!ModioClient.IsInitialized || User.Current == null || !User.Current.IsAuthenticated)
            {
                if (_isWaitingBeforeAuthFlow)
                    return;

                //we've regained focus from a panel that was pushed on top. Close instead
                if (selectionBehaviour == GainedFocusCause.RegainingFocusFromStackedPanel)
                {
                    ModioLog.Message?.Log($"Closing {nameof(ModBrowserPanel)} after regaining focus from cancelled login attempt");
                    ClosePanel();

                    return;
                }

                OpenAuthFlowAfterWaitingIfNeeded().ForgetTaskSafely();
            }

            if (selectionBehaviour == GainedFocusCause.OpeningFromClosed) _openingPanelFromClosed.Invoke();
        }

        async Task OpenAuthFlowAfterWaitingIfNeeded()
        {
            ModioWaitingPanelGeneric waitingPanel = null;
            
            if (!ModioClient.IsInitialized)
            {
                waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
                waitingPanel?.OpenPanel();
                
                ModioLog.Warning?.Log(($"Attempting to open {nameof(ModBrowserPanel)} before initializing the plugin and AutoInitialize is disabled"));
                
                Error error = await ModioClient.Init();

                if (error)
                {
                    ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
                    waitingPanel?.ClosePanel();
                    return;
                }
            }

            //If we're currently fetching the user, wait for that to complete
            if (User.Current != null)
            {
                if (waitingPanel == null)
                {
                    waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
                    waitingPanel?.OpenPanel();    
                }
                
                while (User.Current.IsUpdating)
                {
                    await Task.Yield();
                }

                _isWaitingBeforeAuthFlow = false;
                //successfully updated user
                if (User.Current.IsAuthenticated)
                {
                    waitingPanel?.ClosePanel();
                    return;
                }
                
                //plugin shutdown
                if (!ModioClient.IsInitialized)
                {
                    waitingPanel?.ClosePanel();
                    return;
                }
            }
            _isWaitingBeforeAuthFlow = false;

            // We leave the waiting panel open as the Auth flow will always reliably close it
            ModioPanelManager.GetPanelOfType<ModioAuthenticationPanel>().OpenAuthFlow();
        }

        public override void OnLostFocus()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Search,      OpenSearch);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Filter,      OpenFilter);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Sort,        OpenSort);

            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.SearchClear, ClearSearch);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Cancel,      CancelPressed);

            if (ModioUISearch.Default != null)
                ModioUISearch.Default.OnSearchUpdatedUnityEvent.RemoveListener(HookUpCancelOrClearFilter);

            base.OnLostFocus();
        }

        void HookUpCancelOrClearFilter()
        {
            if (!HasFocus) return;
            
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Cancel,      CancelPressed);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.SearchClear, ClearSearch);

            if (ModioUISearch.Default.HasCustomSearch())
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.SearchClear, ClearSearch);
            else
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.Cancel, CancelPressed);
        }

        void OpenSearch()
        {
            if (_searchField != null) _searchField.SelectInputField();
        }

        void ClearSearch()
        {
            ModioUISearch.Default.ClearSearch();
        }

        void OpenFilter()
        {
            ModioPanelManager.GetPanelOfType<ModFilterPanel>()?.OpenPanel();
        }

        void OpenSort()
        {
            ModioPanelManager.GetPanelOfType<ModSortPanel>()?.OpenPanel();
        }
    }
}
