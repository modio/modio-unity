using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    public class ModioViewportRestraintButtons : MonoBehaviour, ILayoutElement
    {
        [SerializeField] Button _leftButton;
        [SerializeField] Button _rightButton;
        
        ScrollRect _scrollRect;
        ModioViewportRestraint _viewportRestraint;
        
        // All unused, we just want the ILayoutElement events (-1 means "please ignore this" to Unity)
        public float minWidth => -1;
        public float preferredWidth  => -1;
        public float flexibleWidth  => -1;
        public float minHeight => -1;
        public float preferredHeight  => -1;
        public float flexibleHeight  => -1;
        public int layoutPriority => -1;

        void Awake()
        {
            _scrollRect = GetComponentInParent<ScrollRect>();
            _viewportRestraint = GetComponent<ModioViewportRestraint>();

            _scrollRect.onValueChanged.AddListener(OnScrollRectMoved);
            OnScrollRectMoved(Vector2.zero);
            
            _leftButton.onClick.AddListener(LeftButtonPressed);
            _rightButton.onClick.AddListener(RightButtonPressed);
        }

        void LeftButtonPressed() => _viewportRestraint.SelectNextChildInDirection(false);

        void RightButtonPressed() => _viewportRestraint.SelectNextChildInDirection(true);

        void OnScrollRectMoved(Vector2 _)
        {
            _leftButton.interactable = _leftButton.enabled = _scrollRect.normalizedPosition.x > 0.001f;
            _rightButton.interactable = _rightButton.enabled = _scrollRect.normalizedPosition.x < 0.999f;

            float viewportWidth = _scrollRect.viewport.rect.width;
            float contentWidth = _scrollRect.content.rect.width;
            
            _leftButton.gameObject.SetActive(contentWidth > viewportWidth);
            _rightButton.gameObject.SetActive(contentWidth > viewportWidth);
        }

        public void CalculateLayoutInputHorizontal()
        {
            //catch it doing bad edit time stuff
            if (_scrollRect == null) return;
            
            OnScrollRectMoved(Vector2.zero);
        }

        public void CalculateLayoutInputVertical()
        {
            //catch it doing bad edit time stuff
            if (_scrollRect == null) return;
            
            OnScrollRectMoved(Vector2.zero);
        }
    }
}
