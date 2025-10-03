using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modio;
using Modio.Authentication;
using Modio.Mods;
using Modio.Mods.Builder;
using Modio.Unity;
using Modio.Users;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = System.Random;

#if MODIO_FACEPUNCH
using Modio.Platforms.Facepunch;
#endif

public class ModioUnityExample : MonoBehaviour
{
    // Reusing a single byte-array is a small memory-conscious
    // optimization for when we are generating our dummy files.
    static readonly byte[] Megabyte = new byte[1024 * 1024];
    static readonly Random RandomBytes = new Random();

    [Header("Authentication")]
    [SerializeField] GameObject authContainer;
    [SerializeField] InputField authInput;
    [SerializeField] Button authRequest;
    [SerializeField] Button authSubmit;

    [Header("Terms of Use")]
    [SerializeField] GameObject tosContainer;
    [SerializeField] Button termsLink;
    [SerializeField] Button privacyLink;
    [SerializeField] Button denyButton;
    [SerializeField] Button acceptButton;
    
    // Downloading images
    [Header("Random Mod")]
    [SerializeField] GameObject randomContainer;
    [SerializeField] Text randomName;
    [SerializeField] Image randomLogo;
    [SerializeField] Button randomButton;
    
    // Searching for mods
    Mod[] allMods;

    // Installing mods
    Mod currentDownload;
    float downloadProgress;
    float timeToProgressCheck = 1f;

    void Awake()
    {
        // This enforces email auth to be used, a higher priority can be used if needed
        ModioServices.Bind<IModioAuthService>()
                          .FromInstance(new ModioEmailAuthService(GetAuthCode));

#if MODIO_FACEPUNCH
        ModioServices.Bind<IModioAuthService>()
                     .FromInstance(new ModioFacepunchService(), ModioServicePriority.DeveloperOverride);
#endif
        
        authContainer.SetActive(false);
        tosContainer.SetActive(false);
        randomContainer.SetActive(false);
    }

#region Initialization

    void Start()
    {
        InitPlugin();
    }

    async Task InitPlugin()
    {
        Error error = await ModioClient.Init();

        if (error)
        {
            Debug.LogError($"Error initializing mod.io: {error}");
            return;
        }

        Debug.Log("mod.io plugin initialized");
        OnInit();
    }

    void OnInit()
    {
        if (User.Current.IsAuthenticated)
        {
            OnAuth();
            return;
        }

#if MODIO_FACEPUNCH
        if (ModioClient.AuthService is ModioFacepunchService)
        {
            tosContainer.SetActive(true);
            
            termsLink.onClick.AddListener(() => Application.OpenURL("https://mod.io/terms"));
            privacyLink.onClick.AddListener(() => Application.OpenURL("https://mod.io/privacy"));
            
            acceptButton.onClick.AddListener(() => Authenticate());
            denyButton.onClick.AddListener(() => tosContainer.SetActive(false));
            
            return;
        }
#endif

        authContainer.SetActive(true);
        // You can assign these using the Inspector if you prefer
        authRequest.onClick.AddListener(() => Authenticate());
    }

#endregion

#region Authentication

    async Task Authenticate()
    {
        Error error = await ModioClient.AuthService.Authenticate(
            true,
            authInput.text.Length > 0 ? authInput.text : null
        );

        if (error)
        {
            Debug.LogError($"Error authenticating user: {error}");
            return;
        }

        OnAuth();
    }

    async Task<string> GetAuthCode()
    {
        bool codeEntered = false;

        authSubmit.onClick.AddListener(() => codeEntered = true);
        
        while (!codeEntered)
            await Task.Yield();

        return authInput.text;
    }

    async void OnAuth()
    {
        Debug.Log($"Authenticated user: {User.Current.Profile.Username}");
        
        authContainer.SetActive(false);
        tosContainer.SetActive(false);

        await AddModsIfNone();
        
        allMods = await GetAllMods();
        Debug.Log($"Available mods:\n{string.Join("\n", allMods.Select(mod => $"{mod.Name} (id: {mod.Id})"))}");
        
        randomButton.onClick.AddListener(SetRandomMod);
        randomContainer.SetActive(true);
        SetRandomMod();
        
        while (User.Current.IsUpdating)
            await Task.Yield();
        
        Mod[] subscribedMods = GetSubscribedMods();
        Debug.Log($"Subscribed mods:\n{(subscribedMods.Length > 0 ? string.Join("\n", subscribedMods.Select(mod => $"{mod.Name} (id: {mod.Id})")) : "None")}");

        await SubscribeToMod(allMods[UnityEngine.Random.Range(0, allMods.Length - 1)]);
        
        WakeUpModManagement();
        
        var installedMods = ModInstallationManagement.GetAllInstalledMods();
        Debug.Log($"Installed mods:\n{(installedMods.Count > 0 ? string.Join("\n", installedMods.Select(mod => $"{mod.Name} (id: {mod.Id})")) : "None")}");
    }

#endregion

#region Uploading Mods

