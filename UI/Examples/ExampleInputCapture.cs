﻿using System.Collections.Generic;
using System.Linq;
using ModIOBrowser;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// An Example script on how to setup the key/button bindings for the ModIO Browser. Inputs such as
/// Tab left and right, options and alternate-submit. All of these inputs provide extra functionality
/// and ease of navigation for the user.
///
/// This script makes use of the InputReceiver static class to invoke the correct behaviour on the
/// Browser.
/// For example: when the input is captured for KeyCode.Joystick1Button2, the method on InputReceiver.Alternate()
/// is invoked. You can use InputReceiver.cs to tell the browser when a specific input has been used
/// </summary>
public class ExampleInputCapture : MonoBehaviour
{
    // Submit and Horizontal/Vertical directional input is handled by default with Unity's built in
    // UI system. You can set those bindings up with the current or new Unity Input system.
    // refer to the StandaloneInputModule component on the EventSystem gameObject in scene.

    // The following inputs are for added ergonomic use.
    [SerializeField] KeyCode Cancel = KeyCode.JoystickButton1;
    [SerializeField] KeyCode Alternate = KeyCode.JoystickButton2;
    [SerializeField] KeyCode Options = KeyCode.JoystickButton3;
    [SerializeField] KeyCode TabLeft = KeyCode.JoystickButton4;
    [SerializeField] KeyCode TabRight = KeyCode.JoystickButton5;
    [SerializeField] KeyCode Search = KeyCode.JoystickButton9;
    [SerializeField] KeyCode Menu = KeyCode.JoystickButton7;

    // Control mappings for keyboard, controller, and mouse. If the controls for the app
    // are changed under: Project Settings -> Input -> Axes, then they must be also be changed here!
    // Unfortunately as of developing this, there is no simple way to fetch these values by code.
    public List<string> controllerAndKeyboardInput = new List<string>
    {
        "Horizontal",
        "Vertical"
    };
    public List<string> mouseInput = new List<string>
    {
        "Mouse X",
        "Mouse Y"
    };
    public string verticalControllerInput = "Vertical";

    [ContextMenu("Set to controller default")]
    public void SetToControllerDefault()
    {
        Cancel = KeyCode.JoystickButton1;
        Alternate = KeyCode.JoystickButton2;
        Options = KeyCode.JoystickButton3;
        TabLeft = KeyCode.JoystickButton4;
        TabRight = KeyCode.JoystickButton5;
        Search = KeyCode.JoystickButton9;
        Menu = KeyCode.JoystickButton7;
    }

    [ContextMenu("Set to keyboard default")]
    public void SetToKeyboardDefault()
    {
        Cancel = KeyCode.Escape;
        Alternate = KeyCode.Alpha1;
        Options = KeyCode.Alpha2;
        TabLeft = KeyCode.LeftCurlyBracket;
        TabRight = KeyCode.RightCurlyBracket;
        Search = KeyCode.Alpha3;
        Menu = KeyCode.Alpha4;
    }

    void Start()
    {
        if (Application.platform != RuntimePlatform.Switch) return;

        // We have to switch the inputs of South & East for Switch exclusively, hence this is here

        Cancel = KeyCode.Joystick1Button0;

        // Unity's default input system handles the select button incorrectly on Switch, it treats the South button as Select
        // whereas the East button is supposed to be select according to Switch conventions. Therefore, to fix, we have to
        // access the StandaloneInputModule directly and swap them around using the Tuple trick below.
        StandaloneInputModule unityInputModule = FindObjectOfType<StandaloneInputModule>();

        if (unityInputModule == null)
        {
            Debug.Log($"Could not swap unity inputs!");
            return;
        }

        (unityInputModule.submitButton, unityInputModule.cancelButton) = (unityInputModule.cancelButton, unityInputModule.submitButton);
    }

    void Update()
    {
        if(!Browser.IsOpen) return;

        // This is a basic example of one way to capture inputs and inform the UI browser what
        // action that that input should perform.
        //
        // eg.
        // if we detect an ESC button press we can inform the browser with InputReceiver.OnCancel()
        HandleInputReceiver();

        // This is a basic example of how we connect the mouse and controller input to the Browser
        //
        // eg.
        // Pressing a controller or keyboard button will turn off mouse navigation, hide the mouse,
        // and tell the browser to focus on controller navigation, and vice versa.
        HandleControllerInput();
    }

    void HandleInputReceiver()
    {
        if(Input.GetKeyDown(Cancel))
        {
            InputReceiver.OnCancel();
        }
        else if(Input.GetKeyDown(Alternate))
        {
            InputReceiver.OnAlternate();
        }
        else if(Input.GetKeyDown(Options))
        {
            InputReceiver.OnOptions();
        }
        else if(Input.GetKeyDown(TabLeft))
        {
            InputReceiver.OnTabLeft();
        }
        else if(Input.GetKeyDown(TabRight))
        {
            InputReceiver.OnTabRight();
        }
        else if(Input.GetKeyDown(Search))
        {
            InputReceiver.OnSearch();
        }
        else if(Input.GetKeyDown(Menu))
        {
            InputReceiver.OnMenu();
        }
    }

    void HandleControllerInput()
    {
        // Handle controller scrolling
        // For now, this is only used in the mod details view, and may clash with "regular" navigation
        if(Input.GetAxis(verticalControllerInput) != 0f)
        {
            InputReceiver.OnControllerScroll(Input.GetAxis(verticalControllerInput));
        }

        //Do we have input from the keyboard or controller? If so, switch to controller mode.
        if(controllerAndKeyboardInput.Any(x => Input.GetAxis(x) != 0))
        {
            if(InputReceiver.currentSelectedInputField != null)
            {
                return;
            }
            InputReceiver.OnSetToControllerNavigation();
        }
        //If not, do we have input from the mouse? If true, switch to mouse mode.
        else if(mouseInput.Any(x => Input.GetAxis(x) != 0))
        {
            InputReceiver.OnSetToMouseNavigation();
        }
    }


}
