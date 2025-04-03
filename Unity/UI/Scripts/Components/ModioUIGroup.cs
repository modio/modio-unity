using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;
using Modio.Unity.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    public class ModioUIGroup : MonoBehaviour
    {
        static readonly Dictionary<Mod, ModioUIMod> TempActive = new Dictionary<Mod, ModioUIMod>();

        ModioUIMod _template;

        readonly List<ModioUIMod> _active = new List<ModioUIMod>();
        readonly List<ModioUIMod> _inactive = new List<ModioUIMod>();

        (IReadOnlyList<Mod> mods, int selectionIndex) _displayOnEnable;

        [SerializeField, Tooltip("(Optional) The root layout to rebuild before performing selections")]
        RectTransform _layoutRebuilder;

        void Awake()
        {
            _template = GetComponentInChildren<ModioUIMod>();

            if (_template != null)
            {
                _template.gameObject.SetActive(false);
                _inactive.Add(_template);
            }
            else
            {
                Debug.LogWarning(
                    $"{nameof(ModioUIGroup)} {gameObject.name} could not find a child {nameof(ModioUIMod)} template, disabling.",
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

        public void SetMods(IReadOnlyList<Mod> mods, int selectionIndex = 0)
        {
            if (!enabled)
            {
                _displayOnEnable = (mods, selectionIndex);

                return;
            }

            //Treat a null mod list as an empty mod list
            mods ??= Array.Empty<Mod>();

            TempActive.Clear();

            foreach (ModioUIMod uiMod in _active)
            {
                if (mods.Contains(uiMod.Mod) && !TempActive.ContainsKey(uiMod.Mod))
                    TempActive.Add(uiMod.Mod, uiMod);
                else
                {
                    uiMod.gameObject.SetActive(false);
                    uiMod.SetMod(null);

                    _inactive.Add(uiMod);
                }
            }

            _active.Clear();

            for (var i = 0; i < mods.Count; i++)
            {
                bool active = TempActive.Remove(mods[i], out ModioUIMod uiMod);

                if (!active)
                {
                    if (_inactive.Any())
                    {
                        int lastIndex = _inactive.Count - 1;
                        uiMod = _inactive[lastIndex];

                        _inactive.RemoveAt(lastIndex);
                    }
                    else
                    {
                        uiMod = Instantiate(_template.gameObject, _template.transform.parent)
                            .GetComponent<ModioUIMod>();
                    }

                    uiMod.SetMod(mods[i]);
                }

                uiMod.transform.SetSiblingIndex(i);
                if (!active) uiMod.gameObject.SetActive(true);

                _active.Add(uiMod);
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                ModioLog.Error?.Log("You are missing an event system, which the Modio UI requires to work. Consider adding ModioUI_InputCapture to your scene");
                return;
            }

            var currentSelectedGameObject = eventSystem.currentSelectedGameObject;
            var shouldDoSelection = currentSelectedGameObject == null || !currentSelectedGameObject.activeInHierarchy;

            if (!shouldDoSelection && _active.Count > 0 && selectionIndex == 0)
            {
                // Force the selection if we have a child selected, and we should be setting to index 0 (as it's a new, non additive, search)
                shouldDoSelection |= currentSelectedGameObject.transform.parent == _active[0].transform.parent;
            }

            if (shouldDoSelection)
            {
                //Ensure layouts have been applied, otherwise we'll snap scrollviews to their old positions
                if (_layoutRebuilder != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRebuilder);

                var currentFocusedPanel = ModioPanelManager.GetInstance().CurrentFocusedPanel;
                if (_active.Count > 0)
                {
                    currentFocusedPanel.SetSelectedGameObject(
                        _active[Mathf.Min(selectionIndex, _active.Count - 1)].gameObject
                    );
                }
                else
                {
                    if (currentFocusedPanel != null) currentFocusedPanel.DoDefaultSelection();
                }
            }
        }
    }
}
