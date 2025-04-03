using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    /// <summary>
    /// A layout group similar to Grid that allows staggered rows
    ///
    /// Note that this isn't currently fully featured and not all settings will work properly.
    /// If you need a more robust FlexGrid, consider looking at plugins online
    /// </summary>
    public class ModioFlexGrid : LayoutGroup
    {
        [SerializeField] protected float m_Spacing = 0;

        [SerializeField] protected bool m_ChildForceExpandWidth = false;
        [SerializeField] protected bool m_ChildForceExpandHeight = false;

        [SerializeField] protected bool m_ChildControlWidth = true;
        [SerializeField] protected bool m_ChildControlHeight = true;

        [SerializeField] protected bool m_ChildScaleWidth = false;
        [SerializeField] protected bool m_ChildScaleHeight = false;

        [SerializeField] protected bool m_ReverseArrangement = false;

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, false);

            SetLayoutInputForAxis(0, 0, 1, 0);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            SetLayoutInputForAxis(0, 100, 0, 1);
            CalcAlongAxis(1, false);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1);
        }

        /// <summary>
        /// Calculate the layout element properties for this layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        void CalcAlongAxis(int axis, bool isVertical)
        {
            float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);

            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            var rectChildrenCount = rectChildren.Count;

            float totalMinWidthForRow = 0;
            float totalPreferredWidthForRow = 0;
            float totalFlexibleWidthForRow = 0;

            //Currently unused, but would likely be needed if being more accurate
            float totalMinHeightForRow = 0;
            float totalPreferredHeightForRow = 0;
            float totalFlexibleHeightForRow = 0;

            float maxWidthForRow = rectTransform.rect.width - padding.horizontal;

            for (int i = 0; i < rectChildrenCount; i++)
            {
                RectTransform child = rectChildren[i];

                GetChildSizes(
                    child,
                    0,
                    m_ChildControlWidth,
                    m_ChildForceExpandWidth,
                    out var minW,
                    out var preferredW,
                    out var flexibleW
                );

                GetChildSizes(
                    child,
                    1,
                    m_ChildControlHeight,
                    m_ChildForceExpandHeight,
                    out var minH,
                    out var preferredH,
                    out var flexibleH
                );

                var min = axis == 0 ? minW : minH;
                var preferred = axis == 0 ? preferredW : preferredH;
                var flexible = axis == 0 ? flexibleW : flexibleH;

                if (useScale)
                {
                    float scaleFactor = child.localScale[axis];
                    min *= scaleFactor;
                    preferred *= scaleFactor;
                    flexible *= scaleFactor;
                }

                totalMinWidthForRow += minW;
                totalPreferredWidthForRow += preferredW;
                totalFlexibleWidthForRow += flexibleW;

                totalMinHeightForRow = Mathf.Max(minH,             totalMinHeightForRow);
                totalPreferredHeightForRow = Mathf.Max(preferredH, totalPreferredHeightForRow);
                totalFlexibleHeightForRow = Mathf.Max(flexibleH,   totalFlexibleHeightForRow);

                if (totalPreferredWidthForRow > maxWidthForRow)
                {
                    if (axis == 1) // vertical; Add last row's spacing
                    {
                        totalMin += totalMinHeightForRow + m_Spacing;
                        totalPreferred += totalPreferredHeightForRow + m_Spacing;
                        totalFlexible += totalFlexibleHeightForRow;
                    }

                    totalMinWidthForRow = minW;
                    totalPreferredWidthForRow = preferredW;
                    totalFlexibleWidthForRow = flexibleW;
                }

                if (axis == 0) //horizontal
                {
                    totalMin = Mathf.Max(totalMinWidthForRow + combinedPadding,             totalMin);
                    totalPreferred = Mathf.Max(totalPreferredWidthForRow + combinedPadding, totalPreferred);
                    totalFlexible = Mathf.Max(totalFlexibleWidthForRow,                     totalFlexible);
                }

                if (alongOtherAxis)
                {
                    totalMin = Mathf.Max(min + combinedPadding,             totalMin);
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                    totalFlexible = Mathf.Max(flexible,                     totalFlexible);
                }
                else
                {
                    totalMin += min + m_Spacing;
                    totalPreferred += preferred + m_Spacing;

                    // Increment flexible size with element's flexible size.
                    totalFlexible += flexible;
                }
            }

            if (!alongOtherAxis && rectChildren.Count > 0)
            {
                totalMin -= m_Spacing;
                totalPreferred -= m_Spacing;
            }

            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        /// <summary>
        /// Set the positions and sizes of the child layout elements for the given axis.
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
        void SetChildrenAlongAxis(int axis)
        {
            float size = rectTransform.rect.size[axis];

            int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
            int endIndex = m_ReverseArrangement ? 0 : rectChildren.Count;
            int increment = m_ReverseArrangement ? -1 : 1;

            float totalPreferredWidthForRow = 0;

            float maxWidthForRow = rectTransform.rect.width - padding.horizontal;

            float pos = (axis == 0 ? padding.left : padding.top);
            float surplusSpace = size - GetTotalPreferredSize(axis);

            if (surplusSpace > 0)
            {
                if (GetTotalFlexibleSize(axis) == 0)
                    pos = GetStartOffset(
                        axis,
                        GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical)
                    );
            }

            for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
            {
                RectTransform child = rectChildren[i];

                var preferredW = m_ChildControlWidth ? LayoutUtility.GetPreferredSize(child,  0) : child.sizeDelta.x;
                var preferredH = m_ChildControlHeight ? LayoutUtility.GetPreferredSize(child, 1) : child.sizeDelta.y;

                totalPreferredWidthForRow += preferredW;

                if (totalPreferredWidthForRow > maxWidthForRow) //see if we've exceeded this rows capacity
                {
                    if (axis == 1) // vertical; Add last row's spacing
                    {
                        pos += preferredH + m_Spacing;
                    }
                    else
                    {
                        pos = padding.left;
                    }

                    totalPreferredWidthForRow = preferredW;
                }

                if (axis == 1) // vertical
                {
                    SetChildAlongAxisWithScale(child, axis, pos, preferredH, 1);
                }
                else
                {
                    SetChildAlongAxisWithScale(child, axis, pos, preferredW, 1);

                    pos += preferredW + m_Spacing;
                }
            }
        }

        void GetChildSizes(
            RectTransform child,
            int axis,
            bool controlSize,
            bool childForceExpand,
            out float min,
            out float preferred,
            out float flexible
        )
        {
            if (!controlSize)
            {
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0;
            }
            else
            {
                min = LayoutUtility.GetMinSize(child, axis);
                preferred = LayoutUtility.GetPreferredSize(child, axis);
                flexible = LayoutUtility.GetFlexibleSize(child, axis);
            }

            if (childForceExpand) flexible = Mathf.Max(flexible, 1);
        }
    }
}
