using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    /// <summary>
    /// Place this on another LayoutElement to restrict its maximum preferred width
    /// Note that this won't impact minimum width
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class ModioMaxSizeFitter : MonoBehaviour, ILayoutElement
    {
        [SerializeField, Tooltip("Leaving an axis at 0 will ignore it")]
        Vector2 _maxSize;
        [SerializeField] int _layoutPriority = 10;

        bool _calculatingNestedSize;

        float GetPreferredSize(RectTransform.Axis axis)
        {
            float maxSize = _maxSize[(int)axis];

            if(maxSize < 0.01f || layoutPriority < 0) return -1;

            var rectTransform = (RectTransform)transform;

            int prevPriority = layoutPriority;
            _calculatingNestedSize = true;
            var preferredSize = LayoutUtility.GetPreferredSize(rectTransform, (int)axis);
            _calculatingNestedSize = false;

            preferredSize = Mathf.Min(preferredSize, maxSize);
            return preferredSize;
        }

        public void CalculateLayoutInputHorizontal()
        {
        }
        public void CalculateLayoutInputVertical()
        {
        }
        public float minWidth => -1;
        public float preferredWidth  => GetPreferredSize(RectTransform.Axis.Horizontal);
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => GetPreferredSize(RectTransform.Axis.Vertical);
        public float flexibleHeight => -1;
        public int layoutPriority => _calculatingNestedSize ? -1 : _layoutPriority;
    }
}
