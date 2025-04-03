using System;
using Modio.Mods;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyCreator : IModProperty
    {
        [SerializeField] ModioUIUser _user;

        public void OnModUpdate(Mod mod) => _user.SetUser(mod.Creator);
    }
}