    async Task AddModsIfNone()
    {
        (Error error, ModioPage<Mod> page) = await Mod.GetMods(new ModSearchFilter());
        if (error)
        {
            Debug.LogError($"Error getting mods: {error}");
            return;
        }

        if (page.Data.Length != 0)
        {
            Debug.Log($"{page.Data.Length} mods found. Not adding mods");
            return;
        }

        DummyModData[] mods =
        {
            await GenerateDummyMod("Cool Weapon", "A really cool weapon.", "24466B", "FDA576", 10),
            await GenerateDummyMod( "Funny Sound Pack", "You'll laugh a lot using this.", "B85675", "633E63", 50),
            await GenerateDummyMod("Klingon Language Pack", "tlhIngan Hol Dajatlh'a'?", "93681C", "FFEAD0", 1),
            await GenerateDummyMod( "Ten New Missions", "Ported from the sequel to the prequel!", "FDA576", "D45B7A", 99),
        };
        
        foreach (DummyModData mod in mods)
        {
            await UploadMod(mod.name, mod.summary, mod.logo, mod.path);
            // Directory.Delete(mod.path, true); // Uncomment if you generated dummy mods
        }
    }

    async Task UploadMod(string modName, string summary, Texture2D logo, string path)
    {
        Debug.Log($"Starting upload: {modName}");
    
        var builder = Mod.Create();
    
        builder.SetName(modName)
               .SetSummary(summary)
               .SetLogo(logo.EncodeToPNG(), ImageFormat.Png)
               .EditModfile()
               .SetSourceDirectoryPath(path)
               .FinishModfile();
    
        (Error error, Mod mod) = await builder.Publish();
    
        if (error)
        {
            Debug.LogError($"Error uploading mod {modName}: {error}");
            return;
        }

        Debug.Log($"Successfully created mod {mod.Name} with Id {mod.Id}");
    }

#endregion

#region Searching for Mods

    async Task<Mod[]> GetAllMods()
    {
        (Error error, ModioPage<Mod> page) = await Mod.GetMods(new ModSearchFilter());
        if (error)
        {
            Debug.LogError($"Error getting mods: {error}");
            return Array.Empty<Mod>();
        }

        return page.Data;
    }

#endregion

#region Downloading Images

    async void SetRandomMod()
    {
        Mod mod = allMods[UnityEngine.Random.Range(0, allMods.Length - 1)];
        
        randomName.text = mod.Name;

        (Error error, Texture2D texture) = await mod.Logo.DownloadAsTexture2D(Mod.LogoResolution.X320_Y180);

        if (error)
        {
            Debug.LogError($"Error downloading {mod.Name}'s logo: {error}");
            return;
        }
        
        randomLogo.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }

#endregion

#region Subscribing to Mods

    static Mod[] GetSubscribedMods() => User.Current.ModRepository.GetSubscribed().ToArray();

    async Task SubscribeToMod(Mod mod)
    {
        var error = await mod.Subscribe();

        if (error)
        {
            Debug.LogError($"Error subscribing to {mod.Name}: {error}");
            return;
        }
        
        Debug.Log($"Subscribed to mod: {mod.Name}");
    }

#endregion

#region Installing Mods

    void WakeUpModManagement()
    {
        void HandleModManagementEvent(
            Mod mod, 
            Modfile modfile, 
            ModInstallationManagement.OperationType jobType, 
            ModInstallationManagement.OperationPhase jobPhase
        ){
            Debug.Log($"{jobType} {jobPhase}: {mod.Name}");

            switch (jobPhase)
            {
                case ModInstallationManagement.OperationPhase.Started
                    when jobType is not ModInstallationManagement.OperationType.Uninstall:
                    currentDownload = mod;
                    break;

                case ModInstallationManagement.OperationPhase.Cancelled:
                case ModInstallationManagement.OperationPhase.Failed:
                    currentDownload = null;
                    break;
                
                case ModInstallationManagement.OperationPhase.Completed
                    when jobType is not ModInstallationManagement.OperationType.Uninstall:
                    Debug.Log($"Mod {mod.Name} installed at {mod.File.InstallLocation}");
                    currentDownload = null;
                    break;
                
                case ModInstallationManagement.OperationPhase.Completed:
                    Debug.Log($"Mod {mod.Name} uninstalled");
                    break;
            }
        }
        
        ModInstallationManagement.ManagementEvents += HandleModManagementEvent;
    }

    void Update()
    {
        if (currentDownload == null)
            return;

        timeToProgressCheck -= Time.deltaTime;

        if (timeToProgressCheck > 0)
            return;

        Debug.Log($"Downloading {currentDownload.Name}: [{Mathf.RoundToInt(currentDownload.File.FileStateProgress * 100)}%]");
        timeToProgressCheck += 1f;
    }

#endregion

#region Generate Dummy Mods

    async Task<DummyModData> GenerateDummyMod(
        string dummyName,
        string summary,
        string backgroundColor,
        string textColor,
        int megabytes
    )
    {
        Debug.Log($"Writing temporary mod file: {dummyName}");

        string path = Path.Combine(Application.dataPath, $"../_temp_dummy_mods/{dummyName}");
        Directory.CreateDirectory(path);

        using (FileStream fs = File.OpenWrite(Path.Combine(path, $"{dummyName}.dummy")))
        {
            for (int i = 0; i < megabytes; i++)
            {
                RandomBytes.NextBytes(Megabyte);
                await fs.WriteAsync(Megabyte, 0, Megabyte.Length);
            }
        }

        return new DummyModData(
            dummyName,
            summary,
            await GenerateLogo(dummyName.Replace(' ', '+'), backgroundColor, textColor),
            path
        );
    }

    // Uses a third-party API to generate a logo for each
    // mod, adding some variety when we display them later
    async Task<Texture2D> GenerateLogo(string text, string backgroundColor, string textColor)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(
            $"https://placehold.co/512x288/{backgroundColor}/{textColor}.png?text={text}"
        );

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
