using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    public class ModioViewportRestraintButtons : MonoBehaviour
    {
        [SerializeField] Button _leftButton;
        [SerializeField] Button _rightButton;
        
        ScrollRect _scrollRect;
        ModioViewportRestraint _viewportRestraint;

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
        }
    }
}
