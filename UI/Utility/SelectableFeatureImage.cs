
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
                Home.Instance.PageFeaturedRow(false);
            }
            else if(eventData.moveDir == MoveDirection.Right)
            {
                Home.Instance.PageFeaturedRow(true);
            }
        }

        public void OnSelect(BaseEventData eventData)
            => Home.Instance.FeaturedItemSelect(true);

        public void OnDeselect(BaseEventData eventData)
            => Home.Instance.FeaturedItemSelect(false);
    }
}
