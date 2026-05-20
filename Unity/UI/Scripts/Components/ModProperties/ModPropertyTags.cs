using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyTags : ModioResourceProperty
    {
        [SerializeField] ModioUITag _tagTemplate;
        [SerializeField] GameObject _noTagsActive;
        [SerializeField] GameObject _tagsActive;

        readonly List<ModioUITag> _tags = new List<ModioUITag>();

        protected override void OnResourceUpdate(IModioInfo resource)
        {
            if (!_tags.Any())
            {
                if (_tagTemplate == null)
                {
                    bool anyTagsActive = resource.Tags.Any((tag) => tag.IsVisible);
                    
                    if (_noTagsActive != null) _noTagsActive.SetActive(!anyTagsActive);
                    if (_tagsActive != null) _tagsActive.SetActive(anyTagsActive);
                    
                    return;
                }

                _tags.Add(_tagTemplate);
            }

            var visibleIndex = 0;
            
            foreach (ModTag modTag in resource.Tags)
            {
                if(!modTag.IsVisible) continue;
                
                if (visibleIndex >= _tags.Count)
                    _tags.Add(Object.Instantiate(_tags[0], _tags[0].transform.parent));
                
                ModioUITag tag = _tags[visibleIndex];
                tag.gameObject.SetActive(true);
                tag.Set(modTag);

                visibleIndex++;
            }

            for (int i = visibleIndex; i < _tags.Count; i++) 
                _tags[i].gameObject.SetActive(false);

            if (_noTagsActive != null) _noTagsActive.SetActive(visibleIndex == 0);
            if (_tagsActive != null) _tagsActive.SetActive(visibleIndex != 0);
        }
    }
}
