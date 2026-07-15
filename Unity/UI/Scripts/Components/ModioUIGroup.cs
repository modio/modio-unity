using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;
using Modio.Unity.UI.Components.SearchProperties;
using Modio.Unity.UI.Navigation;
using Modio.Unity.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// This component will instantiate and assign a list of <typeparamref name="TResource"/>.
    /// It automatically grabs the first <typeparamref name="TModioUIContainer"/> on its children as a template,
    /// and instantiates more siblings for that template as needed.
    /// 
    /// You can hook it up to a search with a <see cref="SearchPropertyDisplayResults"/>.
    /// 
    /// Typically, the parent of the template UIMod will have a layoutGroup of some kind.
    /// If the UIModGroup is able to recognise it, and it's within a scrollview, it will
    /// use placeholders when the mods are offscreen and dynamically swap them out as
    /// they near the visible area.
    /// </summary>
    /// <typeparam name="TResource">The resource to own, such as a Mod or Collection</typeparam>
    /// <typeparam name="TModioUIContainer">e.g. a <see cref="ModioUIMod"/> or <see cref="ModioUICollection"/></typeparam>
    public class ModioUIGroup<TResource, TModioUIContainer> : MonoBehaviour 
        where TModioUIContainer : MonoBehaviour, IModioUIPropertiesOwner, IModioUIResourceContainer<TResource>
        where TResource : IModioInfo
    {
        static readonly Dictionary<TResource, TModioUIContainer> TempActive = new Dictionary<TResource, TModioUIContainer>();

        [SerializeField]
        TModioUIContainer _template;

        [SerializeField] bool _useCarouselSelectionLogic;

        readonly List<TModioUIContainer> _active = new();
        readonly Stack<TModioUIContainer> _inactive = new();
        readonly Stack<TModioUIContainer> _inactivePlaceholders = new();

        (IReadOnlyList<TResource> mods, int selectionIndex) _displayOnEnable;

        [SerializeField, Tooltip("(Optional) The root layout to rebuild before performing selections"),]
        RectTransform _layoutRebuilder;
        
        [SerializeField, Tooltip("Should we rebuild the layoutRebuilder and redo placeholder logic? Useful if a ScrollRect viewport will change size in response to child count"),]
        bool _rebuildAndReapplyPlaceholders;
        
        ScrollRect _scrollRect;
        LayoutGroup _layoutGroup;

        void Awake()
        {
            if(_template == null)
                _template = GetComponentInChildren<TModioUIContainer>();

            if (_template != null)
            {
                _scrollRect = _template.GetComponentInParent<ScrollRect>();

                if (_scrollRect != null) 
                    _scrollRect.onValueChanged.AddListener(OnScrolled);

                Transform transformParent = _template.transform.parent;
                if (transformParent != null) _layoutGroup = transformParent.GetComponentInParent<LayoutGroup>();

                _template.gameObject.SetActive(false);
                _inactive.Push(_template);
            }
            else
            {
                Debug.LogWarning(
                    $"{nameof(ModioUIGroup<TResource, TModioUIContainer>)} {gameObject.name} could not find a child {nameof(TModioUIContainer)} template, disabling.",
                    this
                );

                enabled = false;
            }
        }

        void OnEnable()
        {
            if (_displayOnEnable.mods != null)
            {
                SetMods(_displayOnEnable.mods, _displayOnEnable.selectionIndex);
                _displayOnEnable = default;
            }
        }

        public void SetMods(IReadOnlyList<TResource> mods, int selectionIndex = 0)
        {
            if (!enabled)
            {
                _displayOnEnable = (mods, selectionIndex);

                return;
            }

            //Treat a null mod list as an empty mod list
            mods ??= Array.Empty<TResource>();

            TempActive.Clear();

            foreach (TModioUIContainer uiMod in _active)
            {
                if (uiMod.IsPlaceholderUI 
                    || !mods.Contains(uiMod.Resource)
                    || !TempActive.TryAdd(uiMod.Resource, uiMod))
                {
                    Repool(uiMod);
                }
            }

            _active.Clear();

            var containWithin = _scrollRect == null ? transform : _scrollRect.transform;
            bool shouldUsePlaceholdersAtAll = _scrollRect != null;
            ModioRectHelper.GetWorldAABB((RectTransform)containWithin, out var scrollMin, out var scrollMax);
            
            var size = scrollMax - scrollMin;
            scrollMin -= size;
            scrollMax += size;
            
            var eventSystem = EventSystem.current;
            GameObject currentSelectedGameObject = null;

            if (eventSystem != null)
                currentSelectedGameObject = eventSystem.currentSelectedGameObject;
            else
                ModioLog.Error?.Log(
                    "You are missing an event system, which the Modio UI requires to work. Consider adding ModioUI_InputCapture to your scene"
                );
            
            Vector3 selectionMax = Vector3.zero;
            Vector3 selectionMin = Vector3.zero;

            if (currentSelectedGameObject != null)
            {
                ModioRectHelper.GetWorldAABB((RectTransform)currentSelectedGameObject.transform, out selectionMin, out selectionMax);
                
                selectionMin -= Vector3.one * 100;
                selectionMax += Vector3.one * 100;
            }

            for (var i = 0; i < mods.Count; i++)
            {
                bool active = TempActive.Remove(mods[i], out TModioUIContainer uiMod);

                bool shouldUsePlaceholder = shouldUsePlaceholdersAtAll && i != 0 && i != selectionIndex && (!active || currentSelectedGameObject == null || currentSelectedGameObject != uiMod.gameObject);

                (Vector3 min, Vector3 max) = GetApproximatePositionFor(i, (RectTransform)_template.transform);
                
                shouldUsePlaceholder &= min.x > scrollMax.x ||
                                        max.x < scrollMin.x ||
                                        min.y > scrollMax.y ||
                                        max.y < scrollMin.y;
                shouldUsePlaceholder &= min.x > selectionMax.x ||
                                        max.x < selectionMin.x ||
                                        min.y > selectionMax.y ||
                                        max.y < selectionMin.y;
                
                if (active && shouldUsePlaceholder)
                {
                    Repool(uiMod);

                    active = false;
                    uiMod = null;
                }

                if (!active)
                {
                    uiMod = GetFromPoolOrCreate(shouldUsePlaceholder);

                    uiMod.SetResource(mods[i]);
                }

                uiMod.transform.SetSiblingIndex(i);
                if (!active) uiMod.gameObject.SetActive(true);

                _active.Add(uiMod);
            }

            if (eventSystem == null) return;

            var shouldDoSelection = currentSelectedGameObject == null || !currentSelectedGameObject.activeInHierarchy;

            if (!shouldDoSelection && _active.Count > 0 && selectionIndex == 0)
            {
                // Force the selection if we have a child selected, and we should be setting to index 0 (as it's a new, non additive, search)
                shouldDoSelection |= currentSelectedGameObject.transform.parent == _active[0].transform.parent;

                // Hack that allows the first carousel to steal input selection priority from other carousels
                if (_useCarouselSelectionLogic && transform.GetSiblingIndex() == 0)
                {
                    var panel = transform.GetComponentInParent<ModioPanelBase>();
                    shouldDoSelection |= panel != null && panel.HasFocus;
                }
            }
            
            //Ensure layouts have been applied, otherwise we'll snap scrollviews to their old positions
            if ((shouldDoSelection || _rebuildAndReapplyPlaceholders) && _layoutRebuilder != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRebuilder);

                if (_rebuildAndReapplyPlaceholders)
                    EnsurePlaceholdersCorrect();
            }

            if (shouldDoSelection)
            {
                var currentFocusedPanel = ModioPanelManager.GetInstance().CurrentFocusedPanel;
                if (_active.Count > 0)
                {
                    currentFocusedPanel.SetSelectedGameObject(
                        _active[Mathf.Min(selectionIndex, _active.Count - 1)].gameObject
                    );
                }
                else
                {
                    if (currentFocusedPanel != null) currentFocusedPanel.DoDefaultSelectionAfterDelayIfStillNeeded();
                }
            }
        }

        (Vector3 min, Vector3 max) GetApproximatePositionFor(int i, RectTransform uiModTransform)
        {
            ModioRectHelper.GetWorldAABB(uiModTransform, out Vector3 min, out Vector3 max);

            var size = max - min;
            
            if (_layoutGroup is HorizontalLayoutGroup horizontalLayout)
            {
                min.x = i * (horizontalLayout.spacing + size.x);
                max = min + size;
            }
            else if (_layoutGroup is VerticalLayoutGroup verticalLayout)
            {
                min.y = i * (verticalLayout.spacing + size.y);
                max = min + size;
            }
            else if (_layoutGroup is GridLayoutGroup gridLayout)
            {
                var gridLayoutTransform = (RectTransform)gridLayout.transform;
                ModioRectHelper.GetWorldAABB(gridLayoutTransform, out Vector3 gridMin, out Vector3 gridMax);

                var cellCountX = Mathf.Max(
                    1,
                    Mathf.FloorToInt((gridLayoutTransform.rect.width - gridLayout.padding.horizontal + gridLayout.spacing.x + 0.001f)
                                     / (gridLayout.cellSize.x + gridLayout.spacing.x))
                );

                var scale = gridLayoutTransform.lossyScale; 

                min.x = gridMin.x + (i % cellCountX) * (gridLayout.cellSize.x + gridLayout.spacing.x) * scale.x;
                max.x = min.x + size.x;
                max.y = gridMax.y - (i / cellCountX) * (gridLayout.cellSize.y + gridLayout.spacing.y) * scale.y;
                min.y = max.y - size.y;
            }
            
            return (min, max);
        }

        TModioUIContainer GetFromPoolOrCreate(bool shouldUsePlaceholder)
        {
            TModioUIContainer uiMod;
            if (shouldUsePlaceholder)
            {
                if (!_inactivePlaceholders.TryPop(out uiMod))
                {
                    var go = new GameObject("Placeholder", typeof(RectTransform), typeof(TModioUIContainer));
                    go.transform.SetParent(_template.transform.parent);
                    go.transform.localScale =  Vector3.one;
                    uiMod = go.GetComponent<TModioUIContainer>();

                    uiMod.IsPlaceholderUI = true;

                    var modioAspectRatioLayout = _template.GetComponent<ModioAspectRatioLayout>();

                    if(modioAspectRatioLayout)
                        go.AddComponent<ModioAspectRatioLayout>().CopySettingsFrom(modioAspectRatioLayout);

                    var layoutElement = go.AddComponent<LayoutElement>();
                    var templateTransform = (RectTransform)_template.transform;
                    layoutElement.preferredHeight = templateTransform.rect.height;
                    layoutElement.preferredWidth = templateTransform.rect.width;
                    layoutElement.minHeight = templateTransform.rect.height;
                    layoutElement.minWidth = templateTransform.rect.width;
                }

                return uiMod;
            }

            if (!_inactive.TryPop(out uiMod))
            {
                uiMod = Instantiate(_template.gameObject, _template.transform.parent)
                    .GetComponent<TModioUIContainer>();
            }

            return uiMod;
        }

        void Repool(TModioUIContainer uiMod)
        {
            uiMod.gameObject.SetActive(false);
            uiMod.SetResource(default(TResource));
            if(uiMod.IsPlaceholderUI)
                _inactivePlaceholders.Push(uiMod);
            else
                _inactive.Push(uiMod);
        }

        void OnScrolled(Vector2 _)
        {
            EnsurePlaceholdersCorrect();
        }

        void EnsurePlaceholdersCorrect()
        {
            ModioRectHelper.GetWorldAABB((RectTransform)_scrollRect.transform, out var scrollMin, out var scrollMax);
            
            //Expand the area by 100% to each side, to allow selection and scrolling logic to work smoothly
            var size = scrollMax - scrollMin;
            scrollMin -= size;
            scrollMax += size;
            
            var currentSelectedGameObject = EventSystem.current?.currentSelectedGameObject;

            Vector3 selectionMax = Vector3.zero;
            Vector3 selectionMin = Vector3.zero;

            if (currentSelectedGameObject != null)
            {
                ModioRectHelper.GetWorldAABB((RectTransform)currentSelectedGameObject.transform, out selectionMin, out selectionMax);
                
                selectionMin -= Vector3.one * 100;
                selectionMax += Vector3.one * 100;
            }
            
            for (int i = 0; i < _active.Count; i++)
            {
                TModioUIContainer currentContainer = _active[i];    

                var shouldBePlaceholder = i > 0;

                ModioRectHelper.GetWorldAABB((RectTransform)currentContainer.transform, out var min, out var max);

                shouldBePlaceholder &= min.x > scrollMax.x ||
                                       max.x < scrollMin.x ||
                                       min.y > scrollMax.y ||
                                       max.y < scrollMin.y;
                shouldBePlaceholder &= min.x > selectionMax.x ||
                                       max.x < selectionMin.x ||
                                       min.y > selectionMax.y ||
                                       max.y < selectionMin.y;

                if(currentContainer.IsPlaceholderUI == shouldBePlaceholder) continue;
                
                TModioUIContainer newContainer = GetFromPoolOrCreate(shouldBePlaceholder);
                newContainer.transform.SetSiblingIndex(i);
                newContainer.SetResource(currentContainer.Resource);
                newContainer.gameObject.SetActive(true);
                _active[i] = newContainer;
                
                currentContainer.transform.SetAsLastSibling();
                Repool(currentContainer);
            }
        }
    }
}
