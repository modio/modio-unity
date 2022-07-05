using System.Collections.Generic;
using System.Linq;
using ModIOBrowser;
using UnityEngine;

/// <summary>
/// Sometimes you want the game to work with both a controller/keyboard and a mouse
/// This function handles toggling between these modes
/// You'll find these settings in Project Settings -> Input. Look for "Axes".
/// </summary>
class KbMouseControlling : MonoBehaviour
{
    public List<string> controllerAndKeyboardInput = new List<string>();
    public List<string> mouseInput = new List<string>();
    public string verticalControllerInput = "Vertical";

    void Update()
    {
        if(Input.GetAxis(verticalControllerInput) != 0f)
        {
            InputReceiver.OnControllerScroll(Input.GetAxis(verticalControllerInput));
        }

        //we could just log the item, ignore null inputs, and reselect that on keyboard
        if(controllerAndKeyboardInput.Any(x => Input.GetAxis(x) != 0))
        {
            if(InputReceiver.currentSelectedInputField != null)
            {
                return;
            }
            InputReceiver.OnSetToControllerNavigation();
        }
        else if(mouseInput.Any(x => Input.GetAxis(x) != 0))
        {
            InputReceiver.OnSetToMouseNavigation();
        }
    }
}
