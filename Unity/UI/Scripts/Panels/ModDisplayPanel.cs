using Modio.Mods;
using Modio.Unity.UI.Components;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels.Report;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Panels
{
    public class ModDisplayPanel : ModioPanelBase
    {
        ModioUIMod _modioUIMod;

        [SerializeField] UnityEvent _onMoreOptionsPressed;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            base.OnGainedFocus(selectionBehaviour);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Report,              ReportPressed);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.MoreFromThisCreator, MoreFromCreatorPressed);

            if (_onMoreOptionsPressed.GetPersistentEventCount() > 0)
            {
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.MoreOptions, MoreOptionsPressed);
            }
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Report,              ReportPressed);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.MoreOptions,         MoreOptionsPressed);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.MoreFromThisCreator, MoreFromCreatorPressed);
        }

        public void OpenPanel(Mod mod)
        {
            OpenPanel();

            _modioUIMod.SetMod(mod);
        }

        void ReportPressed()
        {
            ModioPanelManager.GetPanelOfType<ModioReportPanel>().OpenReportFlow(_modioUIMod.Mod);
        }

        void MoreOptionsPressed()
        {
            _onMoreOptionsPressed.Invoke();
        }

        void MoreFromCreatorPressed()
        {
            ModioUISearch.Default.SetSearchForUser(_modioUIMod.Mod.Creator);
            ClosePanel();
        }
    }
}
