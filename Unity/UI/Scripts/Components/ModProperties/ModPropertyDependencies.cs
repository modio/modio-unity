using System;
using Modio.Mods;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDependencies : IModProperty
    {
        [SerializeField] GameObject _disableIfNoDependencies;
        [SerializeField] TMP_Text _dependenciesCount;

        [SerializeField] ModioUISearch _searchDependencies;

        public void OnModUpdate(Mod mod)
        {
            if (_disableIfNoDependencies != null) _disableIfNoDependencies.SetActive(mod.Dependencies.HasDependencies);
            if (_dependenciesCount != null) _dependenciesCount.text = mod.Dependencies.Count.ToString();

            if (_searchDependencies != null) _searchDependencies.SetSearchForDependencies(mod);
        }
    }
}
