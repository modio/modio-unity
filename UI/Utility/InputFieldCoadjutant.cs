using System;
using System.Collections;
using ModIO.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{
	/// <summary>
	/// This script is attached alongside InputField components so we can detect when an input field
	/// is selected so that we know when to ignore certain inputs (for example when someone is using
	/// their keyboard to navigate the UI but also to type into an input field).
	/// This class also helps the UI navigation to select but not go into edit mode when highlighting
	/// the input field. It also calls the VirtualKeyboardDelegate from OnSubmit when a user begins
	/// editing the field.
	/// </summary>
	internal class InputFieldCoadjutant : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
	{
		[SerializeField] bool editOnFocus;
		[SerializeField] string inputFieldTitle;
		[SerializeField] string inputFieldPlaceholderText;
		[SerializeField] Browser.VirtualKeyboardType keyboardtype = Browser.VirtualKeyboardType.Default;
		[SerializeField] TMP_InputField inputField;
		
		void Reset()
		{
			inputField = GetComponent<TMP_InputField>();
		}

		void OnEnable()
		{
			if(!SharedUi.settings.ShouldWeUseVirtualKeyboardDelegate())
			{
				Destroy(this);
			}
		}

		public void OnSelect(BaseEventData eventData)
		{
			if (SharedUi.settings.ShouldWeUseVirtualKeyboardDelegate())
			{
				if(editOnFocus)
				{
					OpenKeyboard();
				}
				else
				{
					StartCoroutine(UnFocusByDefault());

					InputReceiver.currentSelectedInputField = this;
				}
			}
		}

		IEnumerator UnFocusByDefault()
		{
			yield return new WaitForEndOfFrame();
			inputField.DeactivateInputField();
		}
		
		public void OnDeselect(BaseEventData eventData)
		{
			if (SharedUi.settings.ShouldWeUseVirtualKeyboardDelegate())
			{
				if(InputReceiver.currentSelectedInputField == this)
				{
					InputReceiver.currentSelectedInputField = null;
				}
			}
		}
		
		public void OnSubmit(BaseEventData eventData)
		{
			if (SharedUi.settings.ShouldWeUseVirtualKeyboardDelegate())
			{
				// Check if the user has specified an OS virtual keyboard
				OpenKeyboard();
			}
		}

		void OpenKeyboard()
		{
			// Check if the user has specified an OS virtual keyboard
			Browser.OpenVirtualKeyboard?.Invoke(
				inputFieldTitle,
				inputField.text,
				inputFieldPlaceholderText,
				keyboardtype,
				inputField.characterLimit,
				inputField.multiLine,
				OnCloseVirtualKeyboard);
		}

		void OnCloseVirtualKeyboard(string text)
		{
            // We need to add this action to a queue to be run on the main thread because this
            // callback may have come from a different thread when dealing with cross platform SDKs
            MonoDispatcher.Instance.Run(() =>
            {
				// Change the text of the input field
				inputField.text = text;
					
				// Unselect the inputField's edit mode
				StartCoroutine(UnFocusByDefault());
			});
		}
	}
}
