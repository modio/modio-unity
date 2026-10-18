using System;
using Modio.Extensions;
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

        public IModioSearchSettings LastSearchSettingsFrom => ModioSearch.LastSearchSettingsFrom;

        public int DefaultPageSize => _defaultPageSize;
        public ModioSearch ModioSearch => _modioSearch ??= new ModioSearch(DefaultPageSize, _isDefault);
        ModioSearch _modioSearch;

        public UnityEvent OnSearchUpdatedUnityEvent;

        void Awake()
        {
            ModioSearch.OnSearchUpdated += () => OnSearchUpdatedUnityEvent?.Invoke();
            if (_isDefault || Default == null) Default = this;
        }

        void Start()
        {
            ModioClient.OnInitialized += PluginReady;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= PluginReady;

            //Ensure we clear the instance, so GC can clean up this object
            if (Default == this) Default = null;
            
            User.OnUserChanged -= PluginReady;
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
                    SetSearch(_searchOnStart, true);
                else
                    ModioSearch.ClearCurrentResultsImmediately();
            }
        }

        public void AddUpdatePropertiesListener(UnityAction listener) => OnSearchUpdatedUnityEvent.AddListener(listener);

        public void RemoveUpdatePropertiesListener(UnityAction listener) => OnSearchUpdatedUnityEvent.RemoveListener(listener);

        public void SetSearch(IModioSearchSettings settingsFrom, bool resetToThis = false)
        {
            ModioSearch.SetSearch(settingsFrom, resetToThis, _allowSearchWithoutUser);
        }

        public void ClearSearch() => ModioSearch.ClearSearch();

        public bool HasCustomSearch() => ModioSearch.HasCustomSearch();
    }
}
