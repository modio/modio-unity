using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modio.Unity.UI.Input
{
    public class ModioUIScrollRect : ScrollRect
    {
        ScrollRect _parent;

        // Adding these in, but note they don't show in the non-debug inspector
        // as we're not overriding the ScrollRect inspector
        [SerializeField] bool _passScrollToParent = true;
        [SerializeField] bool _passDragToParent = true;

        protected override void Awake()
        {
            _parent = transform.parent.GetComponentInParent<ScrollRect>();
            if (_parent == null) _parent = null;
        }

        public override void OnScroll(PointerEventData data)
        {
            //TODO: if we want to allow horizontal scroll with a modifier held, do that here
            if (_passScrollToParent && _parent != null) // && !modifierHeld)
            {
                _parent.OnScroll(data);
                return;
            }

            base.OnScroll(data);
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if(_passDragToParent)
                _parent?.OnInitializePotentialDrag(eventData);
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if(_passDragToParent)
                _parent?.OnBeginDrag(eventData);
            base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if(_passDragToParent)
                _parent?.OnDrag(eventData);
            base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if(_passDragToParent)
                _parent?.OnEndDrag(eventData);
            base.OnEndDrag(eventData);
        }
    }
}
