using System;
using Modio.Collections;
using Modio.Mods;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertyContentMods : ICollectionProperty
    {
        [SerializeField] ModioUISearch _searchMods;
        
        public void OnCollectionUpdate(ModCollection collection)
        {
            _searchMods.ModioSearch.SetSearchForCollectionMods(collection);
        }
    }
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

            if (_searchDependencies != null) _searchDependencies.ModioSearch.SetSearchForDependencies(mod);
        }
    }
}
