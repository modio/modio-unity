using System;
using System.Reflection;
using System.Threading.Tasks;
using ModIO;
using ModIO.Implementation;
using ModIO.Util;
using ModIOBrowser.Implementation;
using UnityEngine;
using UnityEngine.UI;
using Result = ModIO.Result;

namespace ModIOBrowser
{

    /// <summary>
    /// The main handler for opening and closing the mod IO Browser.
    /// Use Browser.Open() to open and Browser.Close() to close.
    /// </summary>
    public class Browser : MonoSingleton<Browser>
    {
        // All of the following fields with [SerializeField] attributes are assigned on the prefab
        // from the unity editor inspector
        [Header("Settings")]
        [Tooltip("Setting this to false will stop the Browser from automatically initializing the plugin")]
        [SerializeField] bool autoInitialize = true;
        internal static bool allowEmailAuthentication = true;
        internal static bool allowExternalAuthentication = true;

        [SerializeField] public UiSettings uiConfig;
        [SerializeField] public Home homePanel;
        [SerializeField] private SearchFilter[] browserRowSearchFilters;
        public SingletonAwakener SingletonAwakener;

        [Header("Main")]
        public ColorScheme colorScheme;
        public GameObject BrowserCanvas;

        /// <summary>
        /// This is set whenever GotoPanel is invoked, the current opened panel is cached so that
        /// when the user uses 'back' it knows which panel to close and go back to
        /// </summary>
        public static GameObject currentFocusedPanel;

        [Header("Default Selections")]
        [SerializeField] Selectable defaultCollectionSelection;

        // This is assigned on OpenBrowser() and will get invoked each time the Browser is closed.
        internal static Action OnClose;

        /// <summary>
        /// This delegate will get invoked whenever an InputField is selected. You can specify what
        /// virtual keyboard you'd like to open by providing the Browser with a delegate.
        /// </summary>
        public static VirtualKeyboardDelegate OpenVirtualKeyboard;

        public delegate void VirtualKeyboardDelegate(string title,
                                                     string text,
                                                     string placeholder,
                                                     VirtualKeyboardType virtualKeyboardType,
                                                     int characterLimit,
                                                     bool multiline,
                                                     Action<string> onClose);

        public delegate void RetrieveAuthenticationCodeDelegate(Action<string> callbackOnReceiveCode);

        /// <summary>
        /// Represents the type of keyboard layout appropriate for the type of InputField being selected
        /// </summary>
        public enum VirtualKeyboardType
        {
            Default,
            Search,
            EmailAddress
        }

        // if the ModIO plugin hasn't been initialized yet but the user wishes to open the UI we set
        // this to true and open the browser the moment we have been initialized
        static bool openOnInitialize = false;

        public static bool IsOpen = false;

        public SearchFilter FeaturedSearchFilter { get; private set; }
        public SearchFilter[] BrowserRowSearchFilters => browserRowSearchFilters;

        // Use Awake() to setup the Singleton for Browser.cs and initialize the plugin
        protected override void Awake()
        {
            base.Awake();

            SharedUi.settings = uiConfig;
            SharedUi.colorScheme = colorScheme;

            SetModRowFilterDefaults();
        }

        void Start()
        {
            if (autoInitialize && !ModIOUnity.IsInitialized())
            {
                Result result = ModIOUnity.InitializeForUser("User");
                OnInitialize(result);
            }
        }

        void Update()
        {
            // If the user has indicated that they wish to open the Browser but we haven't been
            // initialized yet, keep checking until we have been initialized
            if(openOnInitialize)
            {
                if(ModIOUnity.IsInitialized())
                {
                    openOnInitialize = false;
                    IsInitialized();
                }
            }
        }

        // Runs at the end of every frame so long as the browser is active, and gets the most recent
        // mod management operation to be run via the UpdateProgressState() method
        void LateUpdate()
        {
            if(BrowserCanvas.activeSelf)
            {
                Mods.UpdateProgressState();
            }
        }

#region Frontend methods

        /// <summary>
        /// We need this to be able to hook up a 'back' button in the UI to close the browser.
        /// The CloseBrowser method for the frontend use is static.
        /// </summary>
        public void CloseBrowserPanel()
        {
            Close();
            homePanel.ResetScrollRect();
        }


