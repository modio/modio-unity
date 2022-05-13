using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser
{
	internal class InputBlockingForInputFieldComponent : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		public void OnSelect(BaseEventData eventData)
		{
			InputReceiver.currentSelectedInputField = this;
		}
		public void OnDeselect(BaseEventData eventData)
		{
			if(InputReceiver.currentSelectedInputField == this)
			{
				InputReceiver.currentSelectedInputField = null;
			}
		}
	}
}
