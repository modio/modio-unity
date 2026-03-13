using System;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    public class ModioAspectRatioLayout : MonoBehaviour, ILayoutElement
    {
        [SerializeField] bool _basedOnHeight = true;
        [SerializeField] float _aspectRatio = 1920f / 1080f;

        public void CopySettingsFrom(ModioAspectRatioLayout other)
        {
            _basedOnHeight = other._basedOnHeight;
            _aspectRatio = other._aspectRatio;
        }
        
        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => -1;
        public float preferredWidth => GetSize(true);
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => GetSize(false);
        public float flexibleHeight => -1;
        public int layoutPriority => 10;

        float GetSize(bool horizontal)
        {
            if(horizontal != _basedOnHeight)
                return -1;

            var rectTransform = (RectTransform)transform.parent;

            if (horizontal) return rectTransform.rect.height * _aspectRatio;
            return rectTransform.rect.width / _aspectRatio;
        }
    }
}
