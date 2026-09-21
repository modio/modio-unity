using System;
using System.Threading.Tasks;
using Modio.Extensions;
using Modio.Mods;
using Modio.Search;
using Modio.Unity.UI.Components;
using Modio.Unity.UI.Components.SearchProperties;
using Modio.Users;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Search
{
    /// <summary>
    /// This manages any searches run by the plugin.
    /// Children of this script can use a <see cref="ModioUISearchProperties"/> to respond to the search updating
    /// Use <see cref="ModioUISearchSettings"/> to specify what you are searching for
    ///
    /// A modioUiSearch is used for the main search by the plugin (the browse screen) as well as a separate one per carousel
    /// It's also used for showing a mods dependencies and a collection's contents
    ///
    /// To get a basic search going;
    ///   - Assign a prefab with a <see cref="ModioUISearchSettings"/> to _searchOnStart
    ///   - Have a child gameObject in the scene with a <see cref="ModioUISearchProperties"/> component
    ///   - Add a <see cref="SearchPropertyDisplayResults"/> to the UISearchProperties
    ///   - Point that DisplayResults at a <see cref="ModioUIModGroup"/>
    /// </summary>
    public class ModioUISearch : MonoBehaviour, IModioUIPropertiesOwner
    {
        [SerializeField] bool _isDefault = true;

        [Header("Optional Overrides")]
        [SerializeField]
        ModioUISearchSettings _searchOnStart;
        [SerializeField] int _defaultPageSize = 24;
        [SerializeField, Tooltip("Allow search to run before we have an authenticated user")]
        bool _allowSearchWithoutUser;

        public static ModioUISearch Default { get; private set; }

        public ModioUISearchSettings LastSearchSettingsFrom { get; private set; }
        ModioUISearchSettings _resetToSearchSettingsFrom;

        public int DefaultPageSize => _defaultPageSize;
        public ModioSearch ModioSearch { get; private set; }

        public UnityEvent OnSearchUpdatedUnityEvent;

        public event Action AppliedSearchPreset;

        void Awake()
        {
            ModioSearch ??= new ModioSearch(DefaultPageSize);
            ModioSearch.OnSearchUpdated += () => OnSearchUpdatedUnityEvent?.Invoke();
            if (_isDefault || Default == null) Default = this;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= PluginReady;

            //Ensure we clear the instance, so GC can clean up this object
            if (Default == this) Default = null;
            
            User.OnUserChanged -= PluginReady;
        }

        void Start()
        {
            ModioClient.OnInitialized += PluginReady;

        }

        void PluginReady(User _) => PluginReady();
        void PluginReady()
        {
            User.OnUserChanged -= PluginReady;
            
            if (!_allowSearchWithoutUser && (User.Current == null || !User.Current.IsInitialized))
            {
                ModioSearch.ClearCurrentResultsImmediately();
                User.OnUserChanged += PluginReady;
                return;
            }

            if (!ModioSearch.TryClearSearch())
            {
                if (_searchOnStart != null)
                    _searchOnStart.Search(this);
                else
                {
                    ModioSearch.ClearCurrentResultsImmediately();
                }
            }
            
            ApplyHiddenTags().ForgetTaskSafely();
        }

        public void AddUpdatePropertiesListener(UnityAction listener)
        {
            OnSearchUpdatedUnityEvent.AddListener(listener);
        }

        public void RemoveUpdatePropertiesListener(UnityAction listener)
        {
            OnSearchUpdatedUnityEvent.RemoveListener(listener);
        }

        public void SetSearch(
            ModSearchFilter searchFilter,
            SpecialSearchType specialSearchType,
            bool resetToThis = false,
            object shareFiltersWith = null,
            ModioUISearchSettings settingsFrom = null
        ) {
            LastSearchSettingsFrom = settingsFrom;
            if (resetToThis) _resetToSearchSettingsFrom = settingsFrom;
            
            //It's possible that this is called before Awake if the GameObject is disabled in hierarchy
            ModioSearch ??= new ModioSearch(DefaultPageSize);

            ModioSearch.SetCollection(settingsFrom?.CollectionId ?? 0);
            
            ModioSearch.SetSearch(searchFilter, specialSearchType, resetToThis, shareFiltersWith, _allowSearchWithoutUser);
            
            if (settingsFrom != null && _isDefault)
            {
                ApplyHiddenTags().ForgetTaskSafely();
            }
            
            AppliedSearchPreset?.Invoke();
        }

        async Task ApplyHiddenTags()
        {
            if (!ModioClient.IsInitialized)
            {
                ModioLog.Verbose?.Log($"Skipping {nameof(ApplyHiddenTags)} as mod.io isn't initialized");
                return;
            }
            
            (Error error, GameTagCategory[] gameTagCategories) = await GameTagCategory.GetGameTagOptions();

            if (LastSearchSettingsFrom == null || gameTagCategories == null) return;
            
            foreach (GameTagCategory gameTagCategory in gameTagCategories)
            {
                bool hide = LastSearchSettingsFrom.hideTagCategories.Contains(gameTagCategory.Name);

                gameTagCategory.TempHidden = hide;

                foreach (ModTag tag in gameTagCategory.Tags)
                {
                    tag.TempHidden = hide;
                }
            }
            
            OnSearchUpdatedUnityEvent.Invoke();
        }
        
        public void ClearSearch()
        {
            if(_resetToSearchSettingsFrom != null)
                LastSearchSettingsFrom = _resetToSearchSettingsFrom;
            ModioSearch.ClearSearch();
            AppliedSearchPreset?.Invoke();
        }

        public bool HasCustomSearch() => ModioSearch.HasCustomSearch() || _resetToSearchSettingsFrom != LastSearchSettingsFrom;
    }
}
