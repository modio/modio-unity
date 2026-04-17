using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    [RequireComponent(typeof(RectTransform)),DisallowMultipleComponent,]
    public class ModioSizeFromChildLayoutElement : MonoBehaviour, ILayoutElement
    {
        [SerializeField] int _layoutPriority;
        
        [SerializeField] RectTransform _heightFrom;
        [SerializeField] RectTransform _widthFrom;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => _widthFrom != null ?  LayoutUtility.GetMinWidth(_widthFrom) : -1;
        public float preferredWidth => _widthFrom != null ?  LayoutUtility.GetPreferredWidth(_widthFrom) : -1;
        public float flexibleWidth => _widthFrom != null ?  LayoutUtility.GetFlexibleWidth(_widthFrom) : -1;
        public float minHeight => _heightFrom != null ? LayoutUtility.GetMinHeight(_heightFrom) : -1;
        public float preferredHeight => _heightFrom != null ?  LayoutUtility.GetPreferredHeight(_heightFrom) : -1;
        public float flexibleHeight => _heightFrom != null ?  LayoutUtility.GetFlexibleHeight(_heightFrom) : -1;
        public int layoutPriority => _layoutPriority;
    }
}
