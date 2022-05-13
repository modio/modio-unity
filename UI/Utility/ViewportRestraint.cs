using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
	public class ViewportRestraint : MonoBehaviour, ISelectHandler
	{
		public bool adjustHorizontally = false;
		public bool adjustVertically = true;
		RectTransform rectTransform;
		//static AnimationCurve animationCurve = AnimationCurve.Constant(0,1,1);
		static float transitionTime = 0.25f;

		public bool UseScreenAsViewport = true;
		public RectTransform Viewport;
		public RectTransform Container;
		public RectTransform HorizontalContainer;

		[Header("Padding")]
		public int Left = 24;
		public int Right = 24;
		public int Top = 24;
		public int Bottom = 24;

		static IEnumerator HorizontalTransitionCoroutine;
		static IEnumerator VerticalTransitionCoroutine;

		void OnEnable()
		{
			rectTransform = transform as RectTransform;
		}

		public void OnSelect(BaseEventData eventData)
		{
			// This may be true when using mouse and keyboard
			if(Browser.mouseAndKeyboardNavigation)
			{
				return;
			}

			if(adjustVertically)
			{
				CheckSelectionVerticalVisibility();
			}
			if(adjustHorizontally)
			{
				CheckSelectionHorizontalVisibility();
			}
		}

		/// <summary>
		/// Use this to inform the handler that a new selectable component has been selected via OnSelect
		/// </summary>
		/// <param name="Container">the RectTransform of the selectable</param>
		public void CheckSelectionVerticalVisibility()
		{

			float viewportTopEdge = UseScreenAsViewport ? Screen.height : Viewport.position.y + Viewport.rect.height * (1 - Viewport.pivot.y);
			float viewportBottomEdge = UseScreenAsViewport ? 0 : Viewport.position.y - Viewport.rect.height * Viewport.pivot.y;

			// get position of top edge
			float y = rectTransform.position.y;
			float height = rectTransform.rect.height;
			float topEdgeY = y + height * (1 - rectTransform.pivot.y);
			float bottomEdgeY = y - height * rectTransform.pivot.y;

			// if top/bottom edge is off screen, transition
			if(topEdgeY > viewportTopEdge - Top)
			{
				if(VerticalTransitionCoroutine != null)
				{
					StopCoroutine(VerticalTransitionCoroutine);
				}
				float distance = topEdgeY - (viewportTopEdge - Top);
				float end = Container.position.y - distance;
				Vector2 transitionDestination = Container.position;
				transitionDestination.y = end;
				VerticalTransitionCoroutine = TransitionVertically(transitionDestination, Container);
				StartCoroutine(VerticalTransitionCoroutine);
			}
			else if(bottomEdgeY < viewportBottomEdge + Bottom)
			{
				if(VerticalTransitionCoroutine != null)
				{
					StopCoroutine(VerticalTransitionCoroutine);
				}
				float distance = bottomEdgeY - (Bottom + viewportBottomEdge);
				float end = Container.position.y - distance;
				Vector2 transitionDestination = Container.position;
				transitionDestination.y = end;
				VerticalTransitionCoroutine = TransitionVertically(transitionDestination, Container);
				StartCoroutine(VerticalTransitionCoroutine);
			}
		}

		public void CheckSelectionHorizontalVisibility()
		{
			RectTransform content = HorizontalContainer ?? Container;

			float viewportRightEdge = UseScreenAsViewport ? Screen.width : Viewport.position.x + Viewport.rect.width * 1 - (Viewport.pivot.x);
			float viewportLeftEdge = UseScreenAsViewport ? 0 : Viewport.position.x - Viewport.rect.width * (Viewport.pivot.x);

			// get position of top edge
			float x = rectTransform.position.x;
			float width = rectTransform.rect.width;
			float rightEdgeX = x + width * (1 - rectTransform.pivot.x);
			float leftEdgeX = x - width * rectTransform.pivot.x;

			// if top/bottom edge is off screen, transition
			if(rightEdgeX > viewportRightEdge - Right)
			{
				if(HorizontalTransitionCoroutine != null)
				{
					StopCoroutine(HorizontalTransitionCoroutine);
				}
				float distance = rightEdgeX - (viewportRightEdge - Right);
				float end = content.position.x - distance;
				Vector2 transitionDestination = content.position;
				transitionDestination.x = end;
				HorizontalTransitionCoroutine = TransitionHorizontally(transitionDestination, content);
				StartCoroutine(HorizontalTransitionCoroutine);
			}
			else if(leftEdgeX < viewportLeftEdge + Left)
			{
				if(HorizontalTransitionCoroutine != null)
				{
					StopCoroutine(HorizontalTransitionCoroutine);
				}
				float distance = leftEdgeX - (viewportLeftEdge + Left);
				float end = content.position.x - distance;
				Vector2 transitionDestination = content.position;
				transitionDestination.x = end;
				HorizontalTransitionCoroutine = TransitionHorizontally(transitionDestination, content);
				StartCoroutine(HorizontalTransitionCoroutine);
			}
		}

		static IEnumerator TransitionHorizontally(Vector2 end, Transform parent)
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
				//time = animationCurve.Evaluate(time);
				current = start;
				current += distance * time;
				current.y = parent.position.y;
				parent.position = current;
				
				yield return new WaitForSecondsRealtime(0.01f);
			}
		}

		static IEnumerator TransitionVertically(Vector2 end, Transform parent)
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
				//time = animationCurve.Evaluate(time);
				current = start;
				current += distance * time;
				current.x = parent.position.x;
				parent.position = current;
				
				yield return new WaitForSecondsRealtime(0.01f);
			}
		}
	}
}
