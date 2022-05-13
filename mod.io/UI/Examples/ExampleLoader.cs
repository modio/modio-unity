using ModIO;
using UnityEngine;

/// <summary>
/// This class serves as an example for how to initialise the ModIOUnity plugin and open the
/// provided UI to browse and manage your mod collection.
/// </summary>
public class ExampleLoader : MonoBehaviour
{
    bool didInitialize;

    // If we haven't initialised the plugin we can do so here (or anytime before opening the UI)
    void Start()
    {
        if(ModIOUnity.IsInitialised())
        {
            // If we are already initialised we can simply open the UI here
            ModIOBrowser.Browser.OpenBrowser(null);
        }
        else
        {
            // Use the OnInitialise method as the callback for when our request has processed 
            ModIOUnity.InitialiseForUser("ExampleLoader", OnInitialise);
        }
    }

    // When this object is destroyed from the scene we will also want to Shutdown the plugin to
    // free up any resources we no longer require it to use
    void OnDestroy()
    {
        if(ModIOUnity.IsInitialised())
        {
            ModIOUnity.Shutdown(OnShutdown);
        }
    }

    // This method will get invoked once we've attempted to initialise the plugin. If it succeeds to
    // initialise then we can continue by opening the UI browser.
    void OnInitialise(Result result)
    {
        if(result.Succeeded())
        {
            Debug.Log("[ExampleLoader] Initialized ModIO Plugin");
            ModIOBrowser.Browser.OpenBrowser(null);
        }
        else
        {
            Debug.LogWarning("[ExampleLoader] Failed to Initialize ModIO Plugin. "
                             + "Make sure your config file is setup, located in "
                             + "Assets/Resources/mod.io\nAlso check you are using the correct "
                             + "server address ('https://api.mod.io/v1' for production or "
                             + "'https://api.test.mod.io/v1' for the test server) and that "
                             + "you've supplied the API Key and game Id for your game.");
        }
    }

    // We can use this to inform us when the Shutdown request completes
    void OnShutdown()
    {
        Debug.Log("[ExampleLoader] Shutdown ModIO Plugin");
    }
}
