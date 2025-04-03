using System.Collections;
using UnityEngine;

namespace Modio.Unity.UI.Navigation
{
    public class ModioViewportRestraint : MonoBehaviour
    {
        public float PercentPaddingHorizontal = 0.05f;
        public float PercentPaddingVertical = 0.25f;

        public bool adjustHorizontally = false;
        public bool adjustVertically = true;
        static float transitionTime = 0.1f;

        public RectTransform Viewport;

        // These containers are what is getting moved in the adjustment check
        public RectTransform DefaultViewportContainer;
        public RectTransform HorizontalViewportContainer;
        static readonly Vector3[] CachedFourCornersArray = new Vector3[4];
        Vector3 _targetPosition;
        Coroutine _animCoroutine;

        public void ChildSelected(RectTransform ensureFits)
        {
            GetWorldAABB(ensureFits, out Vector3 childMin,    out Vector3 childMax);
            GetWorldAABB(Viewport,   out Vector3 viewportMin, out Vector3 viewportMax);

            var pendingAdjustment = DefaultViewportContainer.position - _targetPosition;

            viewportMin += pendingAdjustment;
            viewportMax += pendingAdjustment;

            var viewportSize = viewportMax - viewportMin;

            var padding = new Vector3(
                viewportSize.x * PercentPaddingHorizontal,
                viewportSize.y * PercentPaddingVertical
            );

            var pos = Vector3.Max(Vector3.zero, childMax - (viewportMax - padding));
            var neg = Vector3.Min(Vector3.zero, childMin - (viewportMin + padding));

            var adjustment = pos + neg + pendingAdjustment;
            adjustment.z = 0;

            if (!adjustHorizontally) adjustment.x = 0;
            if (!adjustVertically) adjustment.y = 0;

            if (adjustment.sqrMagnitude < 1f)
            {
                return;
            }

            _targetPosition = DefaultViewportContainer.position - adjustment;

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(Transition(DefaultViewportContainer));

            return;

            void GetWorldAABB(RectTransform rectTransform, out Vector3 min, out Vector3 max)
            {
                rectTransform.GetWorldCorners(CachedFourCornersArray);

                min = Vector3.one * float.MaxValue;
                max = Vector3.one * float.MinValue;

                foreach (Vector3 corner in CachedFourCornersArray)
                {
                    min = Vector3.Min(min, corner);
                    max = Vector3.Max(max, corner);
                }
            }
        }

        IEnumerator Transition(Transform parent)
        {
            Vector2 startPos = parent.position;

            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / transitionTime)
            {
                parent.position = Vector3.Lerp(startPos, _targetPosition, t);

                yield return null; //wait one frame

                if (!adjustHorizontally) //ensure we don't snap sideways in rare edge cases
                {
                    startPos.x = _targetPosition.x = parent.position.x;
                }
            }

            parent.position = _targetPosition;
            _animCoroutine = null;
        }
    }
}
