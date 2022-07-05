using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
	internal class InputBlockingForInputFieldComponent : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		[SerializeField] TMP_InputField inputField;
		void Reset()
		{
			inputField = GetComponent<TMP_InputField>();
		}

		public void OnSelect(BaseEventData eventData)
		{
			StartCoroutine(UnFocusByDefault());
			
			InputReceiver.currentSelectedInputField = this;
		}

		IEnumerator UnFocusByDefault()
		{
			yield return new WaitForEndOfFrame();
			inputField.DeactivateInputField();
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
