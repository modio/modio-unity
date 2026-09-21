using Modio.Collections;
using Modio.Mods;
using Modio.Unity.UI.Components;
using Modio.Unity.UI.Input;
using Modio.Unity.UI.Panels.Report;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Panels
{
    public class ModCollectionDisplayPanel : ModioPanelBase
    {
        ModioUICollection _modioUICollection;

        [SerializeField] UnityEvent _onMoreOptionsPressed;

        protected override void Awake()
        {
            base.Awake();
            _modioUICollection = GetComponent<ModioUICollection>();
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            base.OnGainedFocus(selectionBehaviour);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Report,              ReportPressed);
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.MoreFromThisCreator, MoreFromCreatorPressed);

            if (_onMoreOptionsPressed.GetPersistentEventCount() > 0)
            {
                //ModioUIInput.AddHandler(ModioUIInput.ModioAction.MoreOptions, MoreOptionsPressed);
            }
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Report,              ReportPressed);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.MoreOptions,         MoreOptionsPressed);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.MoreFromThisCreator, MoreFromCreatorPressed);
        }

        public void OpenPanel(ModCollection modCollection)
        {
            OpenPanel();

            _modioUICollection.SetCollection(modCollection);
        }

        void ReportPressed()
        {
            ModioPanelManager.GetPanelOfType<ModioReportPanel>().OpenReportFlow(_modioUICollection.Collection);
        }

        void MoreOptionsPressed()
        {
            _onMoreOptionsPressed.Invoke();
        }

        void MoreFromCreatorPressed()
        {
            ModioUISearch.Default.ModioSearch.SetSearchForUser(_modioUICollection.Collection.Creator);
            ClosePanel();
        }
    }
}
