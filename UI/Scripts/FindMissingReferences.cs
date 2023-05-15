#if UNITY_EDITOR

using System.Collections.Generic;
using ModIOBrowser.Implementation;
using UnityEditor;
using UnityEngine;

namespace ModIOBrowser
{
    class FindMissingReferences
    {
        [MenuItem("Tools/Find Missing button references in scene")]
        public static void CheckForMissingButtonLinks()
        {
            //Looping through buttons may result in duplicates, using a
            //hashset manages that
            var summary = new HashSet<string>();
            
            var buttons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();

            
            foreach(var button in buttons)
            {
                //Get the button's onClick event
                UnityEngine.UI.Button.ButtonClickedEvent onClickEvent = button.onClick;


                //Loop through the event's listeners
                for(int i = 0; i < onClickEvent.GetPersistentEventCount(); i++)
                {
                    if(onClickEvent.GetPersistentTarget(i) == null)
                    {
                        var s = $"Missing ref at path: {button.transform.FullPath()}";

                        summary.Add(s);
                        Debug.LogError(s);
                        
                    }
                }

            }

            var finalSummary = "Warning: Sometimes this function fails, some of these buttons may have functions on them.";
            foreach(var item in summary)
            {
                finalSummary += item + "\n";
            }
            Debug.Log("Summary:\n" + finalSummary);
            GUIUtility.systemCopyBuffer = finalSummary; //for easier copy-pasta
        }
        
        [MenuItem("Tools/Find Missing Color Scheme references in scene")]
        public static void CheckForMissingColorSchemeRefs()
        {
            var buttons = Resources.FindObjectsOfTypeAll<MultiTargetButton>();
            var dropdowns = Resources.FindObjectsOfTypeAll<MultiTargetDropdown>();
            var toggles = Resources.FindObjectsOfTypeAll<MultiTargetToggle>();

            var foundSchemes =  Resources.FindObjectsOfTypeAll<ColorScheme>();
            var colorScheme = foundSchemes[0];
            
            foreach(var button in buttons)
            {
                if(button.scheme == null)
                {
                    button.scheme = colorScheme;
                    Debug.Log($"Button found with missing reference (assigning). Located at {button.transform.FullPath()}");
                }
            }
            foreach(var dropdown in dropdowns)
            {
                if(dropdown.scheme == null)
                {
                    dropdown.scheme = colorScheme;
                    Debug.Log($"Dropdown found with missing reference (assigning). Located at {dropdown.transform.FullPath()}");
                }
            }
            foreach(var toggle in toggles)
            {
                if(toggle.scheme == null)
                {
                    toggle.scheme = colorScheme;
                    Debug.Log($"Toggle found with missing reference (assigning). Located at {toggle.transform.FullPath()}");
                }
            }

            Debug.Log("Finished checking components for missing color scheme references");
        }
    }
}

#endif
