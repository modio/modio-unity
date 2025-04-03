using System;
using Modio.Mods;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyCanAffordPurchase : IModProperty
    {
        [SerializeField] GameObject[] _activateWhenCanAfford;
        [SerializeField] GameObject[] _activateWhenCanNotAfford;

        public void OnModUpdate(Mod mod)
        {
            var currentBalance = User.Current?.Wallet.Balance ?? 0;

            var canAfford = mod.Price <= currentBalance;

            foreach (GameObject gameObject in _activateWhenCanAfford)
            {
                gameObject.SetActive(canAfford);
            }

            foreach (GameObject gameObject in _activateWhenCanNotAfford)
            {
                gameObject.SetActive(!canAfford);
            }
        }
    }
}
