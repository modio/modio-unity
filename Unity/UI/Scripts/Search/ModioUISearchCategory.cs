using System.Collections.Generic;
using UnityEngine;

namespace Modio.Unity.UI.Search
{
    /// <summary>
    /// A broad search category, that has tabs within it
    /// Mostly used for the Library category. The individual SearchSettings are
    /// shown as radio buttons in the Filter section
    /// </summary>
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
