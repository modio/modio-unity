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
        ModioUIUser _modioUIUser;

        [SerializeField] UnityEvent _onMoreOptionsPressed;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();
            _modioUIUser = GetComponent<ModioUIUser>();
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
            _modioUIUser?.SetUser(mod.Creator);
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
            ModioUISearch.Default.ModioSearch.SetSearchForUser(_modioUIMod.Mod.Creator);
            ClosePanel();
        }
    }
}
