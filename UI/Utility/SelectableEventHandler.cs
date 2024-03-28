using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Plugins.mod.io.UI.Utility
{
    public class SelectableEventHandler : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private UnityEvent unityEvent;

        public void OnSelect(BaseEventData eventData)
        {
            unityEvent?.Invoke();
        }
    }
}
