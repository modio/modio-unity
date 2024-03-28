---
id: unity-steam-marketplace-example
title: Unity Steam Marketplace Example
sidebar_label: Unity Steam Marketplace Example
slug: /unity-plugin/unity-steam-marketplace-example
sidebar_position: 7
---

# <span style="color: #ff0f0f;">Steam Marketplace Setup</span>
The mod.io SDK supports full monetization features, allowing you sell a per-game virtual currency to your players that
they can use to purchase mods, with a share of the revenue split between creators and your studio. Every platform 
requires specific setup for monetization features to work, with regards to the virtual currency configuration and API 
calls. The following documentation walks you through the setup process and gives example usages. The mod.io monetization
features are enabled as part of the onboarding process on your game profile. Once that is setup, there is nothing 
further you need to do for initialization in the SDK.

# <span style="color: #ff0f0f;">Steam</span>
There are two packages that are typically used to integrate steamworks into Unity: Facepunch and Steamworks.Net.
Steamworks.Net is closer to the actual C++ implementation. Facepunch is more user friendly and takes advantage
of C# libraries.

### Sync Entitlements
Synchronizes purchased tokens through the Steam Store with the mod.io server. This must be called to update the user's
balance after purchasing token packs. This is done for you automatically in FetchUpdates() but can be called manually 
if needed.
```csharp
async void SyncEntitlements()
{
    var response = await ModIOUnityAsync.SyncEntitlements();
    if(response.result.Succeeded())
    {
        Debug.Log("Token packs have been added to user's balance");
    }
}
```

[Steam API Documentation](https://partner.steamgames.com/doc/home)

## <span style="color: #ff0f0f;">Facepunch</span>
[Facepunch Documentation](https://wiki.facepunch.com/steamworks/)

### Quick Start
- Head over to https://mod.io/g and login with an admin account
- Go to your game's dashboard and click the edit button in the top right corner.
- Click the Monetization dropdown and select settings.
- Enter your Steam app ID and Steam Publisher Key.
- Go back to Unity and Create a folder in the project under the Assets folder named "Facepunch"
- Download the Package zip file at [here.](https://github.com/Facepunch/Facepunch.Steamworks/releases/)
- Open the release zip file and copy the contents of the Unity file into your Facepunch folder.
- Set the Scripting runtime (Api Compatibility level) to .NET 4.x or higher in Project Settings.
- Open the SteamExample Scene
- Enable the "Facepunch Example" game object in the hierarchy.
- Under the "FacepunchExample" component, set your app id
- Select the "Store" game object under "Example Title Canvas"->"Title Options"
- Add an onClickEvent that references the "Facepunch Example" game object's OpenStoreWebOverlay function
- Open Steam and login with an account associated with your app id
- Enter play mode, select Store and the steam overlay will open!

## <span style="color: #ff0f0f;">Steamworks.Net</span>
[Steamworks.Net Documentation](https://steamworks.github.io/)

### Quick Start
- Head over to https://mod.io/g and login with an admin account
- Go to your game's dashboard and click the edit button in the top right corner.
- Click the Monetization dropdown and select settings.
- Enter your Steam app ID and Steam Publisher Key.
- Download the .unitypackage from the Releases page on GitHub [here.](https://github.com/rlabrecque/Steamworks.NET/releases)
- Import the unity package into the project
- Open the steam_appid.txt which now resides in the root of your Unity project and replace "480" with your own AppId
  (If it doesn't exist, just create a new text file named steam_appid.txt which only contains your app Id)
- Download and add the SteamManager class [here.](https://raw.githubusercontent.com/rlabrecque/SteamManager/master/SteamManager.cs)
- Open the SteamExample Scene
- Enable the "Steamworks Example" game object in the hierarchy.
- Under the "SteamworksExample" component, set your app id
- Select the "Store" game object under "Example Title Canvas"->"Title Options"
- Add an onClickEvent that references the "Steamworks Example" game object's OpenStoreWebOverlay function
- Open Steam and login with an account associated with your app id
- Enter play mode, select Store and the steam overlay will open!