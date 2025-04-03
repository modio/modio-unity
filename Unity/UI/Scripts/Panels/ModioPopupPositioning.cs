using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Panels
{
    public class ModioPopupPositioning : MonoBehaviour, ILayoutSelfController
    {
        [SerializeField] RectTransform _containWithin;
        [SerializeField] RectTransform _target;

        [SerializeField] RectOffset _padding = new RectOffset();

        static readonly Vector3[] FourCornersArray = new Vector3[4];

        public void PositionNextTo(RectTransform target)
        {
            _target = target;
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        public void SetLayoutHorizontal()
        {
            SetLayout(RectTransform.Axis.Horizontal);
        }

        public void SetLayoutVertical()
        {
            SetLayout(RectTransform.Axis.Vertical);
        }

        void SetLayout(RectTransform.Axis axis)
        {
            var rectTransform = (RectTransform)transform;

            var preferredSize = LayoutUtility.GetPreferredSize(rectTransform, (int)axis);
            rectTransform.SetSizeWithCurrentAnchors(axis, preferredSize);

            if (_target == null) return;

            GetMinMax(_target,        axis, out var targetMin,  out var targetMax);
            GetMinMax(_containWithin, axis, out var containMin, out var containMax);

            var padding = axis == RectTransform.Axis.Horizontal ? _padding.horizontal : _padding.vertical;

            Vector3 pos = rectTransform.position;

            if (axis == RectTransform.Axis.Horizontal)
            {
                bool usePreferredSide = containMax > targetMax + preferredSize + padding;

                if (usePreferredSide)
                    pos.x = targetMax + _padding.left;
                else
                    pos.x = targetMin - _padding.right - preferredSize;
            }
            else
            {
                pos.y = Mathf.Max(targetMin - _padding.top, containMin + preferredSize + _padding.bottom);
            }

            rectTransform.position = pos;
        }

        void GetMinMax(RectTransform rectTransform, RectTransform.Axis axis, out float min, out float max)
        {
            rectTransform.GetWorldCorners(FourCornersArray);

            min = float.MaxValue;
            max = float.MinValue;

            foreach (Vector3 vector3 in FourCornersArray)
            {
                var current = axis == RectTransform.Axis.Horizontal ? vector3.x : vector3.y;
                min = Mathf.Min(min, current);
                max = Mathf.Max(max, current);
            }
        }
    }
}
