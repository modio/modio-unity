# Unity V1 Upgrade Guide
Where the  <a href="https://github.com/modio/modio-unity/wiki/Quickstart-Guide">Unity Plugin QuickStart Guide</a> contains information to help you install and setup the new Unity Plugin in your project, this guide exists to assist with upgrading from mod.io Unity V1 to the new mod.io Unity Plugin by offering equivalent examples of the most common functions.


## Why Upgrade to the New Plugin?
mod.io Unity Plugin V1 has been deprecated as structural and design decisions have made it impractical to continue maintaining and enhancing with new features and functionality.

Upgrading to the new mod.io Unity Plugin results in extended support, additional features, and improved user experience. It provides enhanced support for consoles and boasts a new UI, as well as numerous enhancements, enabling you to accomplish more with less code.

## Core Differences
### Interface
The new mod.io Unity Plugin has a simplified and clearer interface. All core functionality is accessible via `ModIOUnity.cs`, or the asynchronous version `ModIOUnityAsync.cs`, located within the `ModIO` namespace. Additionally, the browser code is contained in `Browser.cs` in the
`ModIOBrowser` namespace.
No more calling functions from multiple static classes depending on the context!

### Callbacks
All calls into async functions on the new mod.io Unity Plugin provide a single callback parameter with a
`Result` or `ResultAnd<T>` parameter that will **always** be called at the conclusion of the operation. (Async methods return the result struct after an `await` instead.)
This provides a much clearer calling convention and gives a guarantee of one operation completion - one callback invocation, rather than the two callbacks required by async functions in mod.io Unity V1.

Here is an example of the usage with a callback and an await:
```csharp
void CallbackExample()
{
    ModIOUnity.GetMods(filter, (ResultAnd<ModPage> response)=>
    {
        if (response.result.Succeeded())
        {
            // Success
        }
    });
}

// OR

async void AsyncExample()
{
    ResultAnd<ModPage> getMods = await ModIOUnityAsync.GetMods(filter);

    if (getMods.result.Succeeded())
    {
        // Success
    }
}
```

## Initializing the plugin
Initialization in mod.io Unity V1 was handled mostly statically, pulling the details from the `Resources/modio_settings` asset during static initialization.
Optionally, a developer could set the user data directory by calling `UserDataStorage.SetActiveUser()` as seen in the sample below.
```csharp
void ModioUnityV1Example()
{
    // Optional (Sets the user data directory)
    string userProfileIdentifier = "local_game_user_id"; // Local User Account Identifier
    UserDataStorage.SetActiveUser(userProfileIdentifier, setActiveUserCallback);
}
```
Apart from this, there are no initialization possibilities in mod.io Unity V1.

