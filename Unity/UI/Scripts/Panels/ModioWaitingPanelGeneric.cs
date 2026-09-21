using System;
using System.Linq;
using System.Threading.Tasks;
using Modio.Unity.UI.Components.Localization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modio.Unity.UI.Panels
{
    [Serializable]
    class GenericWaitingKeys
    {
        public GenericWaitingType WaitingType;
        public string TitleKey;
        public string DescriptionKey;
        
    }

    public class ModioWaitingPanelGeneric : ModioWaitingPanelBase
    {
        [SerializeField] bool _hideDescriptionOnMatchingTitle = true;
        [SerializeField] ShowIconType _showIcon = ShowIconType.WhenDescriptionHidden;
        [SerializeField] Transform _iconContainer;
        [SerializeField] ModioUILocalizedText _title;
        [SerializeField] ModioUILocalizedText _description;

        [SerializeField] GenericWaitingKeys[] _keys;

        
        void SetWaitingType(GenericWaitingType type)
        {
            string titleKey = _keys.FirstOrDefault(x => x.WaitingType == type)?.TitleKey;
            string descriptionKey = _keys.FirstOrDefault(x => x.WaitingType == type)?.DescriptionKey;

            if (!string.IsNullOrEmpty(titleKey) && _title != null) _title.SetKey(titleKey);
            if (!string.IsNullOrEmpty(descriptionKey) && _description != null) _description.SetKey(descriptionKey);

            if (_title == null || _description == null) return;

            // Hide the description if it matches the title and the option is enabled
            bool titleMatchesDescription = string.Equals(
                ModioUILocalizationManager.GetLocalizedText(titleKey),
                ModioUILocalizationManager.GetLocalizedText(descriptionKey),
                StringComparison.InvariantCultureIgnoreCase
            );

            _description.gameObject.SetActive(!_hideDescriptionOnMatchingTitle || !titleMatchesDescription);

            switch (_showIcon)
            {
                case ShowIconType.Always:
                    _iconContainer.gameObject.SetActive(true);
                    break;
                case ShowIconType.WhenDescriptionHidden:
                    _iconContainer.gameObject.SetActive(!_description.gameObject.activeSelf);
                    break;
                case ShowIconType.Never:
                    _iconContainer.gameObject.SetActive(false);
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }
        
        public void OpenPanel(GenericWaitingType type)
        {
            SetWaitingType(type);
            base.OpenPanel();
        }
        
        public void OpenAndWaitFor<T>(Task<T> task, Action<T> action, GenericWaitingType type = GenericWaitingType.Generic)
        {
            SetWaitingType(type);
            base.OpenAndWaitFor(task, action);
        }

        public Task<T> OpenAndWaitForAsync<T>(Task<T> task, GenericWaitingType type = GenericWaitingType.Generic)
        {
            SetWaitingType(type);
            return base.OpenAndWaitForAsync(task);
        }

        public Task OpenAndWaitFor(Task task, Action action = null, GenericWaitingType type = GenericWaitingType.Generic)
        {
            SetWaitingType(type);
            return base.OpenAndWaitFor(task, action);
        }

        enum ShowIconType
        {
            Never,
            WhenDescriptionHidden,
            Always,
        }
    }

    public enum GenericWaitingType
    {
        Generic,
        Purchasing,
        Server,
    }
}
