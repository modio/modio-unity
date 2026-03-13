using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.Extensions;
using Modio.Mods;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    public class ModioUIFilterDisplay : MonoBehaviour
    {
        class TagEntry
        {
            public ModioUIToggle Toggle;
            public ModTag Tag;
        }

        [SerializeField] ModioUIToggle checkboxTagItemPrefab;
        List<TagEntry> checkboxTagItems = new List<TagEntry>();

        [SerializeField] ModioUIToggle radioTagItemPrefab;
        [SerializeField] ModioUIToggle categoryItemPrefab;

        [SerializeField] Transform _contentContainer;

        List<ModioUIFilterTagCategory> categoryItems = new List<ModioUIFilterTagCategory>();
        bool _hasRegisteredListener;
        bool _hasLocalChanges;
        bool? _showingCollectionTags;

        readonly HashSet<int> _tempHashSet = new();
        HashSet<int> _lastHidden;

        void Start()
        {
            ModioClient.OnInitialized += UpdateTags;
            if (!_hasRegisteredListener)
            {
                RegisterListener();
            }
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= UpdateTags;

        }

        void OnEnable()
        {
            RegisterListener();
        }

        void RegisterListener()
        {
            if (ModioUISearch.Default == null) return;
            _hasRegisteredListener = true;
            ModioUISearch.Default.OnSearchUpdatedUnityEvent.AddListener(UpdateActiveTags);
            UpdateActiveTags();
        }

        void OnDisable()
        {
            ModioUISearch.Default.OnSearchUpdatedUnityEvent.RemoveListener(UpdateActiveTags);
            _hasLocalChanges = false;
        }

        public GameObject GetDefaultSelection()
        {
            if (checkboxTagItems.Count <= 0) return null;

            return (checkboxTagItems.FirstOrDefault(t => t.Toggle.isOn) ?? checkboxTagItems.First())
                .Toggle.gameObject;
        }

        public void UpdateActiveTags()
        {
            //Don't override with the current tags if we're in the process of changing them
            if(_hasLocalChanges) return;

            var currentFilter = ModioUISearch.Default.LastSearchFilter;

            if (ModioClient.IsInitialized) 
                UpdateTags(ModioUISearch.Default.LastSearchPreset == SpecialSearchType.SearchCollections).ForgetTaskSafely();

            foreach (var tagItem in checkboxTagItems)
            {
                if(tagItem.Tag.TagType == ResourceTagType.CollectionCategory)
                    tagItem.Toggle.isOn = currentFilter.GetCollectionCategory() == tagItem.Tag.ApiName;
                else
                    tagItem.Toggle.isOn = currentFilter.GetTags().Contains(tagItem.Tag);
            }

            //Setting the toggles above will flag as local changes; reset that
            _hasLocalChanges = false;
        }

        public void ApplyFilter()
        {
            var tags = checkboxTagItems.Where(tagEntry => tagEntry.Toggle.isOn).Select(tagItem => tagItem.Tag);
            _hasLocalChanges = false;
            ModioUISearch.Default.ApplyTagsToSearch(tags);
        }

        public void ClearFilter()
        {
            foreach (var tagItem in checkboxTagItems)
            {
                tagItem.Toggle.isOn = false;
            }
            _hasLocalChanges = false;
        }

        void UpdateTags() => UpdateTags(false, true).ForgetTaskSafely();
        
        async Task UpdateTags(bool collectionTags, bool forceUpdate = false)
        {
            Error error;
            GameTagCategory[] tagCategories;
            if(collectionTags)
                (error, tagCategories) = await GameTagCategory.GetCollectionTagOptions();
            else
                (error, tagCategories) = await GameTagCategory.GetGameTagOptions();
            
            bool shouldUpdate = collectionTags != _showingCollectionTags || forceUpdate;

            _showingCollectionTags = collectionTags;
            
            _hasLocalChanges = false;

            if (error)
            {
                if(!error.IsSilent)
                    ModioLog.Error?.Log($"Unable to get tags {error}");
                return;
            }

            _tempHashSet.Clear();
            for (int index = 0; index < tagCategories.Length; index++)
            {
                var tagCategory = tagCategories[index];

                if (tagCategory.Hidden) _tempHashSet.Add(index);
            }

            if (_lastHidden == null || !_lastHidden.SetEquals(_tempHashSet))
            {
                shouldUpdate |= _lastHidden != null || _tempHashSet.Count > 0;
                _lastHidden = new HashSet<int>(_tempHashSet);
            }
            
            if (!shouldUpdate) return;

            HideListCheckboxItems(checkboxTagItems);
            HideListItems(ref categoryItems);

            foreach (var category in tagCategories)
            {
                if (category.Hidden) continue;

                var categoryItem = Instantiate(categoryItemPrefab, _contentContainer);
                categoryItem.gameObject.SetActive(true);
                categoryItem.transform.SetAsLastSibling();

                var categoryFilterToggle = categoryItem.GetComponent<ModioUIFilterTagCategory>();
                categoryFilterToggle.Setup(category);
                categoryItems.Add(categoryFilterToggle);

                var toggleGroup = categoryItem.GetComponentInChildren<ToggleGroup>();

                var childToggles = new List<ModioUIToggle>();

                categoryItem.isOn = true;

                categoryItem.onValueChanged.AddListener(
                    expanded =>
                    {
                        foreach (ModioUIToggle toggle in childToggles)
                        {
                            toggle.gameObject.SetActive(expanded);
                        }
                    }
                );

                foreach (var tag1 in category.Tags)
                {
                    ModioUIToggle item;

                    if (category.MultiSelect)
                    {
                        item = Instantiate(checkboxTagItemPrefab, _contentContainer);
                    }
                    else
                    {
                        item = Instantiate(radioTagItemPrefab, _contentContainer);
                        item.group = toggleGroup;
                    }

                    item.GetComponentInChildren<TMP_Text>().text = tag1.NameLocalized;

                    item.gameObject.SetActive(true);
                    item.transform.SetAsLastSibling();

                    item.onValueChanged.AddListener(
                        isOn =>
                        {
                            categoryFilterToggle.SetFilterCount(
                                categoryFilterToggle.CurrentFilterCount + (isOn ? 1 : -1)
                            );
                            _hasLocalChanges = true;
                        }
                    );

                    checkboxTagItems.Add(
                        new TagEntry
                        {
                            Toggle = item,
                            Tag = tag1,
                        }
                    );

                    childToggles.Add(item);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

            return;

            void HideListItems<T>(ref List<T> pool) where T : MonoBehaviour
            {
                foreach (T item in pool)
                {
                    Destroy(item.gameObject);
                }

                pool.Clear();
            }

            void HideListCheckboxItems(List<TagEntry> pool)
            {
                foreach (var item in pool)
                {
                    Destroy(item.Toggle.gameObject);
                }

                pool.Clear();
            }
        }
    }
}
