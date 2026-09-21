using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes
{
    [Serializable]
    public abstract class BaseStyleOption<T> : IStyleComponent<T> where T : Object
    {
        public ThemeOptions OptionType => _option;

        [SerializeField] ThemeOptions _option;

        public void TryStyleComponent(Object component)
        {
            if (component is T superType) StyleComponent(superType);
        }

        public abstract void StyleComponent(T component);
    }
}
