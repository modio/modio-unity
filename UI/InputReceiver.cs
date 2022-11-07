using ModIOBrowser.Implementation;

namespace ModIOBrowser
{

    /// <summary>
    /// This class is intended to be used to inform the Mod Browser UI of input presses.
    /// Regular UI navigation, such as movement (left, right, up, down), will be detected by
    /// default from the SandaloneInputModule component in scene. You can edit these from the unity
    /// editor inspector panel when selecting the EventSystem gameObject in the scene.
    /// </summary>
    public static class InputReceiver
	{
		internal static InputFieldCoadjutant currentSelectedInputField;

		/// <summary>
		/// Used as a 'back' option.
		/// </summary>
		/// <remarks>
		/// Consider an input that would be used to close a dialog, context menu or the current panel.
		/// </remarks>
		public static void OnCancel()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.Cancel();
			}
		}

		/// <summary>
		/// Used as a secondary 'submit' option.
		/// </summary>
		/// <remarks>
		/// Consider an input that would be used to select a secondary option, such as 'subscribe'
		/// while hovering on a mod profile as opposed to a regular submit for selecting it
		/// </remarks>
		public static void OnAlternate()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.Alternate();
			}
		}

		/// <summary>
		/// Used as an extra 'submit' option.
		/// </summary>
		/// <remarks>
		/// Consider an input that would be used to open a context menu, similar to a 'right click'
		/// </remarks>
		public static void OnOptions()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.Options();
			}
		}

		/// <summary>
		/// Used to jump or page a selection, typically the right bumper on a game pad.
		/// </summary>
		public static void OnTabRight()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.TabRight();
			}
		}

		/// <summary>
		/// Used to jump or page a selection, typically the left bumper on a game pad.
		/// </summary>
		public static void OnTabLeft()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.TabLeft();
			}
		}

		/// <summary>
		/// Used to open the search panel.
		/// </summary>
		/// <remarks>
		/// Consider a key press similar to a 'menu' type input
		/// </remarks>
		public static void OnSearch()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.SearchInput();
			}
		}
		
		/// <summary>
		/// Used to open the login dialog / download queue.
		/// </summary>
		/// <remarks>
		/// Consider a key press similar to a 'user menu' input for viewing your profile
		/// </remarks>
		public static void OnMenu()
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.MenuInput();
			}
		}

		/// <summary>
		/// Used as an alternate axis for scrolling content. 
		/// </summary>
		/// <remarks>
		/// Typically this should be the 'other' joystick so the user can scroll without losing or
		/// moving their selection.
		/// </remarks>
		public static void OnControllerScroll(float direction)
		{
			if(Browser.Instance.BrowserCanvas.activeSelf)
			{
				Browser.Instance.SetToControllerNavigation();
				Browser.Scroll(direction);
			}
		}

        /// <summary>
        /// Called when switching to keyboard / controller input
        /// </summary>
        public static void OnSetToControllerNavigation()
        {
	        if(Browser.Instance.BrowserCanvas.activeSelf)
	        {
		        Browser.Instance.SetToControllerNavigation();
	        }
        }

        /// <summary>
        /// Called when switching to mouse input
        /// </summary>
        public static void OnSetToMouseNavigation()
        {
	        if(Browser.Instance.BrowserCanvas.activeSelf)
	        {
		        Browser.Instance.SetToMouseNavigation();
	        }
        }
    }
}
