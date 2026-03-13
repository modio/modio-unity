using UnityEngine;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    public class RectSizeOption : BaseStyleOption<RectTransform>
    {
        [SerializeField]
        Vector2 _offsetMax;
        [SerializeField]
        Vector2 _offsetMin;

        protected override void StyleComponent(RectTransform component)
        {
            component.offsetMax = _offsetMax;
            component.offsetMin = _offsetMin;
        }
    }
}
