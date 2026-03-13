using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    public class ModioViewportRestraint : MonoBehaviour
    {
        public float PercentPaddingHorizontal = 0.05f;
        public float PercentPaddingVertical = 0.25f;

        public bool adjustHorizontally = false;
        public bool adjustVertically = true;
        static float transitionTimeBase = 0.1f;
        static float transitionTimeScreen = 0.4f;

        [SerializeField] bool _snapToMin;
        [SerializeField] bool _snapToMax;

        public RectTransform Viewport;

        // These containers are what is getting moved in the adjustment check
        public RectTransform DefaultViewportContainer;
        public RectTransform HorizontalViewportContainer;
        Vector3 _targetPosition;
        Coroutine _animCoroutine;
        
        ModioViewportRestraint _parentRestraint;

        bool _suppressParentSnap;

        void Awake()
        {
            _parentRestraint = transform.parent.GetComponentInParent<ModioViewportRestraint>();
        }

        public void ChildSelected(RectTransform ensureFits)
        {
            ModioRectHelper.GetWorldAABB(ensureFits, out Vector3 childMin,    out Vector3 childMax);
            ModioRectHelper.GetWorldAABB(Viewport,   out Vector3 viewportMin, out Vector3 viewportMax);

            var pendingAdjustment = DefaultViewportContainer.position - _targetPosition;

            viewportMin += pendingAdjustment;
            viewportMax += pendingAdjustment;

            var viewportSize = viewportMax - viewportMin;

            var padding = new Vector3(
                viewportSize.x * PercentPaddingHorizontal,
                viewportSize.y * PercentPaddingVertical
            );

            Vector3 adjustment;

            if(_snapToMin)
                adjustment = pendingAdjustment + childMin - (viewportMin + padding);
            else if(_snapToMax)
                adjustment = pendingAdjustment + childMax - (viewportMax - padding);
            else
            {
                var pos = Vector3.Max(Vector3.zero, childMax - (viewportMax - padding));
                var neg = Vector3.Min(Vector3.zero, childMin - (viewportMin + padding));

                adjustment = pos + neg + pendingAdjustment;
            }
            adjustment.z = 0;

            if (!adjustHorizontally) adjustment.x = 0;
            if (!adjustVertically) adjustment.y = 0;

            if (adjustment.sqrMagnitude < 1f)
            {
                if (_parentRestraint != null && !_suppressParentSnap) _parentRestraint.ChildSelected(ensureFits);

                return;
            }

            _targetPosition = DefaultViewportContainer.position - adjustment;

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(Transition(DefaultViewportContainer, Viewport));

            if (_parentRestraint != null && !_suppressParentSnap) _parentRestraint.ChildSelected(ensureFits);
        }

        public void SelectNextChildInDirection(bool right)
        {
            var children = GetComponentsInChildren<ModioViewportRestraintChild>();

            ModioRectHelper.GetWorldAABB(Viewport, out Vector3 viewportMin, out Vector3 viewportMax);

            float lowestDistance = float.MaxValue;

            RectTransform best = null;

            var padding = (viewportMax.x - viewportMin.x) * PercentPaddingHorizontal + 20;
            
            foreach (var child in children)
            {
                var childTransform = (RectTransform)child.transform;
                ModioRectHelper.GetWorldAABB(childTransform, out Vector3 childMin, out Vector3 childMax);

                bool offscreenRight = childMax.x > viewportMax.x + padding;
                bool offscreenLeft = childMin.x < viewportMin.x - padding;
                
                if((right && !offscreenRight) || (!right && !offscreenLeft)) continue;

                var distance = right ? childMin.x - (viewportMin.x - padding) : viewportMax.x + padding - childMax.x;
                
                if(distance > lowestDistance) continue;

                lowestDistance = distance;
                best = childTransform;
            }

            if (best == null) return;

            var pendingMovement = right ? lowestDistance : -lowestDistance;

            if (_snapToMin && !right)
            {
                lowestDistance = float.MaxValue;
                
                foreach (var child in children)
                {
                    var childTransform = (RectTransform)child.transform;
                    ModioRectHelper.GetWorldAABB(childTransform, out Vector3 childMin, out Vector3 childMax);

                    bool offscreenRight = childMax.x > viewportMax.x + padding + pendingMovement;
                    bool offscreenLeft = childMin.x < viewportMin.x - padding + pendingMovement;
                
                    /*if((right && !offscreenRight) || (!right && !offscreenLeft)) continue;*/
                    if(offscreenRight || (offscreenLeft)) continue;

                    var distance = !right ? childMin.x - (viewportMin.x - padding) : viewportMax.x + padding - childMax.x;
                
                    if(distance > lowestDistance) continue;

                    lowestDistance = distance;
                    best = childTransform;
                }
            }

            var selectable = best.GetComponent<Selectable>();

            _suppressParentSnap = true;
            if(selectable != null) selectable.Select();
            _suppressParentSnap = false;
        }

        IEnumerator Transition(RectTransform container, RectTransform viewport)
        {
            Vector3 startPos = container.position;

            var delta = _targetPosition - startPos;
            
            float maxDistPercent = Mathf.Max(Mathf.Abs(delta.x / viewport.rect.width),  Mathf.Abs(delta.y / viewport.rect.height));
            if(maxDistPercent > 1f) maxDistPercent = 1f;
            
            var duration = transitionTimeBase + maxDistPercent * transitionTimeScreen;
            
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
            {
                container.position = Vector3.Lerp(startPos, _targetPosition, t);

                yield return null; //wait one frame

                if (!adjustHorizontally) //ensure we don't snap sideways in rare edge cases
                    startPos.x = _targetPosition.x = container.position.x;
                if (!adjustVertically)
                    startPos.y = _targetPosition.y = container.position.y;
            }

            container.position = _targetPosition;
            _animCoroutine = null;
        }
    }
}
