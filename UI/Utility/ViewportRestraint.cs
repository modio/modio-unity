using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
    public class ViewportRestraint : MonoBehaviour, ISelectHandler
    {
        public float PercentPaddingHorizontal = 0.05f;
        public float PercentPaddingVertical = 0.25f;

        public bool adjustHorizontally = false;
        public bool adjustVertically = true;
        static float transitionTime = 0.25f;

        public RectTransform Viewport;
        
        // These containers are what is getting moved in the adjustment check
        public RectTransform DefaultViewportContainer;
        public RectTransform HorizontalViewportContainer;


        static IEnumerator HorizontalTransitionCoroutine;
        static IEnumerator VerticalTransitionCoroutine;

        public void OnSelect(BaseEventData eventData)
        {
            // This may be true when using mouse and keyboard
            if(Browser.mouseNavigation)
            {
                return;
            }

            if(adjustVertically)
            {
                CheckSelectionVerticalVisibility();
            }
            else if(adjustHorizontally)
            {
                CheckSelectionHorizontalVisibility();
            }
        }

        void BeginTransition(IEnumerator coroutineHandle, IEnumerator coroutine, Vector2 containersNewTargetPosition)
        {
            if(coroutineHandle != null)
            {
                StopCoroutine(coroutineHandle);
            }

            // run a new movement coroutine
            coroutineHandle = coroutine;
            StartCoroutine(coroutineHandle);
        }
        
        public void CheckSelectionHorizontalVisibility()
        {
            RectTransform rt = transform as RectTransform;
            RectTransformOverlap rto = new RectTransformOverlap(rt);

            RectTransformOverlap viewport = new RectTransformOverlap(Viewport ?? Browser.Instance.BrowserPanel.transform as RectTransform);

            if(rto.IsOutsideOfRectX(viewport, PercentPaddingHorizontal))
            {
                float distance = RectTransformOverlap.DistanceFromEdgeX(rto, viewport, PercentPaddingHorizontal);

                Vector2 containerPosition = HorizontalViewportContainer == null 
                                            ? DefaultViewportContainer.position
                                            : HorizontalViewportContainer.position;
                
                Vector2 targetPosition = new Vector2(
                    containerPosition.x + distance,
                    containerPosition.y);

                BeginTransition(HorizontalTransitionCoroutine,
                    TransitionHorizontally(targetPosition, HorizontalViewportContainer ?? DefaultViewportContainer),
                    targetPosition);
            }
        }

        public void CheckSelectionVerticalVisibility()
        {
            RectTransform rt = transform as RectTransform;
            RectTransformOverlap rto = new RectTransformOverlap(rt);

            RectTransformOverlap viewport = new RectTransformOverlap(Viewport ?? Browser.Instance.BrowserPanel.transform as RectTransform);

            if(rto.IsOutsideOfRectY(viewport, PercentPaddingVertical))
            {
                float distance = RectTransformOverlap.DistanceFromEdgeY(rto, viewport, PercentPaddingVertical);

                Vector2 targetPosition = new Vector2(
                    DefaultViewportContainer.position.x,
                    DefaultViewportContainer.position.y + distance);

                BeginTransition(HorizontalTransitionCoroutine,
                    TransitionVertically(targetPosition, DefaultViewportContainer),
                    targetPosition);
            }
        }

        static IEnumerator Transition(Vector2 end, Transform parent, bool lockX, bool lockY)
        {
            Vector2 start = parent.position;
            Vector2 distance = end - start;
            Vector2 current;
            float timePassed = 0f;
            float time;

            while(timePassed <= transitionTime)
            {
                timePassed += Time.fixedDeltaTime;
                time = timePassed / transitionTime;
                current = start;
                current += distance * time;

                //The locks are super important. If you don't lock according to axis you can get strange behaviours
                //if you move left / right AND up / down at the same time.

                if(lockX)
                    current.x = parent.position.x;

                if(lockY)
                    current.y = parent.position.y;

                parent.position = current;

                yield return new WaitForSecondsRealtime(0.01f);
            }
        }

        static IEnumerator TransitionHorizontally(Vector2 end, Transform parent)
        {
            yield return Transition(end, parent, false, true);
        }

        static IEnumerator TransitionVertically(Vector2 end, Transform parent)
        {
            yield return Transition(end, parent, true, false);
        }
    }
}
