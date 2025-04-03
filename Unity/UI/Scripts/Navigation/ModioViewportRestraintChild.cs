using UnityEngine;
using UnityEngine.EventSystems;

namespace Modio.Unity.UI.Navigation
{
    public class ModioViewportRestraintChild : MonoBehaviour, ISelectHandler
    {
        [SerializeField] RectTransform _overrideFocusTo;

        ModioViewportRestraint _viewportRestraint;

        void Awake()
        {
            _viewportRestraint = GetComponentInParent<ModioViewportRestraint>();

            if (_viewportRestraint == null) enabled = false;
        }


        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is PointerEventData _)
                return;

            MoveToSelected();
        }

        private void MoveToSelected()
        {
            // Restrict to the overridden target, then the standard one
            // This will ensure we're on screen and as much of the override as possible
            // is also on screen
            if (_overrideFocusTo != null) _viewportRestraint.ChildSelected(_overrideFocusTo);

            var rectTransform = transform as RectTransform;
            if (rectTransform != null) _viewportRestraint.ChildSelected(rectTransform);
        }
    }
}
