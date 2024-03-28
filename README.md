---
id: unity-introduction
title: Unity Introduction
sidebar_label: Unity Introduction
slug: /unity-plugin/unity-introduction
sidebar_position: 0
---

<a href="https://mod.io"><img src="https://mod.io/images/branding/modio-logo-bluewhite.svg" alt="mod.io" width="360" align="right"/></a>
# mod.io Unity Plugin v2024.3.1
[![License](https://img.shields.io/badge/license-MIT-brightgreen.svg)](https://github.com/modio/modio-unity/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/389039439487434752.svg?label=Discord&logo=discord&color=7289DA&labelColor=2C2F33)](https://discord.mod.io)
[![Master docs](https://img.shields.io/badge/docs-master-green.svg)](https://go.mod.io/unity-docs)
[![Unity 3D](https://img.shields.io/badge/Unity-2020.3+-lightgrey.svg)](https://unity3d.com)

Welcome to the mod.io Unity Engine plugin repository!

mod.io enables game developers of all sizes to integrate user-generated content directly into their games quickly and easily. This includes hosting, user profiles and subscriptions, moderation tools, file delivery, and *more*: 

- Completely cross-platform: content uploaded on PC can be enjoyed by players on Xbox, Switch, PlayStation, VR and mobile
- One-click mod installs, synced to a single mod.io account across all platforms
- Independent API: integrate mod.io into your game's launcher or your homepage using our Embedded UGC Hub. It can also be used by community fan sites and Discord bots
- Founded by the [ModDB.com](https://moddb.com) team, with over two decades of experience in the UGC space
- Constantly evolving - we continue to work alongside our partners to iterate and improve our plugin support

The mod.io Unity Engine plugin is the simplest and fastest way to integrate UGC into your Unity **2020.3+** game. It handles all of the common tasks, allowing game developers to quickly and easily implement a solution that enables players to access and discover user-generated content for their games.

A custom built [ready-made UI](#browser-ui) for mod discovery is included, along with installation and collection management, and a full-featured [C# interface](#getting-started) which connects to the [mod.io REST API](https://docs.mod.io).

## Platform Support
You can view the Monetization Endpoints [here.](./Marketplace.md)

|Platform       | Support                 | Documentation                                                                              |
| ------------- |-------------------------|--------------------------------------------------------------------------------------------|
|Windows        | Yes                     |                                                                                            |
|Windows (GDK)  | Yes ([Contact us](#game-studios-and-publishers)) |                                                                                            |
|XBox (GDK)     | Yes ([Contact us](#game-studios-and-publishers)) | [Marketplace](./Platform/Gamecore/Gamecore Marketplace.md)                                 |
|PlayStation 4  | Yes ([Contact us](#game-studios-and-publishers)) | [Marketplace](./Platform/Playstation/ModIO.Implementation.Platform/PS4/PS4 Marketplace.md) |
|PlayStation 5  | Yes ([Contact us](#game-studios-and-publishers)) | [Marketplace](./Platform/Playstation/ModIO.Implementation.Platform/PS5/PS5 Marketplace.md) |
|Nintendo Switch| Yes ([Contact us](#game-studios-and-publishers)) |                                                                                            |
|Linux          | Yes                     |                                                                                            |
|macOS          | Yes                     |                                                                                            |


## Game Studios and Publishers
If you need assistance with first-party approval, or require a private, white-label UGC solution. [Contact us](mailto:developers@mod.io)!

## Contributions Welcome
Our Unity plugin is public and open source. Game developers are welcome to utilize it as-is or fork it for their game's specific requirements.  

Want to make changes to our plugin? Submit a pull request, and we'll review your recommended changes! Our goal at [mod.io](https://mod.io) is an [open modding API](https://docs.mod.io), and you're encouraged to view, fork and contribute to [all of our codebases](https://github.com/modio)!

## Installation

> [!WARNING]  
> If you have a previous version of the plugin installed, it is _highly_ recommended to delete it before updating to a later version.

1. Install the *Newtonsoft Json* plugin using the Package Manager.
   - If your Unity Package Manager does not contain Newtonsoft Json, follow the instructions [here](https://github.com/applejag/Newtonsoft.Json-for-Unity/wiki/Install-official-via-UPM#installing-the-package-via-upm-window) to find the installation method for your Unity version.
2. Download and install the plugin using one of the following methods:
   - Using the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/mod-browser-manager-by-mod-io-138866) and Package Manager.
   - Download the `.unitypackage` directly from the [Releases page](https://github.com/modio/modio-unity/releases).
   - Download an archive of the code using GitHub's download feature, and unpack it in your project's `Assets/Plugins` directory.
3. Restart Unity, to ensure it recognises the new assembly definitions.

> [!NOTE]  
> If you receive errors due to conflicting libraries after installing the plugin, remove any duplicates from `Assets/Plugins/mod.io/ThirdParty`.

## Setup

The first thing you'll need to do is [create a game profile](https://mod.io/g/add) on mod.io (or our [private test environment](https://test.mod.io/g/add)).

> [!IMPORTANT]  
> You'll need your `game ID` and `API key` for the following steps.

1. Ensure you have installed the plugin using the [installation instructions](#installation) above.
2. In Unity, select the mod.io *config file* by navigating to `Tools -> mod.io -> Edit Settings`.
3. In the Inspector under *Server Settings*, enter your `game ID` and `API key`.
4. Use the *Insert URL* buttons to set the `server URL` depending on where you created your game profile earlier.

> [!WARNING]  
> Deselect the config file before entering Play mode. A known Unity bug can cause the Editor to crash in Unity 2019-2021.

Your setup is now complete. The following sections will guide you through getting your mod.io integration up and running quickly.

If you have any questions or need some help join our [Discord](https://discord.mod.io) server.

## Quick Start

The mod.io Unity Engine plugin comes with a prebuilt UI, a drop-in, instant solution for browsing and installing your game's mods.

If you want to skip implementing your own UI, head to the [Browser UI](#browser-ui) section for setup and usage instructions. However, we recommend following the guide below to better understand how the plugin works.

## Getting Started

In the following section, we will walk through implementing some of the most common functions of the mod.io Unity Engine plugin. We recommend reading this step-by-step guide to ensure you understand how everything works, but you can find the resulting class for reference [here](#complete-class).

### Video Tutorial

If you prefer a video tutorial, click the image below to view it on YouTube:

<a href="https://www.youtube.com/watch?v=pmECrkdzHzQ">
   <img src="https://i.ytimg.com/vi/pmECrkdzHzQ/maxresdefault.jpg" alt="mod.io" width="49%"/>
</a>

### Initial Setup

First, let's create a new `MonoBehaviour` called `ModIOExample.cs` that will contain all of our example functionality:

```csharp
using UnityEngine;

public class ModIOExample : MonoBehaviour
{
    // TODO: Keep reading the Getting Started guide
}
```

Once you've created the above class:

1. Create a new `Scene`.
2. In that scene, create an `Empty Game Object` (name it anything you'd like).
3. Add the `ModIOExample` component to your `GameObject`.
4. Save the scene.

### Initialization

> [!IMPORTANT]  
> The plugin relies on the *config file* that is configured during the [setup instructions](#setup) above. Please ensure you have completed all of those steps before proceeding.

Before the plugin can be used, it needs to be initialized for the current player. This usually only needs to happen once, so let's implement Unity's `Start` method in our `ModIOExample.cs` file:

```csharp
using ModIO; // Add this to the top of your class

void Start()
{
    Result result = ModIOUnity.InitializeForUser("default");
    if (!result.Succeeded())
        return;

    Debug.Log("ModIO plugin initialized!");
}
```

The value passed into `InitializeForUser` is important. We've used *"default"* here, however, if your game allows for multiple players on the same installation, you should instead use a unique identifier per player. This allows the plugin to cache authentication and mod-subscriptions on a per-player basis.

Now, return to your scene in Unity, enter Play mode and you should see the logged success message.

## Authentication

> [!NOTE]  
> This guide uses `ModIOUnityAsync` wherever possible. However, you can find callback equivalents to every method in `ModIOUnity` if you prefer.

Most of the API’s functionality requires player authentication. The plugin offers a large range of SSO (single-sign on) authentication options, including Steam, Xbox, Google, PlayStation, and more. We strongly recommend using these options as they provide a frictionless user experience and don't require multiple steps.

For now, let's start with a simple email authentication to allow us full access.

> [!NOTE]  
> While creating the UI layout referenced below is outside the scope of this guide, there are great Unity UI tutorials available. You can, however, use the image below as a guide for the elements required to achieve the same functionality:
>
> ![Auth Form Example](Example/Images/auth_form_example.png)

With your UI created, let's add our authentication functionality: 

```csharp
using UnityEngine.UI; // Add this to the top of your class

[SerializeField] InputField authInput;
[SerializeField] Button authRequest;
[SerializeField] Button authSubmit;
   
void Start()
{
    // Initialization ...

    OnInit();
}

async void OnInit()
{
    Result result = await ModIOUnityAsync.IsAuthenticated();
    if (result.Succeeded())
    {
        OnAuth();

        return;
    }
    
    // You can assign these using the Inspector if you prefer
    authRequest.onClick.AddListener(RequestAuthCode);
    authSubmit.onClick.AddListener(SubmitAuthCode);
}
   
async void RequestAuthCode()
{
    Result result = await ModIOUnityAsync.RequestAuthenticationEmail(authInput.text);
    if (!result.Succeeded())
    {
        Debug.LogError($"RequestAuthenticationEmail failed: {result.message}");

        return;
    }

    Debug.Log($"Authentication email sent to: {authInput.text}");

    authInput.text = string.Empty;
}

async void SubmitAuthCode()
{
    Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(authInput.text);
    if (!result.Succeeded())
    {
        Debug.LogError($"SubmitEmailSecurityCode failed: {result.message}");

        return;
    }

    OnAuth();
}
   
async void OnAuth()
{
    ResultAnd<UserProfile> result = await ModIOUnityAsync.GetCurrentUser();
    if (!result.result.Succeeded())
    {
        Debug.LogError($"GetCurrentUser failed: {result.result.message}");
    }

    Debug.Log($"Authenticated user: {result.value.username}");
}
```

> [!IMPORTANT]  
> Don't forget to assign the fields in the Inspector!

If you've implemented the above correctly, you should now be able to:

1. Start Play mode in Unity
2. Enter your email address in the input field and press the `authRequest` button
3. Retrieve the authorization code from your inbox
4. Enter the authorization code into the input field and press the `authSubmit` button
5. See the logged authentication message

> [!NOTE]
> If there is no mod.io account associated with the provided email address, one will automatically be created.

There is something worth highlighting: if you restart Play mode, you'll see the logged authentication message again almost immediately. This is the result of two separate factors:

- The value passed to `InitializeForUser` during the [initialization section](#initialization) needs to have been used when a user successfully authenticated.
- At the beginning of `OnInit()`, we check to see if we are already authenticated, and if so move straight to `OnAuth()`.

If you change the initialization value (currently *"default"*), you will no longer receive the authenticated log. This functionality can enable support for multiple players; separately tracking authentication states and mod-subscriptions. However, as mentioned previously, most games can pass a constant value if they only ever expect one player on the device.

> [!NOTE]  
> If your email provider supports it, you can use plus-addressing to test multiple users with a single email address:
> ```
> john.smith+test1@gmail.com
> john.smith+test2@gmail.com
> john.smith+test3@gmail.com
> ```

## Adding Mods

> [!NOTE]  
> Among a range of other functionality, players can use the mod.io website for creating, modifying, and removing mods for your game.
>
> In this section, we're going to add mods using the plugin and API. Feel free to skip this section if you'd prefer to use the web interface.

Before we can interact with your game's mods via the API, we're going to need to create some test mods. We’ll start by adding some functionality that checks to see if your game has any mods. If it doesn't then we'll upload some using the API:

```csharp
using System.Threading.Tasks; // Add this to the top of your class

async void OnAuth()
{
    // Authenticated ...
    
    await AddModsIfNone();
}

async Task AddModsIfNone()
{
    // This section ensures we only upload our mods once. Don't worry too much
    // about the specifics for now, we will introduce SearchFilters and GetMods 
    // properly later on.
    ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
    if (!resultAnd.result.Succeeded())
    {
        Debug.LogError($"GetMods failed: {resultAnd.result.message}");

        return;
    }

    if (resultAnd.value.modProfiles.Length != 0)
    {
        Debug.Log($"{resultAnd.value.modProfiles.Length} mods found. Not adding mods");

        return;
    }

    // TODO: Keep reading the Getting Started guide
}
```

### Generating Dummy Mods

> [!NOTE]  
> This section is going to generate some dummy mods for use throughout the rest of this guide. If you already have mods or test files ready to upload, you can skip to the [uploading mods](#uploading-mods) section.

<details>

<summary><i>Click to expand</i></summary>
<br>
Let's generate a few dummy mods for you to use for testing. At a minimum, a mod requires the following:

- A name
- A summary
- A logo (image file with a minimum resolution of 512x288)
- At least one file

We'll use a third-party API to generate a logo for each of your mods, and we'll create a temporary folder and dummy file in each in your Unity project's directory:

> [!NOTE]  
> Don't worry if you don't understand the code below. Its only job is to generate our dummy mods, and it doesn't have any relation to the plugin! If you'd prefer to create your own dummy mods, skip to the [uploading mods](#uploading-mods) section!

> [!WARNING]  
> The following code is going to generate a handful of 10-100 MB files, the size of which will give us enough time to show download progress later on. Ensure you have some free space available in your project directory.

```csharp
using System.IO; // Add these to the top of your class
using UnityEngine.Networking;

// Reusing a single byte-array is a small memory-conscious
// optimization for when we are generating our dummy files.
static readonly byte[] Megabyte = new byte[1024 * 1024];
static readonly Random RandomBytes = new Random();

async Task AddModsIfNone()
{
    // Return if any mods exist ...
    
    DummyModData[] mods =
    {
        await GenerateDummyMod("Cool Weapon", "A really cool weapon.", "24466B", "FDA576", 10),
        await GenerateDummyMod("Funny Sound Pack", "You'll laugh a lot using this.", "B85675", "633E63", 50),
        await GenerateDummyMod("Klingon Language Pack", "tlhIngan Hol Dajatlh'a'?", "93681C", "FFEAD0", 1),
        await GenerateDummyMod("Ten New Missions", "Ported from the sequel to the prequel!", "FDA576", "D45B7A", 99),
    };
}

async Task<DummyModData> GenerateDummyMod(string name, string summary, string backgroundColor, string textColor, int megabytes)
{
    Debug.Log($"Writing temporary mod file: {name}");

    string path = Path.Combine(Application.dataPath, $"../_temp_dummy_mods/{name}");
    Directory.CreateDirectory(path);

    using (FileStream fs = File.OpenWrite(Path.Combine(path, $"{name}.dummy")))
    {
        for (int i = 0; i < megabytes; i++)
        {
            RandomBytes.NextBytes(Megabyte);
            await fs.WriteAsync(Megabyte, 0, Megabyte.Length);
        }
    }

    return new DummyModData(
        name,
        summary,
        await GenerateLogo(name.Replace(' ', '+'), backgroundColor, textColor),
        path
    );
}

// Uses a third-party API to generate a logo for each
// mod, adding some variety when we display them later
async Task<Texture2D> GenerateLogo(string text, string backgroundColor, string textColor)
{
    UnityWebRequest request = UnityWebRequestTexture.GetTexture($"https://placehold.co/512x288/{backgroundColor}/{textColor}.png?text={text}");
    request.SendWebRequest();

    while (!request.isDone)
        await Task.Yield();

    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError($"GenerateLogo failed: {request.error}");

        return null;
    }

    return DownloadHandlerTexture.GetContent(request);
}

readonly struct DummyModData
{
    public readonly string name;
    public readonly string summary;
    public readonly Texture2D logo;
    public readonly string path;

    public DummyModData(string name, string summary, Texture2D logo, string path)
    {
        this.name = name;
        this.summary = summary;
        this.logo = logo;
        this.path = path;
    }
}
```

</details>

### Uploading Mods

Uploading mods is a two-step process:

1. We need to create a *Mod Profile*. This is essentially the mod's page or listing. It contains all of a mod's metadata, including its files.
   - Creating a *Mod Profile* requires an API call (`CreateModProfile`), which will return a `mod id`.
2. We can upload our files to the associated *Mod Profile* with our newly created `mod id`.

Let's add a method that handles both steps:

> [!NOTE]  
> The following code takes advantage of `ModIOUnity.GetCurrentUploadHandle`, which can be used for obtaining the current upload progress. This isn't required, and you can use `await ModIOUnityAsync.UploadModFile(details)` instead if you prefer. 

```csharp
async Task UploadMod(string name, string summary, Texture2D logo, string path)
{
    Debug.Log($"Starting upload: {name}");

    ModProfileDetails details = new ModProfileDetails
    {
        name = name,
        summary = summary,
        logo = logo,
    };

    ResultAnd<ModId> resultCreate = await ModIOUnityAsync.CreateModProfile(ModIOUnity.GenerateCreationToken(), details);
    if (!resultCreate.result.Succeeded())
    {
        Debug.LogError($"CreateModProfile failed: {resultCreate.result.message}");

        return;
    }

    ModfileDetails modFile = new ModfileDetails
    {
        modId = resultCreate.value,
        directory = path,
    };

    float progress = 0f;

    Task<Result> taskUpload = ModIOUnityAsync.UploadModfile(modFile);
    while (!taskUpload.IsCompleted)
    {
        ProgressHandle progressHandle = ModIOUnity.GetCurrentUploadHandle();

        if (!Mathf.Approximately(progressHandle.Progress, progress))
        {
            progress = progressHandle.Progress;
            Debug.Log($"Uploading: {name} ({Mathf.RoundToInt(progress * 100)}%)");
        }

        await Task.Delay(1000);
    }

    if (!taskUpload.Result.Succeeded())
    {
        Debug.LogError($"UploadModfile failed: {taskUpload.Result.message}");

        return;
    }

    Debug.Log($"Finished upload: {name}");
}
```

All that's left now is to feed some mods to our brand new `UploadMod` method. After we test to see if any mods exist in `AddModsIfNone`, we will iterate our list of mods and upload them:

> [!IMPORTANT]  
> If you didn't generate dummy mods in the previous section, modify the below to suit your mod files.

```csharp
async Task AddModsIfNone()
{
    // Return if any mods exist ...
    
    DummyModData[] mods =
    {
        // ...
    };
    
    foreach (DummyModData mod in mods)
    {
        await UploadMod(mod.name, mod.summary, mod.logo, mod.path);
        // Directory.Delete(mod.path, true); // Uncomment if you generated dummy mods
    }
}
```

> [!NOTE]  
> When uploading a mod, the plugin expects a directory (for each mod) that it will compress before uploading. You do <b><u>not</u></b> need to zip your files before uploading.

That’s it! Enter Play mode now and, after authentication, the Unity console should come to life with the upload progress of your mods. If you're eager, you can view the mods as soon as they're uploaded by going to your game's mod.io page and using the web interface.

## Searching for Mods

Searching for mods is a simple task — we've actually seen it already in the [adding mods](#adding-mods) section. To get a list of all<sup>1</sup> available mods you can use the `GetMods` method:

```csharp
using System; // Add this to the top of your class

async Task<ModProfile[]> GetAllMods()
{
    ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
    if (!resultAnd.result.Succeeded())
    {
        Debug.LogError($"GetMods failed: {resultAnd.result.message}");

        return Array.Empty<ModProfile>();
    }

    return resultAnd.value.modProfiles;
}
```

> [!IMPORTANT]  
> A *Mod Profile* is a read-only snapshot of the state of a mod. It is <u>not</u> a unique or dynamic class. Compare `ModProfile.id` if you want to determine whether two Mod Profiles represent the same mod. 

If we add the above to our example class, and then head back up to our `OnAuth` method we can quickly log a list of all<sup>1</sup> of our available mods:

```csharp
using System.Linq; // Add this to the top of your class

ModProfile[] allMods;

async void OnAuth()
{
    // ...
    
    allMods = await GetAllMods();
    Debug.Log($"Available mods:\n{string.Join("\n", allMods.Select(mod => $"{mod.name} (id: {mod.id.id})"))}");
}
```

We write *all<sup>1</sup>* because while using the default *Search Filter* settings will return all of *your* mods, this is only because you don't have many. This brings us to *Search Filters*. 

### Search Filters

The maximum number of results returned can be set in the Search Filter using its `SetPageSize()` method. However, the default value of 100 is also the limit. In order to return later results, you can use the Search Filter's `SetPageIndex()` method.

This is fairly simple in practice and is explained best with the following snippet:

```csharp
var searchFilter = new SearchFilter();
searchFilter.SetPageSize(10);
searchFilter.SetPageIndex(0); // Will return results 1-10
searchFilter.SetPageIndex(1); // Will return results 11-20
searchFilter.SetPageIndex(2); // Will return results 21-30

// You can also set pageIndex and pageSize using the constructor
new SearchFilter(0, 10); // Will return results 1-10
new SearchFilter(1, 10); // Will return results 11-20
new SearchFilter(2, 10); // Will return results 21-30
```

> [!NOTE]  
> Search Filters have a number of options for filtering and ordering your results. See the [documentation](https://sdkdocs.mod.io/unity/) (or use code completion in your IDE) for its available options.

### Downloading Images

More specifically: downloading a *Mod Profile's* images. We'll cover subscribing to and installing mods soon.

A common feature when listing mods is to display an image along with its name and summary. Metadata images such as logos, screenshots, and avatars don't require subscribing to a mod to view them, and can be downloaded separately from a mod's in-game files.

As we know, [all mods have a logo](#adding-mods). So let's write a short method that selects a random mod, downloads its logo and displays it alongside its name:

> [!NOTE]  
> Below is a screenshot of the UI we're using to utilize the method. You can use this as a guide for your own or display the result however you'd like!
>
> ![Auth Form Example](Example/Images/random_mod_example.png)

```csharp
[SerializeField] Text randomName;
[SerializeField] Image randomLogo;

async void SetRandomMod()
{
    ModProfile modProfile = allMods[UnityEngine.Random.Range(0, allMods.Length)];

    randomName.text = modProfile.name;

    ResultAnd<Texture2D> resultAnd = await ModIOUnityAsync.DownloadTexture(modProfile.logoImage_320x180);
    if (!resultAnd.result.Succeeded())
    {
        Debug.LogError($"DownloadTexture failed: {resultAnd.result.message}");

        return;
    }

    Texture2D logo = resultAnd.value;
    randomLogo.sprite = Sprite.Create(logo, new Rect(0, 0, logo.width, logo.height), Vector2.zero);
}
```

> [!WARNING]  
> The code above relies on `allMods`, which is set in the first [searching for mods](#searching-for-mods) section. **Ensure that `allMods` has been set before running this method.**

This method is downloading the smallest version of the logo, `logoImage_320x180`. However, Mod Profiles have a number of sizes for each image. See the [documentation](https://sdkdocs.mod.io/unity/) (or use code completion in your IDE) to view available options. 

## Getting Subscribed Mods

We're going to cover mod subscriptions in what will seem like a backward way. First, we'll learn how to get a list of our subscribed mods, then we'll learn how to subscribe to a mod.

The reason we do it this way is because subscribed mods are cached locally, so we avoid redundant API calls if we're already subscribed. 

With that in mind, the rest is quite straightforward:

```csharp
static ModProfile[] GetSubscribedMods()
{
    SubscribedMod[] subscribed = ModIOUnity.GetSubscribedMods(out Result result);
    if (!result.Succeeded())
    {
        Debug.LogError($"GetSubscribedMods failed: {result.message}");

        return Array.Empty<ModProfile>();
    }

    return subscribed.Select(mod => mod.modProfile).ToArray();
}
```

Getting the user's subscribed mods first requires a call to `FetchUpdates()`. This method synchronises the cached local state with mod.io (subscriptions, ratings, etc.). This is an expensive method and usually only needs to be called on authentication, as local subscription changes are reflected automatically. However if, for example, you change subscriptions using the web interface, they won't be reflected in your game until `FetchUpdates()` has been called.

We'll make use of the above from our `OnAuth()` method, after we log all available mods. First, we call `FetchUpdates()` to synchronize our local state, and then we can get an array of our subscribed mods:

```csharp
async void OnAuth()
{
    // ...
    
    Result resultUpdate = await ModIOUnityAsync.FetchUpdates(); // Synchronize our local state
    if (!resultUpdate.Succeeded())
    {
        Debug.LogError($"FetchUpdates failed: {resultUpdate.message}");
    }

    ModProfile[] subscribedMods = GetSubscribedMods();
    Debug.Log($"Subscribed mods:\n{(subscribedMods.Length > 0 ? string.Join("\n", subscribedMods.Select(mod => $"{mod.name} (id: {mod.id.id})")) : "None")}");
}
```

> [!IMPORTANT]  
> A *Mod Profile* is a read-only snapshot of the state of a mod. It is <u>not</u> a unique or dynamic class. Compare `ModProfile.id` if you want to determine whether two Mod Profiles represent the same mod.

For now, you should see "*Subscribed mods: None*" in the log if you enter Play mode. But we're going to rectify that *very* soon.

## Subscribing to Mods

> [!NOTE]  
> The web interface at your game's mod.io page can also be used to subscribe to mods. However, you'll need to exit and enter Play mode to see the changes, as `FetchUpdates()` needs to be run to synchronise the local state.

Subscribing to mods is very simple. The following code adds a check to see if we're already subscribed and then logs the name of the mod once the subscription is successful. The only *actual* requirement is the call to `SubscribeToMod()`:

```csharp
async Task SubscribeToMod(ModId modId, ModProfile[] subscribed)
{
    if (subscribed.Any(mod => mod.id == modId))
        return;

    Result result = await ModIOUnityAsync.SubscribeToMod(modId);
    if (!result.Succeeded())
    {
        Debug.LogError($"SubscribeToMod failed: {result.message}");

        return;
    }

    Debug.Log($"Subscribed to mod: {allMods.First(mod => mod.id == modId).name}");
}
```

> [!NOTE]  
> Inefficient code is used here to reduce the sample's complexity. Translating subscriptions to a `List<ModId>` and keeping a `Dictionary<ModId, ModProfile>` would be more efficient solutions for these look-ups.

To test it out:

1. Enter Play mode, and in the "Available mods" log, locate the "Ten New Missions" id.
2. In your `OnAuth()` method, after we log all subscribed mods add the following line (replace `YOUR_MOD_ID` with the id from step 1):
   ```csharp
   async void OnAuth()
   {
       // ...
       
       await SubscribeToMod(new ModId(YOUR_MOD_ID), subscribedMods);
   }
   ```
3. Restart Play mode and you should see "Ten New Missions" in your "Subscribed mods" log!

## Installing Mods

Now, the moment we've all been waiting for. Downloading, installing, updating, and deleting mods are all handled automatically by the plugin. However, it requires both an authenticated user and to opt-in to the behaviour. The latter is done via a simple `EnableModManagement()` call. This method requires a delegate argument that exposes us to the various events that mod management emits.

In the following code we're going to enable mod management and also log the download progress when a mod is being downloaded:

```csharp
string downloadName = "";
float downloadProgress;

void EnableModManagement()
{
    void HandleModManagementEvent(ModManagementEventType eventType, ModId modId, Result eventResult)
    {
        switch (eventType)
        {
            case ModManagementEventType.DownloadStarted:
                downloadName = allMods.First(mod => mod.id == modId).name;
                Debug.Log($"Downloading {downloadName}");
                break;
            case ModManagementEventType.Downloaded:
                Debug.Log($"Downloaded {downloadName}");
                downloadName = string.Empty;
                break;
            case ModManagementEventType.DownloadFailed:
                Debug.Log($"Download failed {downloadName}");
                downloadName = string.Empty;
                break;
        }
    }

    ModIOUnity.EnableModManagement(HandleModManagementEvent);
}

void Update()
{
    if (downloadName.Length == 0)
        return;

    ProgressHandle progress = ModIOUnity.GetCurrentModManagementOperation();

    if (Mathf.Approximately(progress.Progress, downloadProgress))
        return;

    downloadProgress = progress.Progress;
    Debug.Log($"Downloading {downloadName} ({Mathf.RoundToInt(downloadProgress * 100)}%)");
}
```

In a real implementation, you'll likely track the `modId`'s download and install progress separately to display in your UI. But, this should give you an idea of what's possible with the mod management feature.

> [!NOTE]  
> There are a number of mod management events available. See the [documentation](https://sdkdocs.mod.io/unity/) (or use code completion in your IDE) for a complete list.

## Using Mods

We’re nearing the end now. You've [initialized](#initialization). You've [authenticated](#authentication). You've [uploaded](#adding-mods). You've [searched](#searching-for-mods). You've [subscribed](#subscribing-to-mods). You've [installed](#installing-mods). It's all led to this single question:

*"How do I find installed mods?"*

The answer is very straight forward: `GetInstalledModsForUser()`. Using this is as simple as expected:

```csharp
void LogInstalledModsForUser()
{
    UserInstalledMod[] installedMods = ModIOUnity.GetInstalledModsForUser(out Result result);
    if (!result.Succeeded())
    {
        Debug.LogError($"GetInstalledModsForUser failed: {result.message}");

        return;
    }

    Debug.Log($"Installed mods:\n{(installedMods.Length > 0 ? string.Join("\n", installedMods.Select(mod => $"{mod.modProfile.name} ({mod.directory})")) : "None")}");
}
```

We're currently logging each installed mod and the path to its files (`UserInstalledMod.directory`). However, *you* are only limited by how you want to utilize user-generated content. A mod's installation directory is exactly the same as when we uploaded it: uncompressed and ready for action.

That’s it, we’re done. The time has come to build a bridge to your creator community using mod.io.

Please join us on our [Discord server](https://discord.mod.io) if you have any questions or need some help.

## Complete Class

> [!NOTE]  
> You can also find the following class (along with an example scene) in `Assets/Plugins/mod.io/Example`. 

<details>

<summary><i>Click to expand</i></summary>

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ModIO
{
    public class ModIOExample : MonoBehaviour
    {
        // Generating Dummy Mods
        static readonly byte[] Megabyte = new byte[1024 * 1024];
        static readonly System.Random RandomBytes = new System.Random();

        [Header("Authentication")]
        [SerializeField] GameObject authContainer;
        [SerializeField] InputField authInput;
        [SerializeField] Button authRequest;
        [SerializeField] Button authSubmit;

        // Downloading Images
        [Header("Random Mod")]
        [SerializeField] GameObject randomContainer;
        [SerializeField] Text randomName;
        [SerializeField] Image randomLogo;
        [SerializeField] Button randomButton;

        // Searching for Mods
        ModProfile[] allMods;

        // Installing Mods
        string downloadName = "";
        float downloadProgress;

        void Awake()
        {
            randomContainer.SetActive(false);
        }

        #region Initialization

        void Start()
        {
            Result result = ModIOUnity.InitializeForUser("default");
            if (!result.Succeeded())
                return;

            Debug.Log("ModIO plugin initialized!");

            OnInit();
        }

        async void OnInit()
        {
            Result result = await ModIOUnityAsync.IsAuthenticated();
            if (result.Succeeded())
            {
                OnAuth();

                return;
            }

            authRequest.onClick.AddListener(RequestAuthCode);
            authSubmit.onClick.AddListener(SubmitAuthCode);
        }

        #endregion

        #region Authentication

        async void RequestAuthCode()
        {
            Result result = await ModIOUnityAsync.RequestAuthenticationEmail(authInput.text);
            if (!result.Succeeded())
            {
                Debug.LogError($"RequestAuthenticationEmail failed: {result.message}");

                return;
            }

            Debug.Log($"Authentication email sent to: {authInput.text}");

            authInput.text = string.Empty;
        }

        async void SubmitAuthCode()
        {
            Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(authInput.text);
            if (!result.Succeeded())
            {
                Debug.LogError($"SubmitEmailSecurityCode failed: {result.message}");

                return;
            }

            OnAuth();
        }

        #endregion

        async void OnAuth()
        {
            ResultAnd<UserProfile> result = await ModIOUnityAsync.GetCurrentUser();
            if (!result.result.Succeeded())
            {
                Debug.LogError($"GetCurrentUser failed: {result.result.message}");
            }

            Debug.Log($"Authenticated user: {result.value.username}");

            authContainer.SetActive(false);

            await AddModsIfNone();

            allMods = await GetAllMods();
            Debug.Log($"Available mods:\n{string.Join("\n", allMods.Select(mod => $"{mod.name} (id: {mod.id.id})"))}");

            randomButton.onClick.AddListener(SetRandomMod);
            randomContainer.SetActive(true);
            SetRandomMod();

            ModProfile[] subscribedMods = await GetSubscribedMods();
            Debug.Log($"Subscribed mods:\n{(subscribedMods.Length > 0 ? string.Join("\n", subscribedMods.Select(mod => $"{mod.name} (id: {mod.id.id})")) : "None")}");

            if (allMods.Length > 0)
            {
                int index = Array.FindIndex(allMods, mod => mod.name == "Ten New Missions");
                if (index != -1)
                    await SubscribeToMod(allMods[index].id, subscribedMods);
                else
                    Debug.Log("Couldn't find Ten New Missions mod, not subscribing");
            } else
                Debug.Log("No mods found, not subscribing");

            EnableModManagement();

            LogInstalledModsForUser();
        }

        #region Adding Mods

        async Task AddModsIfNone()
        {
            ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"GetMods failed: {resultAnd.result.message}");

                return;
            }

            if (resultAnd.value.modProfiles.Length != 0)
            {
                Debug.Log($"{resultAnd.value.modProfiles.Length} mods found. Not adding mods");

                return;
            }

            DummyModData[] mods =
            {
                await GenerateDummyMod("Cool Weapon", "A really cool weapon.", "24466B", "FDA576", 10),
                await GenerateDummyMod("Funny Sound Pack", "You'll laugh a lot using this.", "B85675", "633E63", 50),
                await GenerateDummyMod("Klingon Language Pack", "tlhIngan Hol Dajatlh'a'?", "93681C", "FFEAD0", 1),
                await GenerateDummyMod("Ten New Missions", "Ported from the sequel to the prequel!", "FDA576", "D45B7A", 99),
            };

            foreach (DummyModData mod in mods)
            {
                await UploadMod(mod.name, mod.summary, mod.logo, mod.path);
                Directory.Delete(mod.path, true);
            }
        }

        #endregion

        #region Uploading Mods

        static async Task UploadMod(string name, string summary, Texture2D logo, string path)
        {
            Debug.Log($"Starting upload: {name}");

            ModProfileDetails details = new ModProfileDetails
            {
                name = name,
                summary = summary,
                logo = logo,
            };

            ResultAnd<ModId> resultCreate = await ModIOUnityAsync.CreateModProfile(ModIOUnity.GenerateCreationToken(), details);
            if (!resultCreate.result.Succeeded())
            {
                Debug.LogError($"CreateModProfile failed: {resultCreate.result.message}");

                return;
            }

            ModfileDetails modFile = new ModfileDetails
            {
                modId = resultCreate.value,
                directory = path,
            };

            float progress = 0f;

            Task<Result> taskUpload = ModIOUnityAsync.UploadModfile(modFile);
            while (!taskUpload.IsCompleted)
            {
                ProgressHandle progressHandle = ModIOUnity.GetCurrentUploadHandle();

                if (!Mathf.Approximately(progressHandle.Progress, progress))
                {
                    progress = progressHandle.Progress;
                    Debug.Log($"Uploading: {name} ({Mathf.RoundToInt(progress * 100)}%)");
                }

                await Task.Delay(1000);
            }

            if (!taskUpload.Result.Succeeded())
            {
                Debug.LogError($"UploadModfile failed: {taskUpload.Result.message}");

                return;
            }

            Debug.Log($"Finished upload: {name}");
        }

        #endregion

        #region Searching for Mods

        async Task<ModProfile[]> GetAllMods()
        {
            ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"GetMods failed: {resultAnd.result.message}");

                return Array.Empty<ModProfile>();
            }

            return resultAnd.value.modProfiles;
        }

        #endregion

        #region Downloading Images

        async void SetRandomMod()
        {
            ModProfile modProfile = allMods[UnityEngine.Random.Range(0, allMods.Length)];

            randomName.text = modProfile.name;

            ResultAnd<Texture2D> resultAnd = await ModIOUnityAsync.DownloadTexture(modProfile.logoImage_320x180);
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"DownloadTexture failed: {resultAnd.result.message}");

                return;
            }

            Texture2D logo = resultAnd.value;
            randomLogo.sprite = Sprite.Create(logo, new Rect(0, 0, logo.width, logo.height), Vector2.zero);
        }

        #endregion

        #region Getting Subscribed Mods

        static async Task<ModProfile[]> GetSubscribedMods()
        {
            Result resultUpdate = await ModIOUnityAsync.FetchUpdates();
            if (!resultUpdate.Succeeded())
            {
                Debug.LogError($"FetchUpdates failed: {resultUpdate.message}");

                return Array.Empty<ModProfile>();
            }

            SubscribedMod[] subscribed = ModIOUnity.GetSubscribedMods(out Result resultSubscribed);
            if (!resultSubscribed.Succeeded())
            {
                Debug.LogError($"GetSubscribedMods failed: {resultSubscribed.message}");

                return Array.Empty<ModProfile>();
            }

            return subscribed.Select(mod => mod.modProfile).ToArray();
        }

        #endregion

        #region Subscribing to Mods

        async Task SubscribeToMod(ModId modId, ModProfile[] subscribed)
        {
            if (subscribed.Any(mod => mod.id == modId))
                return;

            Result result = await ModIOUnityAsync.SubscribeToMod(modId);
            if (!result.Succeeded())
            {
                Debug.LogError($"SubscribeToMod failed: {result.message}");

                return;
            }

            Debug.Log($"Subscribed to mod: {allMods.First(mod => mod.id == modId).name}");
        }

        #endregion

        #region Installing Mods

        void EnableModManagement()
        {
            void HandleModManagementEvent(ModManagementEventType eventType, ModId modId, Result eventResult)
            {
                switch (eventType)
                {
                    case ModManagementEventType.DownloadStarted:
                        downloadName = allMods.First(mod => mod.id == modId).name;
                        Debug.Log($"Downloading {downloadName}");
                        break;
                    case ModManagementEventType.Downloaded:
                        Debug.Log($"Downloaded {downloadName}");
                        downloadName = string.Empty;
                        break;
                    case ModManagementEventType.DownloadFailed:
                        Debug.Log($"Download failed {downloadName}");
                        downloadName = string.Empty;
                        break;
                }
            }

            ModIOUnity.EnableModManagement(HandleModManagementEvent);
        }

        void Update()
        {
            if (downloadName.Length == 0)
                return;

            ProgressHandle progress = ModIOUnity.GetCurrentModManagementOperation();

            if (Mathf.Approximately(progress.Progress, downloadProgress))
                return;

            downloadProgress = progress.Progress;
            Debug.Log($"Downloading {downloadName} ({Mathf.RoundToInt(downloadProgress * 100)}%)");
        }

        #endregion

        #region Using Mods

        static void LogInstalledModsForUser()
        {
            UserInstalledMod[] installedMods = ModIOUnity.GetInstalledModsForUser(out Result result);
            if (!result.Succeeded())
            {
                Debug.LogError($"GetInstalledModsForUser failed: {result.message}");

                return;
            }

            Debug.Log($"Installed mods:\n{(installedMods.Length > 0 ? string.Join("\n", installedMods.Select(mod => $"{mod.modProfile.name} ({mod.directory})")) : "None")}");
        }

        #endregion

        #region Generate Dummy Mods

        static async Task<DummyModData> GenerateDummyMod(string name, string summary, string backgroundColor, string textColor, int megabytes)
        {
            Debug.Log($"Writing temporary mod file: {name}");

            string path = Path.Combine(Application.dataPath, $"../_temp_dummy_mods/{name}");
            Directory.CreateDirectory(path);

            using (FileStream fs = File.OpenWrite(Path.Combine(path, $"{name}.dummy")))
            {
                for (int i = 0; i < megabytes; i++)
                {
                    RandomBytes.NextBytes(Megabyte);
                    await fs.WriteAsync(Megabyte, 0, Megabyte.Length);
                }
            }

            return new DummyModData(
                name,
                summary,
                await GenerateLogo(name.Replace(' ', '+'), backgroundColor, textColor),
                path
            );
        }

        static async Task<Texture2D> GenerateLogo(string text, string backgroundColor, string textColor)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture($"https://placehold.co/512x288/{backgroundColor}/{textColor}.png?text={text}");
            request.SendWebRequest();

            while (!request.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"GenerateLogo failed: {request.error}");

                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        readonly struct DummyModData
        {
            public readonly string name;
            public readonly string summary;
            public readonly Texture2D logo;
            public readonly string path;

            public DummyModData(string name, string summary, Texture2D logo, string path)
            {
                this.name = name;
                this.summary = summary;
                this.logo = logo;
                this.path = path;
            }
        }

        #endregion
    }
}
```

</details>

## Browser UI

> [!IMPORTANT]  
> The Browser UI relies on the *config file* that is configured during the [setup instructions](#setup) above. Ensure you have completed all of those steps before proceeding.

The Browser UI is incredibly simple to set up, and completely avoids the complexity that can come with building a full-featured mod browser: 

1. Drag the Browser UI prefab at `Assets/Plugins/mod.io/UI/Examples/ModIOBrowser` into your scene.
2. Call its `ModIOBrowser.Browser.Open()` method to show the browser in your scene.

If you want a fuller understanding of the plugin and its features, we recommend following the [getting started](#getting-started) guide above.

<p float="left">
  <img src="https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/a7f9360d-4837-4d6e-b5cb-db5544a27b8c_orig.png" width="49%" alt="Browser UI screenshot" />
  <img src="https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/b37c33d6-aaa1-49c5-a6fd-c4ae18627bd2_orig.png" width="49%" alt="Browser UI screenshot" /> 
</p>

# Marketplace
The mod.io SDK supports full monetization features, allowing you to sell a per-game virtual currency to your players that
they can use to purchase mods, with a share of the revenue split between creators and your studio. Every platform
requires specific setup for monetization features to work, with regards to the virtual currency configuration and API
calls. The following documentation walks you through the setup process and gives example usages. The mod.io monetization
features are enabled as part of the onboarding process on your game profile. Once that is setup, there is nothing
further you need to do for initialization in the SDK.

### Enable Marketplace in the Plugin
The first thing you will need to do is enable the marketplace toggle inside your config. This informs the plugin that your game profile has marketplace features enabled and will behave accordingly.

> [!NOTE]
> You can quickly access your config file by going to Tools > mod.io > Edit Settings 

### Get User Wallet Balance
Returns the current user's token balance
```csharp
async void GetUserWalletBalanceExample()
{
   var response = await ModIOUnityAsync.GetUserWalletBalance();
   if (response.result.Succeeded())
   {
       Debug.Log($"User has a balance of {response.value.balance } tokens.");
   }
   else
   {
       Debug.Log("failed to get balance");
   }
}
```

### Purchase Item
Purchases a mod using Tokens
```csharp
async void PurchaseItemExample()
{
    string idempotent = $"aUniqueKey";//Unique key used to prevent duplicate purchases
    ModId modId = new ModId(1234);//Mod to purchase
    int displayAmount = 12;//Price displayed to the player (Must match mod price)
    var response = await ModIOUnityAsync.PurchaseItem(modId, displayAmount, idempotent);
    if (response.result.Succeeded())
    {
        Debug.Log("Completed Purchase");
    }
    else
    {
        Debug.Log("failed to complete purchase");
    }
}
```

### Get User Purchases
Returns the current user's purchased Mods
```csharp
async void GetUserPurchases()
{
    ModIOUnity.GetPurchasedMods(out Result result);

    if (result.Succeeded())
    {
        foreach (var modProfile in response.value.modProfiles)
        {
            Debug.Log($"User owns mod with id: {modProfile.id}");
        }
    }
    else
    {
        Debug.Log("Failed to get purchases");
    }
}
```

### Syncing Purchases with Steam
If you setup SKUs for your users to purchase tokens through steam, you can sync these purchases with the mod.io server with the `SyncEntitlments` method. If a user purchases a token pack on steam, you can add the SKU used for that token pack on the Web by going to Admin > Monetization > Manage SKUs. Then when you use SyncEntitlments it will consume the purchased item and add those tokens to the user's wallet. Below is a very simple example of how to use the method.
> [!NOTE]
> SyncEntitlements will automatically be run when using ModIOUnity.FetchUpdates as well

```csharp
async void SyncEntitlements()
    {
        Result result = await ModIOUnityAsync.SyncEntitlements();
        if (response.result.Succeeded())
        {
            Debug.Log("Entitlements are synced");
        }
        else
        {
            Debug.Log("failed to sync");
        }
        }
```
> [!NOTE]
> This method will also work with console platforms
