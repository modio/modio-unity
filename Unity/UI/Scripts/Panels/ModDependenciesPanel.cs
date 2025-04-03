using System.Threading.Tasks;
using Modio.Mods;
using Modio.Unity.UI.Components;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModDependenciesPanel : ModioPanelBase
    {
        ModioUIMod _modioUIMod;

        [SerializeField] GameObject[] _showForSubscribeFlowOnly;
        [SerializeField] GameObject[] _hideForSubscribeFlow;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();

            IsSubscribeFlow(false);
        }

        public void OpenPanel(ModioUIMod mod)
        {
            OpenPanel(mod.Mod);
        }

        public void OpenPanel(Mod mod)
        {
            OpenPanel();

            _modioUIMod.SetMod(mod);
        }

        public void IsSubscribeFlow(bool isSubscribe)
        {
            foreach (GameObject go in _showForSubscribeFlowOnly)
            {
                go.SetActive(isSubscribe);
            }

            foreach (GameObject go in _hideForSubscribeFlow)
            {
                go.SetActive(!isSubscribe);
            }
        }

        public void ConfirmPressed()
        {
            SubscribeWithDependenciesAndHandleResult();
        }

        async void SubscribeWithDependenciesAndHandleResult()
        {
            Task<Error> subWithDependenciesTask = _modioUIMod.Mod.Subscribe(true);

            var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
            Error error;

            if (waitingPanel != null)
                error = await waitingPanel.OpenAndWaitForAsync(subWithDependenciesTask);
            else
                error = await subWithDependenciesTask;

            ClosePanel();

            if (error) ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
        }
    }
}
