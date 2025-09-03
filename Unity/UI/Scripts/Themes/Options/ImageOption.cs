using System;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class ImageOption : BaseStyleOption<Image>
    {
        [SerializeField] Sprite _image;

        protected override void StyleComponent(Image component)
        {
            component.sprite = _image;
        }
    }
}
