using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyDateUpdated : ModPropertyDateBase
    {
        [SerializeField] GameObject _disableIfNoUpdate;

        protected override DateTime GetValue(Mod mod) => mod.DateUpdated;

        public override void OnModUpdate(Mod mod)
        {
            base.OnModUpdate(mod);
            if (_disableIfNoUpdate != null) _disableIfNoUpdate.SetActive(mod.DateUpdated != mod.DateLive);
        }
    }
}
