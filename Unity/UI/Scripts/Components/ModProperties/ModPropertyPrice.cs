using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyPrice : ModPropertyNumberBase
    {
        [SerializeField] GameObject _disableIfFree;
        [SerializeField] bool _alsoDisableIfPurchased;
        [SerializeField] GameObject _enableIfPurchased;

        protected override long GetValue(Mod mod)
        {
            if (_disableIfFree != null)
                _disableIfFree.SetActive(mod.IsMonetized && !(_alsoDisableIfPurchased && mod.IsPurchased));

            if (_enableIfPurchased != null) _enableIfPurchased.SetActive(mod.IsPurchased);

            return mod.Price;
        }
    }
}