For the new mod.io Unity Plugin, we have kept an automatic initialization option that pulls the data from the `Resources/mod.io/config` asset, similar to the function of mod.io Unity V1. However, there are also explicit initialization methods and shutdown methods that can be utilized if automatic initialization is disabled in the config asset.
```csharp
void InitializationExample()
{
    string userProfileIdentifier = "local_game_user_id"; // Local User Account Identifier
    Result result = ModIOUnity.InitializeForUser(userProfileIdentifier, initializationCallback);
    // Do work...
    ModIOUnity.Shutdown(shutdownCallback);
}
```
For further information, see the [initialization documentation](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#a1ed1149bf968635573142b52dbef516b).

## Getting and Synchronizing Subscription Data
mod.io Unity V1 built the synchronization of an authenticated user's subscription data into the UI code, meaning that a developer not using the sample browser would be responsible for ensuring that the user's subscriptions were kept in agreement with the server.
This has not changed in the new mod.io Unity Plugin, but the process of keeping that data synchronized is much easier, along with fetching the data for the subscribed mods.

Adding, synchronizing, and retrieving subscription data in mod.io Unity V1 involves chaining multiple calls together.
```csharp
void ModioUnityV1Example()
{
    int newSubModId = 123;
    int newUnsubModId = 456;

    // This call adds the sub to the local cache and queues it for synchronization
    UserAccountManagement.SubscribeToMod(newSubModId);
    // This call adds the unsub to the local cache and queues it for synchronization
    UserAccountManagement.UnsubscribeFromMod(newUnsubModId);
    // Push local subscription changes to mod.io servers
    UserAccountManagement.PushSubscriptionChanges(
        () => { /* chain callback into next section */ },
        (List<WebRequestError> unsortedErrorList) => { /* error callback code */ });

    // Fetch remote subscription changes from mod.io servers
    UserAccountManagement.PullSubscriptionChanges(
        (List<ModProfile> newRemoteSubscriptions) => { /* chain callback into next section */ },
        (WebRequestError error) => { /* error callback code */ });
    // Get Mod Profiles for Subscribed Mods
    List<int> subscribedModIds = LocalUser.subscribedModIds;
    ModManager.GetModProfiles(modIds,
        (ModProfile[] modProfiles) => { /* success callback code */ },
        (WebRequestError error) => { /* error callback code */ });
}
```

The new mod.io Unity Plugin streamlines this process by reducing the need for callback chaining, synchronizing immediately for local changes, and removing the need to handle the mod ids.
[FetchUpdates()](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#aab9e3eb7697f13e4e96273ac50ab79af) is the single synchronization function on the interface, handling all synchronization actions and only needing to be run generally once per session (Ideally after you Initialize the plugin).
```csharp
void Example()
{
    // Synchronizes the local and server data
    ModIOUnity.FetchUpdates(
        (Result result) => { /* callback code */ });
}
```
Furthermore, the subscribe and unsubscribe operations automatically flag the mod as requiring installation/uninstallation, a responsibility placed on the consumer in mod.io Unity V1. (See below)

## Listing the User's Installed Mods
Likewise in mod.io Unity V1, the new mod.io Unity Plugin allows the sharing of installed mods across multiple users to save network traffic and storage space.

mod.io Unity V1 didn't have a direct method of retrieving the mods installed for the current user. There are a variety of different methods that need to be chained together to retrieve a complete picture of the installed mod data.
```csharp
void ModioUnityV1Example()
{
    // Retrieves a de-identified list of mod directories for mods the user has "enabled"
    bool onlyEnabledMods = true;
    ModManager.QueryInstalledModDirectories(onlyEnabledMods,
        (List<string> installedModDirectories) => { /* callback code */ });

    // Retrieves a mapping of mod directories for mods the user has subscribed to
    List<int> subscriptions = LocalUser.subscribedModIds;
    ModManager.QueryInstalledMod(subscriptions,
        (IList<KeyValuePair<ModfileIdPair, string>> modDirectoryMap) => { /* callback code */ });

    // Retrieves the data for the installed mods that the user has "enabled" with no directory
    bool onlyEnabledMods = true;
    ModManager.QueryInstalledModVersions(onlyEnabledMods,
        (List<ModfileIdPair> installedModVersions) =>
        {
            // map the mod ids to a list
            List<int> modIds = installedModVersions.Select(x => x.modId).ToList();
            ModManager.GetModProfiles(modIds,
                (ModProfile[] modProfiles) => { /* success callback code */ },
                (WebRequestError error) => { /* error callback code */ });
        });
}
```

The new mod.io Unity Plugin makes this much simpler, giving you all the information in a single call, returning a [UserInstalledMod](https://sdkdocs.mod.io/unity/struct_mod_i_o_1_1_user_installed_mod.html) array (and a `Result`).
```csharp
void Example()
{
    UserInstalledMod[] mods = ModIOUnity.GetInstalledModsForUser(out Result result);
}
```

## Downloading, Updating and Uninstalling Mods
The new mod.io Unity Plugin has the business rules of "Subscription = install and update" built into it, such that the download, extract, and uninstall processes are managed automatically by the [Mod Management Loop](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#aabba78ef1b55e60e2334cc1ba6faf1c3), a process that runs asynchronously to detect changes to the subscriptions and automate mod data management.

mod.io Unity V1 handled the installation and uninstallation of mods in the ModBrowser code, but any developer looking to exclude that code or understand the installation process had a more difficult time.
```csharp
void ModioUnityV1Example()
{
    /// === Add a new subscription and install ===
    int newSubModId = 123;
    // This call adds the sub to the local cache and queues it for synchronization
    UserAccountManagement.SubscribeToMod(newSubModId);
    // Push local subscription changes to mod.io servers
    UserAccountManagement.PushSubscriptionChanges(
        () => { /* chain callback into next section */ },
        (List<WebRequestError> unsortedErrorList) => { /* error callback code */ });
    // Download and Install all mods (equivalent to new mod.io Unity Plugin)
    gameObject.StartCoroutine(ModManager.DownloadAndUpdateMods_Coroutine(LocalUser.subscribedModIds,
        () => { /* callback code */ }));

    /// === Remove a subscription and uninstall ===
    int newUnsubModId = 456;
    // This call adds the unsub to the local cache and queues it for synchronization
    UserAccountManagement.UnsubscribeFromMod(newUnsubModId);
    // Push local subscription changes to mod.io servers
    UserAccountManagement.PushSubscriptionChanges(
        () => { /* chain callback into next section */ },
        (List<WebRequestError> unsortedErrorList) => { /* error callback code */ });
    // Uninstall the mod
    ModManager.UninstallMod(newUnsubModId,
        (bool uninstallSuccess) => { /* callback code */ });

    // === Fetch remote data and fix installation state ===
    // Fetch remote subscription changes from mod.io servers
    UserAccountManagement.PullSubscriptionChanges(
        (List<ModProfile> newRemoteSubscriptions) => { /* chain callback into next section */ },
        (WebRequestError error) => { /* error callback code */ });
    // Download and Install all mods (equivalent to new mod.io Unity Plugin)
    gameObject.StartCoroutine(ModManager.DownloadAndUpdateMods_Coroutine(LocalUser.subscribedModIds,
        () => { /* chain callback into next section */ }));
    // Calculate uninstall mods
    List<int> modsToUninstall = new List<int>();
    List<int> subscribedModIds = LocalUser.subscribedModIds;
    ModManager.QueryInstalledModVersions(subscribedModIds,
        (IList<KeyValuePair<ModfileIdPair, string>> modDirectoryMap) =>
            {
                if(!subscribedModIds.Contains(modDirectoryMap.Key.modId))
                {
                    modsToUninstall.Add(modDirectoryMap.Key.modId);
                }
            }
            /* chain callback into next section */
        );
    // uninstall mods
    foreach(int modId in modsToUninstall)
    {
        ModManager.UninstallMod((bool uninstallSuccess) => { /* callback code */ });
    }
}
```
Of note, the uninstall process above, can't account for mods installed by other users on the system.
This is one of the key processes that has been streamlined in the new mod.io Unity Plugin.

A call to [ModIOUnity.EnableModManagement](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#aabba78ef1b55e60e2334cc1ba6faf1c3) starts the background process of monitoring for subscription changes, and takes a (nullable) callback for mod management events. This can be disabled at any point with a call to [ModIOUnity.DisableModManagement](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#a7eda62ae267aa409b6408fd60ed16429).
Any changes invoked locally, and any changes retrieved with [ModIOUnity.FetchUpdates](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#aab9e3eb7697f13e4e96273ac50ab79af) are automatically queued and actioned.
```csharp
void Example()
{
    ModManagementEventDelegate eventDelegate = (ModManagementEventType eventType, ModId modId, Result eventResult) => { /* handle event */};

    // Begins monitoring for changes and enables downloading/extracting/deleting of mod data
    ModIOUnity.EnableModManagement(eventDelegate);

    /// === Add a new subscription and install ===
    int newSubModId = 123;
    // Pushes the subscription to the server and flags for download and installation
    ModIOUnity.SubscribeToMod(newSubModId, (Result result) => /* callback code */});

    /// === Remove a subscription and uninstall ===
    int newUnsubModId = 456;
    // Pushes the unsubcribe action to the server and flags it for uninstallation
    ModIOUnity.UnsubscribeFromMod(newUnsubModId, (Result result) => /* callback code */);

    // Ends monitoring for changes and disables downloading/extracting/deleting of mod data
    ModIOUnity.DisableModManagement();
}
```
**Note:** The `Result` returned from [ModIOUnity.SubscribeToMod](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#ad05f5c1fc8b66389cbb11c2f6feff34d) and [ModIOUnity.UnsubscribeFromMod](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#a68adbece196b850ef361c445d915fb42) indicate the outcome of the subscribe/unsubscribe attempt being sent to the server. For notification of the outcome of an installation/uninstallation operation, the delegate passed to [ModIOUnity.EnableModManagement](https://sdkdocs.mod.io/unity/class_mod_i_o_1_1_mod_i_o_unity.html#aabba78ef1b55e60e2334cc1ba6faf1c3) will be invoked.

## Authenticating a user
The new Plugin requires a user to be authenticated in order to download, install
and manage mods. The plugin offers email authentication or numerous third party
methods, such as Steam or Xbox.

### Email Authentication
In the V1 Plugin you can authenticate via email in the following way:
```csharp
void modioUnityV1_RequestEmailCode(string playerEmail)
{
    ModIO.APIClient.SendSecurityCode(
        playerEmail, 
        (APIMessage apiMessage) => { /* callback code */ }, 
        (WebRequestError error) => { /* callback code */ });
}

void modioUnityV1_SubmitSecurityCode(string userSecurityCode)
{
    ModIO.UserAccountManagement.AuthenticateWithSecurityCode(
        userSecurityCode, 
        (UserProfile userProfile) => { /* callback code */ }, 
        (WebRequestError error) => { /* callback code */ });
}
```
In the new Plugin you can do it like so:
```csharp
async void RequestEmailCode(string playerEmail)
{
    Result result = await ModIOUnityAsync.RequestAuthenticationEmail(playerEmail);
 
    if (result.Succeeded())
    {
        Debug.Log("Succeeded in sending security code");
    }
    else
    {
        Debug.Log("Failed to send security code to that email address");
    }
}

async void SubmitSecurityCode(string userSecurityCode)
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

### Third party Authentication
In the V1 Plugin you can authenticate a user with a third party service
like Steam:
```csharp
UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(pTicket,
                                                              pcbTicket,
                                                              hasUserAcceptedTerms,
                                                              onSuccess,
                                                              onError);
```
In the new Plugin you can use the following:
```csharp
ModIOUnity.AuthenticateUserViaSteam(token, email, termsHash, callback);
```
(Note you can use `ModIOUnityAsync` to `await` instead.)

Be advised that you will need to get the terms of use hash key in order to properly
authenticate. This requires the use of `ModIOUnity.GetTermsOfUse`.
This is to ensure users view and accept the terms of use.

Here is a complete example getting the TOS hash and Authenticating via Steam:

```csharp
// Use this to cache the TOS we receive
TermsOfUse termsOfUse;

// This needs to be displayed to the user before they can authenticate
async void DisplayTOS()
{
    ResultAnd<TermsOfUse> tos = await ModIOUnityAsync.GetTermsOfUse();
 
    if (tos.result.Succeeded())
    {
        // Display the terms of use via a text field
        TextField.text = tos.value.termsOfUse;
        
        // cache the Terms of Use
        termsOfUse = tos.value;
    }
}

// If they agree after viewing the TOS you can attempt to authenticate with the TOS hash
async void AgreeAndAuthenticate(string token, string email)
{
    Result result = await ModIOUnityAsync.AuthenticateUserViaSteam(token, email, termsOfUse.hash);
    
    if (result.Succeeded())
    {
        // Succeeded to authenticate via Steam
    }
}
```

## Need more help?
If you require additional assistance you're welcome to contact us
directly via email or on our Discord channel. Our team can help answer
specific questions about the plugin and provide support.