        /// <summary>
        /// Use this method to open the Mod Browser UI for a user to begin browsing.
        /// </summary>
        /// <param name="onClose">Assign an action to be invoked when the browser is closed</param>
        /// <remarks>
        /// Keep in mind that a user may close the browser from an internal method, such as using
        /// the UI 'back' button or pressing ESC.
        /// </remarks>
        public static void Open(Action onClose)
        {
            OnClose = onClose;

            if(!ModIOUnity.IsInitialized())
            {
                openOnInitialize = true;
            }
            else
            {
                IsInitialized();
                //Turn on selection manager and exampleinpute capture?
            }
        }

        /// <summary>
        /// Use this method to properly close and hide the Mod Browser UI.
        /// </summary>
        /// <remarks>
        /// You may not need to use this method since the browser has the ability to close itself,
        /// however if you need to close it for any reason you can do so via this method. Keep in
        /// that the OnClose delegate provided from OpenBrowser will get invoked from this method.
        /// </remarks>
        public static void Close()
        {
            openOnInitialize = false;

            // Deactivate the Canvas
            Instance?.BrowserCanvas?.SetActive(false);
            IsOpen = false;
            OnClose?.Invoke();

            SelectionManager.Instance.gameObject.SetActive(false);
        }


        /// <summary>
        /// You can use this to convert your byte[] steam app ticket into a trimmed base64 encoded
        /// string to be used for the steam authentication.
        /// </summary>
        /// <param name="ticketData">the byte[] steam app ticket data</param>
        /// <param name="ticketSize">the desired length of the ticket to be trimmed to</param>
        /// <seealso cref="SetupSteamAuthenticationOption"/>
        /// <returns>base 64 encoded string from the provided steam app ticket</returns>
        [Obsolete("Use EncodeEncryptedSteamAppTicket located in ModIO.Utility instead.")]
        public static string EncodeEncryptedSteamAppTicket(byte[] ticketData, uint ticketSize)
        {
            //------------------------------- Trim the app ticket --------------------------------//
            byte[] trimmedTicket = new byte[ticketSize];
            Array.Copy(ticketData, trimmedTicket, ticketSize);

            string base64Ticket = null;
            try
            {
                base64Ticket = Convert.ToBase64String(trimmedTicket);
            }
            catch(Exception exception)
            {
                Debug.LogError($"[mod.io Browser] Unable to convert the app ticket to a "
                               + $"base64 string, caught exception: {exception.Message} - "
                               + $"{exception.InnerException?.Message}");
            }

            return base64Ticket;
        }

        /// <summary>
        /// Using this method will enable an option in the Authentication modal for a user to
        /// log in with their Xbox credentials. Simply provide the xbox token and the user's email
        /// address and the authentication flow for Xbox will be available.
        /// </summary>
        /// <param name="getXboxTokenDelegate">delegate for retrieving the Xbox token</param>
        /// <param name="userEmail">(Optional) provide the users email address</param>
        public static void SetupXboxAuthenticationOption(RetrieveAuthenticationCodeDelegate getXboxTokenDelegate, string userEmail = null)
        {
            MonoDispatcher.Instance.Run(() =>
            {
                Authentication.getXboxToken = getXboxTokenDelegate;
                Authentication.optionalThirdPartyEmailAddressUsedForAuthentication = userEmail;
            });
        }

        /// <summary>
        /// Using this method will enable an option in the Authentication modal for a user to
        /// log in with their Switch account. Simply provide the NSA ID of the user and their email
        /// address and the authentication flow for Switch will be available.
        /// </summary>
        /// <param name="getSwitchNsaIdDelegate">delegate Switch NSA ID of the user</param>
        /// <param name="userEmail">(Optional) provide the users email address</param>
        public static void SetupSwitchAuthenticationOption(RetrieveAuthenticationCodeDelegate getSwitchNsaIdDelegate, string userEmail = null)
        {
            MonoDispatcher.Instance.Run(() =>
            {
                Authentication.getSwitchToken = getSwitchNsaIdDelegate;
                Authentication.optionalThirdPartyEmailAddressUsedForAuthentication = userEmail;
            });
        }

