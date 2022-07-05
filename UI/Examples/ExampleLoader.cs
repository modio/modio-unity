using UnityEngine;

/// <summary>
/// This class serves as an example for how to initialize the ModIOUnity plugin and open the
/// provided UI to browse and manage your mod collection.
/// </summary>
public class ExampleLoader : MonoBehaviour
{
    void Start()
    {
        ModIOBrowser.Browser.OpenBrowser(null);
    }
}
