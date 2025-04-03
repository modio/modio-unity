# New Component UI Preview

The Component UI has two key goals; provide a series of components that are easier to integrate into all designs, and a robust UI suitable for dropping into your project as is or with as much customization as you'd like

<p float="left">
  <img src="/Assets/Plugins/ModioUI/ReadmeImages/compui-songsofconquest.png" width="49%" alt="Screenshot of the Mod Browser" />
  <img src="/Assets/Plugins/ModioUI/ReadmeImages/compui-songsofconquest-2.png" width="49%" alt="Screenshot of an individual mod" /> 
</p>

> [!IMPORTANT]  
> The Component UI relies on the *config file* that is configured during the setup instructions on the main Unity Modio readme. Ensure you have completed all of those steps before proceeding.

# Example Scene
Please see the ModioComponentUI scene for an example of the UI in action.

There's a few key things in the scene to note, which you'll need to copy into your scene or create a suitable alternative
- Canvas (Mods) contains the prefab ModBrowser. This is the core of the UI, and can be dropped into your menu scene as is
  - Either duplicate the Canvas, or add the ModBrowser prefab to a canvas of your own. It's designed to look best on a "Scale with Screen Size" canvas with `Reference Resolution` 1920x1080 and `Match` set to 1 (Height)
- ModioUI_InputCapture will load an EventSystem at runtime, as well as integrating it with our UI. It also maps icons for use in the UI
  - You will need to have one of the EventSystems it references available, or create your own in scene
  - If you are using Unity's InputSystem, you can extract ModioInputListener_InputSystem.zip
- ExamplePlatformIntegration loads example integrations (such as Steam (via the Facepunch library) or console integrations).
You will likely want to create or modify your own implementations based on these.
- ModioUI_Localization provides an example localization implementation. It's possible to bypass this and use your own loc system if desired
- Canvas (Title) is a basic menu and settings page that gives some more control of the demo

### Opening the Browser
- You can open the browser by calling `OpenPanel()` on the instance of `ModBrowserPanel` in your scene.
  - You can also get that instance by calling `ModioPanelManager.GetPanelOfType<ModBrowserPanel>()`

# Key components
### ModioUIMod and similar
We use monobehaviours like ModioUIMod to act as containers for `Modio.Unity.UI.Data.Mod`, which holds all the details you may wish to know about the mod. 
Children of that gameobject will add `ModioUIModProperties` monoBehaviours, which have properties added to update UI or add listeners to buttons (such as to subscribe or get ratings)

There's also
- ModioUISearch and ModioUISearchProperties, which contain and modify the current search filter (tags, search text, pagination) and access the resulting mods
  - There's currently two ModioUISearches in our example. One exists on the ModBrowser root object, and controls the main search. 
  All children use this by default
  - ModPanel_Dependencies has its own ModioUISearch and its children will display the results of a dependency search instead
- ModioUIUser and ModioUIUserProperties, which has details about either the logged in local user or the author of a mod.
  - If you want to display the author of a mod, use a `ModProperties` with the Creator property, pointed at a ModioUIUser on the same GameObject. 
  You can then use ModioUIUserProperties to display details like the name or icon
  - See "Title and Creator" on the "ModContent_ModDetails" prefab for an example

### ModioPanels
We use a panels system, which handles the basics of pushing and popping panels as they're opened by the user
- There's only ever one panel focused, and they handle registering and unregistering from input events when focus changes.
- This includes interactive screens like ModDisplay or the main ModBrowser, as well as non-interactive states like a waiting screen (ModioWaitingPanelGeneric)

### Localization
We have a basic localization implementation in the ModioUI_Localization prefab. This allows all of our buttons to look up a key in a CSV, but lacks the advanced features typical in most localization packages.
- The localization solution looks at `Settings.server.languageCode` to determine the language. Please re-initialise the plugin via `ModIOUnity.InitializeForUser` if you change the language. See `ModioExampleSettingsPanel` for an example.
- If you are using a localization package, you can override the implementation by calling `ModioUI_Localization.SetCustomHandler(YourHandlerMethod)`
- Alternatively, you can leave both localization solutions running
- The component UI does not handle RTL text to avoid conflicts. If your game supports RTL language, you'll need to apply your solution to the Component UI yourself

# Customization

### Basic Recoloring
The Component UI is built using a small set of prefabs stored in `ModioUI/Prefabs/Widgets/UIBasics/Components/`
- The majority of background elements are ButtonBackground or PanelBackground
  - In addition to changing the `Image` component's Color, you'll need to change the values on `ModioUISelectableTransitions` which controls the various selection states
  - ButtonBackground is also used in locations other than buttons, with some of its functionality (like SelectableTransitions) disabled
- Most text is based on the `ButtonText (TMP)` prefab. You can change its font, and change colors in the same way as above

### Adding ModioUIModProperties options
To add a new property, simply extend IModProperty and respond to the OnModUpdate property, which will be called whenever the mod is changed or 'dirtied' (such as by changing subscription or file state, rating the mod, or dependencies being fetched from the server)
```csharp
[Serializable]
public class ModPropertySummary : IModProperty
{
    [SerializeField] TMP_Text _text;

    public void OnModUpdate(Mod mod) => _text.text = mod.Summary;
}
```
