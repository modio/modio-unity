using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    public class ModioUIMaximumHeight : MonoBehaviour, ILayoutElement
    {
        [SerializeField] float _restrictHeightTo = 100f;
        [SerializeField] Toggle _expandAnyway;
        [SerializeField] GameObject _showWhenRestrictingHeight;

        bool _isRestrictingHeight;
        Graphic _graphic;

        public float minWidth => -1;
        public float preferredWidth => -1;
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => _isRestrictingHeight ? _restrictHeightTo : -1;
        public float flexibleHeight => -1;
        public int layoutPriority => 10;

        void Awake()
        {
            if (_expandAnyway != null) _expandAnyway.onValueChanged.AddListener(OnExpandAnywayChanged);

            _graphic = GetComponent<Graphic>();
            if (_graphic != null) _graphic.RegisterDirtyLayoutCallback(GraphicLayoutDirty);
        }

        void OnDestroy()
        {
            if (_graphic != null) _graphic.UnregisterDirtyLayoutCallback(GraphicLayoutDirty);
        }

        void OnEnable()
        {
            if (_expandAnyway != null) _expandAnyway.isOn = false;
        }

        void OnExpandAnywayChanged(bool isExpanded)
        {
            SetDirty();
        }

        public void CalculateLayoutInputHorizontal() { }

        public void CalculateLayoutInputVertical()
        {
            RecalculateRestrictingHeight(true);
        }

        void GraphicLayoutDirty()
        {
            RecalculateRestrictingHeight(false);
        }

        void RecalculateRestrictingHeight(bool delayButtonActivation)
        {
            _isRestrictingHeight = false;
            _isRestrictingHeight = LayoutUtility.GetPreferredHeight((RectTransform)transform) > _restrictHeightTo;

            if (_expandAnyway != null || _showWhenRestrictingHeight != null)
            {
                if (delayButtonActivation)
                {
                    //we can't set this immediately, as we're in a GUI rebuild loop already (it will throw errors)
                    StartCoroutine(SetButtonsActiveDelayed(_isRestrictingHeight));
                }
                else
                {
                    SetButtonsActive(_isRestrictingHeight);
                }

                if (_expandAnyway != null && _expandAnyway.isOn)
                {
                    _isRestrictingHeight = false;
                }
            }
        }

        IEnumerator SetButtonsActiveDelayed(bool shouldBeVisible)
        {
            yield return new WaitForEndOfFrame();
            SetButtonsActive(shouldBeVisible);
        }

        void SetButtonsActive(bool shouldBeVisible)
        {
            if (_expandAnyway != null) _expandAnyway.gameObject.SetActive(shouldBeVisible);
            if (_showWhenRestrictingHeight != null) _showWhenRestrictingHeight.SetActive(shouldBeVisible);
        }

        void SetDirty()
        {
            if (!isActiveAndEnabled) return;
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            SetDirty();
        }
#endif
    }
}
