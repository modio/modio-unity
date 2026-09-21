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

            float remainingHorizontal = _containWithin.rect.width - _target.rect.width - _padding.horizontal;
            bool swapHorizontalAndVertical = remainingHorizontal < rectTransform.rect.width;
            
            GetMinMax(_target,        axis, out var targetMin,  out var targetMax);
            GetMinMax(_containWithin, axis, out var containMin, out var containMax);

            var padding = axis == RectTransform.Axis.Horizontal ? _padding.horizontal : _padding.vertical;

            Vector3 pos = rectTransform.position;
            
            float scale = rectTransform.lossyScale.x;

            bool axisIsHorizontal = (axis == RectTransform.Axis.Horizontal);

            if (swapHorizontalAndVertical)
            {
                if (axisIsHorizontal)
                {
                    pos.x = Mathf.Max(
                        targetMin - _padding.top * scale,
                        containMin + (preferredSize + _padding.bottom) * scale
                    );

                    pos.x = (targetMax + targetMin - preferredSize) / 2;
                }
                else
                {
                    bool usePreferredSide = containMin < targetMin - (preferredSize + _padding.bottom) * scale;

                    if (usePreferredSide)
                    {
                        pos.y = targetMin - _padding.bottom * scale;
                    }
                    else
                    {
                        pos.y = targetMax + (_padding.bottom + preferredSize) * scale;
                    }
                }
            }
            else
            {
                if (axisIsHorizontal)
                {
                    bool usePreferredSide = containMax > targetMax + (preferredSize + padding) * scale;

                    if (usePreferredSide)
                    {
                        pos.x = targetMax + (_padding.left * scale);
                    }
                    else
                    {
                        pos.x = targetMin - (_padding.right + preferredSize) * scale;
                    }
                }
                else
                {
                    pos.y = Mathf.Max(
                        targetMin - _padding.top * scale,
                        containMin + (preferredSize + _padding.bottom) * scale
                    );
                }
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
