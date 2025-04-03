using Modio.Mods;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUITag : MonoBehaviour
    {
        [SerializeField] TMP_Text _label;
        ModTag _tag;

        public virtual void Set(ModTag tag)
        {
            _tag = tag;
            if (_label != null) _label.text = tag.NameLocalized;
        }

        public void TagSelectedForSearch()
        {
            ModioUISearch.Default.SetSearchForTag(_tag);
        }
    }
}
