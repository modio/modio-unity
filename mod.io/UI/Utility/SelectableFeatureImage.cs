
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
    public class SelectableFeatureImage : MonoBehaviour, IMoveHandler, ISelectHandler, IDeselectHandler
    {

        public void OnMove(AxisEventData eventData)
        {
            if(eventData.moveDir == MoveDirection.Left)
            {
                Browser.Instance.PageFeaturedRowLeft();
            }
            else if(eventData.moveDir == MoveDirection.Right)
            {
                Browser.Instance.PageFeaturedRowRight();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            //ScrollRectViewHandler.CheckSelectionVerticalVisibility(transform as RectTransform);
            Browser.Instance.isFeaturedItemSelected = true;
        }
        public void OnDeselect(BaseEventData eventData)
        {
            Browser.Instance.isFeaturedItemSelected = false;
        }
    }
}