        /// <summary>
        /// Using this method will enable an option in the Authentication modal for a user to
        /// log in with their steam credentials. Simply provide the steam ticket (base 64 encoded)
        /// and the user's email address and the authentication flow for steam will be available.
        /// </summary>
        /// <remarks>
        /// You can use our utility method to encode the ticket if you're unsure:
        /// Browser.EncodeEncryptedSteamAppTicket(byte[] ticketData, uint ticketSize).
        /// If you're using the Facepunch library the app ticket you receive is already pre-trimmed
        /// so you can simply use ticketData.Length for the ticketSize parameter.
        /// </remarks>
        /// <param name="getSteamTicketDelegate">delegate used to get the base64 encoded app ticket</param>
        /// <param name="userEmail">(Optional) provide the users email address</param>
        /// <seealso cref="EncodeEncryptedSteamAppTicket"/>
        public static void SetupSteamAuthenticationOption(RetrieveAuthenticationCodeDelegate getSteamTicketDelegate, string userEmail = null)
        {
            MonoDispatcher.Instance.Run(() =>
            {
                Authentication.getSteamAppTicket = getSteamTicketDelegate;
                Authentication.optionalThirdPartyEmailAddressUsedForAuthentication = userEmail;
            });
        }

        public static void SetupEpicAuthenticationOption(RetrieveAuthenticationCodeDelegate getEpicTicketDelegate, string userEmail = null)
        {
            MonoDispatcher.Instance.Run(() =>
            {
                Authentication.getEpicAuthCode = getEpicTicketDelegate;
                Authentication.optionalThirdPartyEmailAddressUsedForAuthentication = userEmail;
            });
        }

        public static void SetupGOGAuthenticationOption(RetrieveAuthenticationCodeDelegate getGogTicketDelegate, string userEmail = null)
        {
            MonoDispatcher.Instance.Run(() =>
            {
                Authentication.getGogAuthCode = getGogTicketDelegate;
                Authentication.optionalThirdPartyEmailAddressUsedForAuthentication = userEmail;
            });
        }

        /// <summary>
        /// Using this method will enable an option in the Authentication modal for a user to
        /// log in with their PlayStation credentials. Simply provide the auth code
        /// and the user's email address and the authentication flow for PlayStation will be available.
        /// </summary>
        /// <param name="getPlayStationAuthCodeDelegate">Delegate to get the PlayStation auth code</param>
        /// <param name="environment"></param>
        /// <param name="userEmail">(Optional) provide the users email address</param>
        public static void SetupPlayStationAuthenticationOption(RetrieveAuthenticationCodeDelegate getPlayStationAuthCodeDelegate, PlayStationEnvironment environment, string userEmail = null)
        {
            MonoDispatcher.Instance.Run(() =>
            {
                Authentication.getPlayStationAuthCode = getPlayStationAuthCodeDelegate;
                Authentication.optionalThirdPartyEmailAddressUsedForAuthentication = userEmail;
                Authentication.PSEnvironment = environment;
            });
        }

        public void SetFeaturedFilter(SearchFilter searchFilter)
        {
            FeaturedSearchFilter = searchFilter;
        }

        public void SetBrowserRowSearchFilters(SearchFilter[] searchFilters)
        {
            this.browserRowSearchFilters = searchFilters;
        }

