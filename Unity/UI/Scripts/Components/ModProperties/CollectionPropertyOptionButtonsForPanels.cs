using System;
using Modio.Collections;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Report;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertyOptionButtonsForPanels : ICollectionProperty
    {
        [SerializeField] Button _viewCollectionButton;
        [SerializeField] Button _moreFromCreatorButton;
        [SerializeField] Button _reportModButton;
        
        ModCollection _collection;

        public void OnCollectionUpdate(ModCollection collection)
        {
            _collection = collection;

            SetupButton(_viewCollectionButton,  ViewCollectionButtonClicked);
            SetupButton(_moreFromCreatorButton, MoreFromCreatorButtonClicked);
            SetupButton(_reportModButton,       ReportModButtonClicked);

            return;

            void SetupButton(Button button, UnityAction listener)
            {
                if (button == null) return;
                button.onClick.RemoveListener(listener);
                button.onClick.AddListener(listener);
            }
        }
        
        void ViewCollectionButtonClicked()
        {
            ModioPanelManager.GetPanelOfType<ModCollectionDisplayPanel>().OpenPanel(_collection);
        }

        void MoreFromCreatorButtonClicked()
        {
            ModioUISearch.Default.ModioSearch.SetSearchForUser(_collection.Creator);
        }

        void ReportModButtonClicked()
        {
            ModioPanelManager.GetPanelOfType<ModioReportPanel>().OpenReportFlow(_collection);
        }
    }
}
