using System.Collections.Generic;
using UnityEngine;

namespace Modio.Unity.UI.Search
{
    public class ModioUISearchCategory : MonoBehaviour
    {
        [SerializeField] string _categoryLabel;
        [SerializeField] string _categoryLabelLocalized;
        [SerializeField] List<ModioUISearchSettings> _tabs;
        [SerializeField] ModioUISearchSettings _customSearchBase;

        public string CategoryLabel => _categoryLabel;
        public string CategoryLabelLocalized => _categoryLabelLocalized;
        public IEnumerable<ModioUISearchSettings> Tabs => _tabs;
        public ModioUISearchSettings CustomSearchBase => _customSearchBase;
    }
}
