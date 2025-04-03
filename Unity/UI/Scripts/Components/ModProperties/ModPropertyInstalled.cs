using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyInstalled : IModProperty
    {
        [SerializeField] GameObject _notInstalledActive;
        [SerializeField] GameObject _installedActive;

        public void OnModUpdate(Mod mod)
        {
            if (_notInstalledActive != null)
                _notInstalledActive.SetActive(mod.File.State != ModFileState.Installed);

            if (_installedActive != null) _installedActive.SetActive(mod.File.State == ModFileState.Installed);
        }
    }
}
