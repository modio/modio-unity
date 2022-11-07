using System.Collections.Generic;
using System.Linq;
using ModIOBrowser.Implementation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExampleTitleScene : MonoBehaviour
{
    [SerializeField] Selectable DefaultSelection;

    public string verticalControllerInput = "Vertical";
    public List<string> mouseInput = new List<string>();

    void Start()
    {
        OpenTitle();
    }

    public void OpenMods()
    {
        // Assign the 'GoBackToTitleScene' method as the onClose method so we can maintain a focused
        // selectable highlight if we're on controller
        ModIOBrowser.Browser.Instance.gameObject.SetActive(true);
        ModIOBrowser.Browser.OpenBrowser(OpenTitle);
        gameObject.transform.parent.gameObject.SetActive(false);
    }

    public void OpenTitle()
    {
        ModIOBrowser.Browser.Instance.gameObject.SetActive(false);
        gameObject.transform.parent.gameObject.SetActive(true);
        DefaultSelection.Select();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void DeselectOtherTitles()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if(Input.GetAxis(verticalControllerInput) != 0f)
        {
            //Hide mouse
            Cursor.lockState = CursorLockMode.Locked;

            if(EventSystem.current.currentSelectedGameObject == null)
            {
                DefaultSelection.Select();
            }
        }
        else if(mouseInput.Any(x => Input.GetAxis(x) != 0))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
