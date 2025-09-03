using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes
{
    [Serializable]
    public abstract class BaseStyleOption<T> : IStyleOption
    {
        public ThemeOptions OptionType => _option;

        [SerializeField] ThemeOptions _option;

        public void TryStyleComponent(Object component)
        {
            if (component is T superType) StyleComponent(superType);
        }

        protected abstract void StyleComponent(T component);
    }
}
