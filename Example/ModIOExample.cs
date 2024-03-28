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

            Result resultUpdate = await ModIOUnityAsync.FetchUpdates();
            if (!resultUpdate.Succeeded())
            {
                Debug.LogError($"FetchUpdates failed: {resultUpdate.message}");
            }

            ModProfile[] subscribedMods = GetSubscribedMods();
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

            ResultAnd<Texture2D> resultAnd = await ModIOUnityAsync.DownloadTexture(modProfile.logoImage320x180);
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
