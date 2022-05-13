using ModIOBrowser;
using UnityEngine;

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
    [SerializeField] KeyCode Cancel = KeyCode.Joystick1Button1;
    [SerializeField] KeyCode Alternate = KeyCode.Joystick1Button2;
    [SerializeField] KeyCode Options = KeyCode.Joystick1Button3;
    [SerializeField] KeyCode TabLeft = KeyCode.Joystick1Button4;
    [SerializeField] KeyCode TabRight = KeyCode.Joystick1Button5;
    [SerializeField] KeyCode Search = KeyCode.Joystick1Button9;
    [SerializeField] KeyCode Menu = KeyCode.Joystick1Button7;

    void Update()
    {
        // This is a basic example of one way to capture inputs and inform the UI browser what
        // action that that input should perform.
        //
        // eg.
        // if we detect an ESC button press we can inform the browser with InputReceiver.OnCancel()
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
        else if(Input.GetAxis("Vertical") != 0f)
        {
            InputReceiver.OnControllerScroll(Input.GetAxis("Vertical"));
        }
    }
}
