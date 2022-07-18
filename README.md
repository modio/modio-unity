# <a href="https://www.youtube.com/watch?v=pmECrkdzHzQ">Watch the video tutorial</a>
<a href="https://www.youtube.com/watch?v=pmECrkdzHzQ"><img src="https://img.youtube.com/vi/pmECrkdzHzQ/0.jpg" alt="mod.io" width="420"/></a>

<a href="https://mod.io"><img src="https://beta.mod.io/images/branding/modio_logo_bluewhite.svg" alt="mod.io" width="360" align="right"/></a>
# Mod.io Unity Plugin
[![License](https://img.shields.io/badge/license-MIT-brightgreen.svg)](https://github.com/modio/modio-unity/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/389039439487434752.svg?label=Discord&logo=discord&color=7289DA&labelColor=2C2F33)](https://discord.mod.io)
[![Master docs](https://img.shields.io/badge/docs-master-green.svg)](https://github.com/modio/modio-unity-v2/wiki)
[![Unity 3D](https://img.shields.io/badge/Unity-2018.4+-lightgrey.svg)](https://unity3d.com)

## Installation
Requires **Unity 2018.4** or later. Tested on Windows, and MacOS.

### Git Repository or .unitypackage
You can import the plugin directly from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/mod-browser-manager-by-mod-io-138866), or by downloading the package directly from the [Releases page](https://github.com/modio/modio-unity/releases). If you have any previous versions of the plugin installed, it is highly recommended to delete them before importing a newer version.

Alternatively, you can download an archive of the code using GitHub's download feature and place it in the Assets/Plugins directory within your Unity project.

## Getting started

1. Set up your [game profile on mod.io](https://mod.io/games/add) (or our [private test environment](https://test.mod.io/games/add)) to get your game ID and API key. 
2. Add the plugin to your project using the installation instructions above.
3. Ensure you dont have any conflicting libraries by going to Assets/Plugins/mod.io/ThirdParty to remove any libraries you may already have in your project (such as JsonNet).
4. Restart unity to ensure it recognises the new assembly definitions.
5. Go to Tools > mod.io > Edit Settings to locate the config file.
6. Select the config file and use the inspector to assign your game ID and API key in server settings (Make sure to deselect the config file before using playmode in the editor. A known unity bug can cause the editor to crash in 2019-2021).
7. Setup complete! Join us [on Discord](https://discord.mod.io) if you have any questions or need help.

## Setting up the Browser UI

If you do not wish to create your own UI implementation you can use our default UI that comes built in to the plugin. (If you dont wish to use the UI it is safe to delete the UI folder located at Assets/Plugins/mod.io/UI)

1. Follow the steps above to setup the config.
2. Navigate to the ModIOBrowser prefab at Assets/Plugins/mod.io/UI/Examples and drag it into your scene.
3. Use the ModIOBrowser.Browser.OpenBrowser() method to open the browser in your scene. 
`ModIOBrowser.Browser.OpenBrowser(null)`
4. The Browser UI is now setup!

## Authentication
In the current version of the plugin it is required that a user session is authenticated. Either via email or through another third party, such as Steam or Google. The process is fairly simply. Examples can be found below. 


## Usage
below are a couple examples for some of the common usages of the plugin. Such as initialising, authenticating, enabling automatic downloads and installs, and getting a few mods from the mod.io server.

All of the methods required to use the plugin can be found in ModIOUnity.cs. If you prefer using async methods over callbacks you can alternatively use ModIOUnityAsync.cs to use an async variation of the same methods.

### Initialise the plugin
```javascript
async void Example()
{
    Result result = await ModIOUnityAsync.InitializeForUser("ExampleUser");
 
    if (result.Succeeded())
    {
        Debug.Log("Initialised plugin");
    }
    else
    {
        Debug.Log("Failed to initialise plugin");
    {
}
```

### Get the user's installed mods
```javascript
void Example()
{
    SubscribedMod[] mods = ModIOUnity.GetSubscribedMods(out Result result);
    if (result.Succeeded())
    {
        foreach(var mod in mods)
        {
            if(mod.status == SubscribedModStatus.Installed)
            {
                string directoryWithInstalledMod = mod.directory;
            }
        }
    }
}
```

### Enable automatic mod downloads and installs
```javascript
void Example()
{
    Result result = ModIOUnity.EnableModManagement(ModManagementDelegate);

    if (result.Succeeded())
    {
        Debug.Log("Enabled mod management");
    }
    else
    {
        Debug.Log("Failed to enable mod management");
    {
}
 
// The following method will get invoked whenever an event concerning mod management occurs
void ModManagementDelegate(ModManagementEventType eventType, ModId modId, Result result)
{
    Debug.Log("a mod management event of type " + eventType.ToString() + " has been invoked");
}
```

### Authenticate a user
In the current version of the plugin it is required that a user session is authenticated in order to subscribe and download mods. You can accomplish this with an email address or through another third party service, such as Steam or Google. Below is an example of how to do this from an email address provided by the user. A security code will be sent to their email account and can be used to authenticate (The plugin will cache the session token to avoid having to re-authenticate every time they run the application).
```javascript
async void RequestEmailCode()
{
    Result result = await ModIOUnityAsync.RequestAuthenticationEmail("johndoe@gmail.com");
 
    if (result.Succeeded())
    {
        Debug.Log("Succeeded to send security code");
    }
    else
    {
        Debug.Log("Failed to send security code to that email address");
    }
}

async void SubmitCode(string userSecurityCode)
{
    Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(userSecurityCode);
 
    if (result.Succeeded())
    {
        Debug.Log("You have successfully authenticated the user");
    }
    else
    {
        Debug.Log("Failed to authenticate the user");
    }
}
```

### Get Mod profiles from the mod.io server
```javascript
async void Example()
{
    // create a filter to retreive the first ten mods for your game
    SearchFilter filter = new SearchFilter();
    filter.SetPageIndex(0);
    filter.SetPageSize(10);
    
    ResultAnd<ModPage> response = await ModIOUnityAsync.GetMods(filter);

    if (response.result.Succeeded())
    {
        Debug.Log("ModPage has " + response.value.modProfiles.Length + " mods");
    }
    else
    {
        Debug.Log("failed to get mods");
    }
}
```

##Submitting mods
You can also submit mods directly from the plugin. Refer to the documentation for methods such as ModIOUnity.CreateModProfile and ModIOUnity.UploadModfile.

Users can also submit mods directly from the mod.io website by going to your game profile page. Simply create an account and upload mods directly.

## Dependencies
The [mod.io](https://mod.io) Unity Plugin requires the functionality of two other open-source Unity plugins to run. These are included as libraries in the UnityPackage in the `Assets/Plugins/mod.io/ThirdParty` directory:
* Json.Net for improved Json serialization. ([GitHub Repo](https://github.com/SaladLab/Json.Net.Unity3D) || [Unity Asset Store Page](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347))
* SharpZipLib to zip and unzip transmitted files. ([GitHub Repo](https://github.com/icsharpcode/SharpZipLib))

## Benefits
mod.io offers the same core functionality as Steamworks Workshop (1 click mod installs in-game), plus mod hosting, moderation and all of the critical pieces needed. Where we differ is our approach to modding and the flexibility a REST API offers. For example: 

* Our API is not dependent on a client, platform or SDK, allowing you to run mod.io in many places such as your homepage and launchers.
* Designing a good mod browsing UI is hard, our plugin ships with a UI built in to save you a lot of effort and help your mods stand out.
* We don’t apply rules globally, so if you want to enable patronage, sales or other experimental features, reach out to discuss.
* Our platform is built by the super experienced ModDB.com team and is continually improving for your benefit.
* Your community can consume the mod.io API to build modding fan sites or discord bots if they want.
* Communicate and interact with your players, using our built-in emailer

## Large studios and Publishers
A private white label option is available to license, if you want a fully featured mod-platform that you can control and host in-house. [Contact us](mailto:developers@mod.io?subject=Whitelabel) to discuss.

## Contributions Welcome
Our Unity plugin is public and open source. Game developers are welcome to utilize it directly, to add support for mods in their games, or fork it for their games customized use. Want to make changes to our plugin? Submit a pull request with your recommended changes to be reviewed.

## Other Repositories
Our aim with [mod.io](https://mod.io), is to provide an [open modding API](https://docs.mod.io). You are welcome to [view, fork and contribute to our other codebases](https://github.com/modio) in use.
