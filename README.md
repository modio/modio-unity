<a href="https://mod.io"><img src="https://mod.io/images/branding/modio-logo-bluedark.svg" alt="mod.io" width="360" align="right"/></a>
# mod.io Unity Plugin v2023.7.1
[![License](https://img.shields.io/badge/license-MIT-brightgreen.svg)](https://github.com/modio/modio-unity/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/389039439487434752.svg?label=Discord&logo=discord&color=7289DA&labelColor=2C2F33)](https://discord.mod.io)
[![Master docs](https://img.shields.io/badge/docs-master-green.svg)](https://go.mod.io/unity-docs)
[![Unity 3D](https://img.shields.io/badge/Unity-2020.3+-lightgrey.svg)](https://unity3d.com)

Welcome to the mod.io Unity Engine plugin repository. It allows game developers to host and automatically install user-created mods in their games which use **Unity 2020.3** or newer. It provides a UI for mod discovery, installation and collection management, and a C# interface which connects to the [mod.io REST API](https://docs.mod.io).

## Watch the video tutorial
<a href="https://www.youtube.com/watch?v=pmECrkdzHzQ"><img src="https://i.ytimg.com/vi/pmECrkdzHzQ/maxresdefault.jpg" alt="mod.io" width="560"/></a>

## Platform Support

|Platform       | Support                                        |
| ------------- |------------------------------------------------|
|Windows        | Yes                                            |
|Windows (GDK)  | Yes [Contact us](#game-studios-and-publishers) |
|XBox (GDK)     | Yes [Contact us](#game-studios-and-publishers) |
|PlayStation 4  | Yes [Contact us](#game-studios-and-publishers) |
|PlayStation 5  | Yes [Contact us](#game-studios-and-publishers) |
|Nintendo Switch| Yes [Contact us](#game-studios-and-publishers) |
|Linux          | Yes                                            |
|macOS          | Yes                                            |

### Git Repository or .unitypackage
You can import the plugin directly from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/mod-browser-manager-by-mod-io-138866), or by downloading the package directly from the [Releases page](https://github.com/modio/modio-unity/releases). If you have any previous versions of the plugin installed, it is highly recommended to delete them before importing a newer version.

Alternatively, you can download an archive of the code using GitHub's download feature and place it in the Assets/Plugins directory within your Unity project.

## Getting started

1. Set up your [game profile on mod.io](https://mod.io/g/add) (or our [private test environment](https://test.mod.io/g/add)) to get your game ID and API key.
2. Add the plugin to your project using the installation instructions above.
3. Ensure you dont have any conflicting libraries by going to Assets/Plugins/mod.io/ThirdParty to remove any libraries you may already have in your project.
4. If you dont have Newtonsoft in your project, add it from the Package Manager by clicking the "Add package from git URL" option and enter "com.unity.nuget.newtonsoft-json" to add the Newtonsoft package
5. Restart unity to ensure it recognises the new assembly definitions.
6. Go to Tools > mod.io > Edit Settings to locate the config file.
7. Select the config file and use the inspector to assign your game ID and API key in server settings (Make sure to deselect the config file before using playmode in the editor. A known unity bug can cause the editor to crash in 2019-2021).
8. Setup complete! Join us [on Discord](https://discord.mod.io) if you have any questions or need help.

## Setting up the Browser UI

If you do not wish to create your own UI implementation you can use our default UI that comes built in to the plugin. Examples of how the UI looks are provided below. (If you dont wish to use the UI it is safe to delete the UI folder located at Assets/Plugins/mod.io/UI)

1. Follow the steps above to setup the config.
2. Navigate to the ModIOBrowser prefab at Assets/Plugins/mod.io/UI/Examples and drag it into your scene.
3. Use the ModIOBrowser.Browser.OpenBrowser() method to open the browser in your scene.
   `ModIOBrowser.Browser.OpenBrowser(null)`
4. The Browser UI is now setup!

![Example mod browser](https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/a7f9360d-4837-4d6e-b5cb-db5544a27b8c_orig.png)
![Example mod collection](https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/b37c33d6-aaa1-49c5-a6fd-c4ae18627bd2_orig.png)

## Authentication
In the current version of the plugin it is required that a user session is authenticated. Either via email or through another third party, such as Steam or Google. The process is fairly simply. Examples can be found below.


## Usage
below are a couple examples for some of the common usages of the plugin. Such as initialising, authenticating, enabling automatic downloads and installs, and getting a few mods from the mod.io server.

All of the methods required to use the plugin can be found in ModIOUnity.cs. If you prefer using async methods over callbacks you can alternatively use ModIOUnityAsync.cs to use an async variation of the same methods.

### Initialise the plugin
```c#
void Example()
{
    Result result ModIOUnity.InitializeForUser("ExampleUser");
 
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
```c#
void Example()
{
    UserInstalledMod[] mods = ModIOUnity.GetInstalledModsForUser(out Result result);
    
    foreach(UserInstalledMod mod in mods)
    {
        // This is the location of the installed mod
        string directory = mod.directory;
    }
}
```

### Enable automatic mod downloads and installs
```c#
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
```c#
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
```c#
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

## Submitting mods
You can also submit mods directly from the plugin. Refer to the documentation for methods such as `ModIOUnity.CreateModProfile` and `ModIOUnity.UploadModfile`.

Users can also submit mods directly from the mod.io website by going to your game profile page. Simply create an account and upload mods directly.

### Adding a mod
Here we go through the mod addition flow. Generate Token, Create Mod Profile, and Upload Mod File
```c#
public async void CreateMod()
{
   //token used to create mod profile
   var token = ModIOUnity.GenerateCreationToken();

   //Mod profile specifics
   ModProfileDetails modDetails = new ModProfileDetails
   {
       logo = GetTexture(),//the texture you will use for this mod's logo
       summary = "A brief summary of the mod.",
       name = "Mod Name"
   };

   //create the mod profile
   var createResultAnd = await ModIOUnityAsync.CreateModProfile(token, modDetails);
   if(!createResultAnd.result.Succeeded())
       return;//create mod unsuccessful

   //Points to a folder where all mod files are located (folder cannot be empty)
   ModfileDetails modFile = new ModfileDetails
   {
       modId = createResultAnd.value,
       directory = "files/mods/mod_123"
   };

   //upload the file to the mod profile
   var result = await ModIOUnityAsync.UploadModfile(modFile);
   if(result.Succeeded())
   {
       //Upload file successful!
   }
}
```

### Loading mods
Here is an example that grabs all mods installed for the current user, finds the png files in the mod's directory if they are tagged as a "Texture" and then loads them into a Texture2D asset.
```c#
public void LoadModExample()
{
   UserInstalledMod[] mods = ModIOUnity.GetInstalledModsForUser(out Result result);
   if (result.Succeeded())
   {
      foreach(var mod in mods)
      {
          //Tags are USER defined strings for a game and are setup in the web portal.
          string textureTag = "Texture";
   
          string directoryWithInstalledMod = mod.directory;
   
          //Optionally, you may want to use tags to help you determine the files to look for in an installed mod folder
          if(!mod.modProfile.tags.Contains(textureTag))
          {
              //Get all files in a directory
              string[] filePaths = System.IO.Directory.GetFiles(directoryWithInstalledMod);
              foreach(var path in filePaths)
              {
                  //Find .png files so that we can convert them into textures
                  if(path.EndsWith(".png"))
                  {
                      Texture2D tex = new Texture2D(1024, 1024);
                      
                      //Load a texture from directory
                      tex.LoadImage(File.ReadAllBytes(path));
                      
                      //Now you can replace the current texture in your game with the new one
                  }
              }
          }
      }
   }
}
```

## Dependencies
The [mod.io](https://mod.io) Unity Plugin requires the functionality of two other open-source Unity plugins to run. These are included as libraries in the UnityPackage in the `Assets/Plugins/mod.io/ThirdParty` directory:
* Json.Net for improved Json serialization. ([GitHub Repo](https://github.com/SaladLab/Json.Net.Unity3D) || [Unity Asset Store Page](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347))
* SharpZipLib to zip and unzip transmitted files. ([GitHub Repo](https://github.com/icsharpcode/SharpZipLib))

## Benefits
mod.io offers the same core functionality as Steamworks Workshop (1 click mod installs in-game), plus mod hosting, moderation and all of the critical pieces needed. Where we differ is our approach to modding and the flexibility a REST API offers. For example:

* We make mods cross platform accessible. That means users can upload a mod on PC and someone else can play it on the Xbox, for example.
* Our API is not dependent on a client, platform or SDK, allowing you to run mod.io in many places such as your homepage and launchers.
* Designing a good mod browsing UI is hard, our plugin ships with a UI built in to save you a lot of effort and help your mods stand out.
* We don’t apply rules globally, so if you want to enable patronage, sales or other experimental features, reach out to discuss.
* Our platform is built by the super experienced ModDB.com team and is continually improving for your benefit.
* Your community can consume the mod.io API to build modding fan sites or discord bots if they want.

## Game studios and Publishers
If you need assistance with 1st party approvals, or require a private, white-label UGC solution. [Contact us](mailto:developers@mod.io) to discuss.

## Contributions Welcome
Our Unity plugin is public and open source. Game developers are welcome to utilize it directly, to add support for mods in their games, or fork it for their games customized use. Want to make changes to our plugin? Submit a pull request with your recommended changes to be reviewed.

## Other Repositories
Our aim with [mod.io](https://mod.io), is to provide an [open modding API](https://docs.mod.io). You are welcome to [view, fork and contribute to our other codebases](https://github.com/modio) in use.