        private void SetModRowFilterDefaults()
        {
            if(this.FeaturedSearchFilter == null)
            {
                this.FeaturedSearchFilter = new SearchFilter();
                FeaturedSearchFilter.RevenueType = RevenueType.Free;
                this.FeaturedSearchFilter.SetPageIndex(0);
                this.FeaturedSearchFilter.SetPageSize(10);
                this.FeaturedSearchFilter.SetSortBy(SortModsBy.Downloads);
                // Note: this is a mistake on the backend api. Ascending is swapped with descending for this field
                this.FeaturedSearchFilter.SetToAscending(true);
            }

            if(browserRowSearchFilters == null || browserRowSearchFilters.Length == 0)
            {
                browserRowSearchFilters = new SearchFilter[4];

                var filter = new SearchFilter();
                // Edit filter for next row
                filter = new SearchFilter();
                filter.SetPageIndex(0);
                filter.SetPageSize(20);
                filter.SetSortBy(SortModsBy.Rating);
                filter.SetToAscending(true);
                browserRowSearchFilters[0] = filter;

                filter = new SearchFilter();
                // Edit filter for next row
                filter = new SearchFilter();
                filter.SetPageIndex(0);
                filter.SetPageSize(20);
                filter.SetSortBy(SortModsBy.Subscribers);
                filter.SetToAscending(true);
                browserRowSearchFilters[1] = filter;

                filter = new SearchFilter();
                // Edit filter for next row
                filter = new SearchFilter();
                filter.SetPageIndex(0);
                filter.SetPageSize(20);
                filter.SetSortBy(SortModsBy.Popular);
                filter.SetToAscending(false);
                browserRowSearchFilters[2] = filter;

                // Edit filter for next row
                filter = new SearchFilter();
                filter.SetPageIndex(0);
                filter.SetPageSize(20);
                filter.SetSortBy(SortModsBy.DateSubmitted);
                filter.SetToAscending(false);
                browserRowSearchFilters[3] = filter;
            }
            else
            {
                foreach(var filter in this.browserRowSearchFilters)
                {
                    if(filter.RevenueType == RevenueType.FreeAndPaid || filter.RevenueType == RevenueType.Paid)
                        filter.RevenueType = RevenueType.Free;
                    filter.SetPageIndex(0);
                    filter.SetPageSize(20);
                }
            }
        }


#endregion // Frontend methods

#region Initialization
        /// <summary>
        /// We use this to check initialization if the plugin hasn't been initialized we will first
        /// attempt to initialize it ourselves, based on the current config file.
        /// </summary>
        /// <param name="result"></param>
        static void OnInitialize(Result result)
        {
            if(result.Succeeded())
            {
                if(openOnInitialize)
                {
                    IsInitialized();
                }
                Debug.Log("[mod.io Browser] Initialized ModIO Plugin");
            }
            else
            {
                Close();
                Debug.LogWarning("[mod.io Browser] Failed to Initialize ModIO Plugin. Make sure your config file (Tools -> mod.io -> Edit Settings) contains the correct server address (test or production) and that you've supplied the game id and API Key for your game.");
            }
        }

        static async void IsInitialized()
        {
            openOnInitialize = false;

            if(Instance == null)
            {
                Debug.LogWarning("[mod.io Browser] Could not open because the Browser.cs"
                                 + " singleton hasn't been set yet. (Check the gameObject holding"
                                 + " the Browser.cs component isn't set to inactive)");
                return;
            }

            //Ensure singletons are initialized
            Instance.SingletonAwakener.AttemptInitilization();

            // Activate the Canvas
            if(!Instance.BrowserCanvas.activeSelf)
            {
                Instance.BrowserCanvas.SetActive(true);
                IsOpen = true;
            }

            Collection.Instance.CacheLocalSubscribedModStatuses();

            // open the browser panel (This will show loading icons etc, but wont load yet)
            Home.Instance.Open();

            // wait and check if we're authenticated, we need to know if our access token is still valid
            var isAuthed = await ModIOUnityAsync.IsAuthenticated();
            if(isAuthed.Succeeded())
            {
                Authentication.Instance.IsAuthenticated = true;
                ModIOUnity.FetchUpdates(delegate { });

                // We may require the new access token before getting mods
                await Authentication.GetNewAccessToken();
            }
            else
            {
                Authentication.Instance.IsAuthenticated = false;

                // Attempt to open an authentication option if one exists
                AuthenticationPanels.Instance.SkippedIntoTheOnlyExistingAuthenticationOption();
            }

            Authentication.Instance.currentAuthenticationPortal = Settings.build.userPortal;
            Implementation.Avatar.Instance.SetupUser();

            // refresh the home panel now that we know if our access token will work
            Home.Instance.RefreshHomePanel();
            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);

            SelectionManager.Instance.gameObject.SetActive(true);
        }

        public void OpenMenuProfile() => Navigating.OpenMenuProfile();

        #endregion

#region Editor helpers
        public void CheckForMissingReferencesInScene()
        {
            Debug.LogWarning("This function may give false positives, mostly in the case of text input fields and dropdowns");
            MonoBehaviour[] components = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach(MonoBehaviour component in components)
            {
                var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach(var field in fields)
                {
                    if(field.FieldType == typeof(GameObject) && field.GetValue(component) == null)
                    {
                        //Debug.LogError("Missing reference on " + component.name + " of object: " + component.gameObject.name);
                        Debug.LogError("Missing reference at: " + component.transform.FullPath());
                    }
                }
            }
        }
#endregion
    }
}
