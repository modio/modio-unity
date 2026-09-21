using Modio.Platforms;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUITitleSafeArea : MonoBehaviour
    {
        static Vector2? _offset;
        [SerializeField] bool _inverseForBackgrounds;

        void OnEnable()
        {
            if(_offset == null)
            {
                (float horizontal, float vertical) titleSafeArea = (1, 1);

                if (ModioServices.TryResolve(out ITitleSafeAreaProvider titleSafeAreaProvider))
                {
                    titleSafeArea = titleSafeAreaProvider.GetTitleSafeArea();
                }
                /*else  // For testing
                {
                    titleSafeArea = (0.9f, 0.9f);
                }*/

                var rectTransform = (RectTransform)transform;

                _offset = new Vector2(
                    rectTransform.rect.width * (1 - titleSafeArea.horizontal) / 2,
                    rectTransform.rect.height * (1 - titleSafeArea.vertical) / 2
                );
            }

            SetPadding(_inverseForBackgrounds ? -_offset.Value : _offset.Value);
            
        }
        
        void SetPadding(Vector2 offset)
        {
            var rectTransform = (RectTransform)transform;

            rectTransform.offsetMin = offset;
            rectTransform.offsetMax = -offset;
        }
    }
}
