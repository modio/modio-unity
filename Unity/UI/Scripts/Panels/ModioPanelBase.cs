using System;
using Modio.Unity.UI.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modio.Unity.UI.Panels
{
    public abstract class ModioPanelBase : MonoBehaviour
    {
        /// <summary>
        /// Chosen by ModioPanelManager based on how a panel gains focus
        /// e.g. when a panel is opened the first time (selectOnOpen), vs when it regains focus from
        /// a panel covering it being popped (selectLast).
        /// </summary>
        public enum GainedFocusCause
        {
            OpeningFromClosed,
            RegainingFocusFromStackedPanel,
            InputSuppressionChangeOnly,
        }

        [SerializeField] GameObject _panelToEnable;

        [SerializeField] Selectable _selectOnOpen;

        [SerializeField] bool _startHidden;

        [SerializeField] ModioPanelBase _openOnTopOf;

        GameObject _lastSelectedGameObject;
        public bool HasFocus { get; private set; }

        public event Action<bool> OnHasFocusChanged;

        protected virtual void Awake()
        {
            ModioPanelManager.GetInstance().RegisterPanel(this);
        }

        protected virtual void Start()
        {
            if (_startHidden && !HasFocus)
            {
                if (_panelToEnable == null)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    _panelToEnable.SetActive(false);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (HasFocus) OnLostFocus();
        }

        public void OpenPanel()
        {
            var transformParent = transform.parent;
            if (transformParent != null && !transformParent.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"Attempted to open panel {this} with disabled parent. Suppressing this to avoid lost input.");
                return;
            }

            if (_panelToEnable == null)
            {
                gameObject.SetActive(true);
            }
            else
            {
                _panelToEnable.SetActive(true);
            }

            if (_openOnTopOf != null)
            {
                if (_openOnTopOf._panelToEnable == null
                        ? !gameObject.activeSelf
                        : !_openOnTopOf._panelToEnable.activeSelf)
                {
                    _openOnTopOf.OpenPanel();
                }
            }

            ModioPanelManager.GetInstance().OpenPanel(this);
        }

        public void ClosePanel()
        {
            ModioPanelManager.GetInstance().ClosePanel(this);

            if (_panelToEnable == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                _panelToEnable.SetActive(false);
            }
        }

        public virtual void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            HasFocus = true;
            ModioUIInput.AddHandler(ModioUIInput.ModioAction.Cancel, CancelPressed);
            ModioUIInput.SwappedControlScheme += OnSwappedControlScheme;

            if (selectionBehaviour == GainedFocusCause.RegainingFocusFromStackedPanel &&
                _lastSelectedGameObject != null &&
                _lastSelectedGameObject.activeInHierarchy &&
                !EventSystem.current.alreadySelecting)
            {
                SetSelectedGameObject(_lastSelectedGameObject);
                NewSelectionWhileFocused(_lastSelectedGameObject);
            }
            else if (selectionBehaviour != GainedFocusCause.InputSuppressionChangeOnly)
            {
                DoDefaultSelection();
            }

            OnHasFocusChanged?.Invoke(true);
        }

        public virtual void OnLostFocus()
        {
            HasFocus = false;
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.Cancel, CancelPressed);
            ModioUIInput.SwappedControlScheme -= OnSwappedControlScheme;

            OnHasFocusChanged?.Invoke(false);
        }

        protected virtual void CancelPressed()
        {
            ClosePanel();
        }

        public virtual void DoDefaultSelection()
        {
            if (_selectOnOpen != null)
            {
                _selectOnOpen.Select();
                NewSelectionWhileFocused(_selectOnOpen.gameObject);
            }
        }

        public virtual void FocusedPanelLateUpdate()
        {
            var currentSelection = EventSystem.current.currentSelectedGameObject;

            if (currentSelection != null && currentSelection.activeInHierarchy)
            {
                if (!ReferenceEquals(_lastSelectedGameObject, currentSelection))
                {
                    NewSelectionWhileFocused(currentSelection);
                }
            }
            else if (ModioUIInput.IsUsingGamepad)
            {
                DoDefaultSelection();
            }
        }

        public virtual void SetSelectedGameObject(GameObject selection)
        {
            EventSystem.current.SetSelectedGameObject(selection);
            // Replacing with the following will partially disable selection when not using controller
            /*if(ModioUIInput.IsUsingGamepad)
                EventSystem.current.SetSelectedGameObject(selection);
            else
                OverrideLastSelectedGameObject(selection);*/
        }

        /// <summary>
        /// Allow overriding the last selected gameobject, without actually selecting it
        /// (Used when pushing something over this panel, and wanting to change what is selected when regaining focus)
        /// </summary>
        public void OverrideLastSelectedGameObject(GameObject selection)
        {
            _lastSelectedGameObject = selection;
        }

        protected virtual void NewSelectionWhileFocused(GameObject currentSelection)
        {
            _lastSelectedGameObject = currentSelection;
        }

        void OnSwappedControlScheme(bool isController)
        {
            if (isController)
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    //We already have a selection, don't worry about this
                    return;
                }

                float closestDistSqr = float.MaxValue;
                Selectable closest = null;
                Vector3 closestTo = UnityEngine.Input.mousePosition;

                //Look at all selectables that are on this panel. Find the one closest to the mouse that isn't obscured
                foreach (Selectable selectable in GetComponentsInChildren<Selectable>())
                {
                    if (selectable.gameObject.activeInHierarchy &&
                        selectable.navigation.mode != UnityEngine.UI.Navigation.Mode.None &&
                        selectable.interactable)
                    {
                        var rectTransform = selectable.transform as RectTransform;

                        if (rectTransform == null) continue;

                        var centerPoint = rectTransform.TransformPoint(rectTransform.rect.center);

                        var sqrDist = (centerPoint - closestTo).sqrMagnitude;

                        if (sqrDist > closestDistSqr) continue;

                        //if (ModioUiVisibilityTester.IsSelectableUnobscured(selectable, centerPoint))
                        {
                            closest = selectable;
                            closestDistSqr = sqrDist;
                        }
                    }
                }

                if (closest != null) closest.Select();
            }
        }
    }
}
