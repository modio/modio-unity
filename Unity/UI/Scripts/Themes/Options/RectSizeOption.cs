using System;
using UnityEngine;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class RectSizeOption : BaseStyleOption<RectTransform>
    {
        [SerializeField]
        Vector2 _offsetMax;
        [SerializeField]
        Vector2 _offsetMin;

        public override void StyleComponent(RectTransform component)
        {
            component.offsetMax = _offsetMax;
            component.offsetMin = _offsetMin;
        }
    }
}
