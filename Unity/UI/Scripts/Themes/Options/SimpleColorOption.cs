using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    public class SimpleColorOption : IStyleOption
    {
        public ThemeOptions OptionType => _option;

        [SerializeField] Color _color;
        [SerializeField] ThemeOptions _option;

        public void TryStyleComponent(Object component)
        {
            if (component is Image image) image.color = _color;
            else if (component is TMP_Text text) text.color = _color;
        }
    }
}
