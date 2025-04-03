using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    /// <summary>
    /// Resizes a RectTransform to fit a specified aspect ratio within a parent.
    /// Has additional settings for margin (space to parent), max size,
    /// and padding (size within this rect that doesn't count towards the aspect ratio)
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class ModioAspectRatioFitter : MonoBehaviour, ILayoutSelfController
    {
        [SerializeField] float _aspectRatio = 16f / 9f;

        [SerializeField] RectOffset _margin;

        [SerializeField] Vector2 _additionalPadding;
        [SerializeField] Vector2 _maxSize;

        DrivenRectTransformTracker _tracker;
        bool _delayedSetDirty;

        void OnEnable()
        {
            UpdateRect();
        }

        /// <summary>
        /// Function called when this RectTransform or parent RectTransform has changed dimensions.
        /// </summary>
        void OnRectTransformDimensionsChange()
        {
            UpdateRect();
        }

        /// <summary>
        /// Update the rect based on the delayed dirty.
        /// Got around issue of calling onValidate from OnEnable function.
        /// </summary>
        void Update()
        {
            if (_delayedSetDirty)
            {
                _delayedSetDirty = false;
                UpdateRect();
            }
        }

        void OnValidate()
        {
            _delayedSetDirty = true;
        }

        void UpdateRect()
        {
            _tracker.Clear();

            var rectTransform = (RectTransform)transform;

            var parent = (RectTransform)rectTransform.parent;
            Vector2 parentSize = parent.rect.size;

            var availableSize = parentSize - new Vector2(_margin.horizontal, _margin.vertical);

            if (_maxSize.x > 1) availableSize.x = Mathf.Min(availableSize.x, _maxSize.x);
            if (_maxSize.y > 1) availableSize.y = Mathf.Min(availableSize.y, _maxSize.y);

            var sizeWithoutPadding = availableSize - _additionalPadding;

            if (sizeWithoutPadding.y * _aspectRatio < sizeWithoutPadding.x)
            {
                sizeWithoutPadding.x = sizeWithoutPadding.y * _aspectRatio;
            }
            else
            {
                sizeWithoutPadding.y = sizeWithoutPadding.x / _aspectRatio;
            }

            availableSize = sizeWithoutPadding + _additionalPadding;

            _tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDelta);

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, availableSize.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   availableSize.y);
        }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public void SetLayoutHorizontal() { }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public void SetLayoutVertical() { }
    }
}
