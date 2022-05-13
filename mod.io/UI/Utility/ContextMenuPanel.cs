using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
    public class ContextMenuPanel : MonoBehaviour, IPointerExitHandler
    {
        public void OnPointerExit(PointerEventData eventData)
        {
            gameObject.SetActive(false);
        }
    }
}
