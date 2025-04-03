using System;
using System.Linq;
using Modio.Unity.UI.Components.Selectables.Transitions;
using Modio.Unity.UI.Input;
using UnityEngine;

namespace Modio.Unity.UI.Components.Selectables
{
    public class ModioUISelectableTransitions : MonoBehaviour
    {
        public enum ToggleFilter
        {
            Any     = OnlyOn | OnlyOff,
            OnlyOn  = 0b01,
            OnlyOff = 0b10,
        }

        [SerializeField,
         Tooltip(
             "Use to limit transitions to a toggle value.\ne.g. \"Only On\" will only trigger if the toggle is on. "
         )]
        ToggleFilter _toggleFilter = ToggleFilter.Any;
        [SerializeReference] ISelectableTransition[] _transitions;
        IPropertyMonoBehaviourEvents[] _monoBehaviourEvents;

        IModioUISelectable _owner;
        ModioUIToggle _toggle;

        void Awake()
        {
            _owner = GetComponentInParent<IModioUISelectable>();
            _toggle = _owner as ModioUIToggle;

            _monoBehaviourEvents = _transitions.Any(property => property is IPropertyMonoBehaviourEvents)
                ? _transitions.OfType<IPropertyMonoBehaviourEvents>().ToArray()
                : Array.Empty<IPropertyMonoBehaviourEvents>();

            if (_owner == null && enabled)
            {
                Debug.Log(
                    $"{GetType().Name} {gameObject.name} could not find an {nameof(IModioUISelectable)}, disabling.",
                    this
                );

                enabled = false;
            }
        }

        void Start()
        {
            foreach (var monoBehaviourEvents in _monoBehaviourEvents) monoBehaviourEvents.Start();
        }

        void OnEnable()
        {
            foreach (var monoBehaviourEvents in _monoBehaviourEvents) monoBehaviourEvents.OnEnable();

            if (_owner != null)
            {
                _owner.StateChanged += OnSelectionStateChanged;
                OnSelectionStateChanged(_owner.State, true);
            }
        }

        void OnDisable()
        {
            if (_owner != null) _owner.StateChanged -= OnSelectionStateChanged;
            ModioUIInput.SwappedControlScheme -= OnSwappedToController;

            foreach (var monoBehaviourEvents in _monoBehaviourEvents) monoBehaviourEvents.OnDisable();
        }

        void OnDestroy()
        {
            if (_owner != null) _owner.StateChanged -= OnSelectionStateChanged;
            ModioUIInput.SwappedControlScheme -= OnSwappedToController;

            foreach (var monoBehaviourEvents in _monoBehaviourEvents) monoBehaviourEvents.OnDestroy();
        }

        void OnSelectionStateChanged(IModioUISelectable.SelectionState state, bool instant)
        {
            if (_toggle != null)
            {
                if (!_toggleFilter.HasFlag(_toggle.isOn ? ToggleFilter.OnlyOn : ToggleFilter.OnlyOff)) return;
            }
            else if (_toggleFilter == ToggleFilter.OnlyOn) //Treat buttons like toggles that are always off
                return;

            ModioUIInput.SwappedControlScheme -= OnSwappedToController;
            if (state == IModioUISelectable.SelectionState.Highlighted)
            {
                if(ModioUIInput.IsUsingGamepad)
                    state = IModioUISelectable.SelectionState.Normal;
                ModioUIInput.SwappedControlScheme += OnSwappedToController;
            }

            foreach (ISelectableTransition transition in _transitions)
                transition.OnSelectionStateChanged(state, instant);
        }
        void OnSwappedToController(bool isController)
        {
            if (_owner != null && _owner.State == IModioUISelectable.SelectionState.Highlighted)
                OnSelectionStateChanged(IModioUISelectable.SelectionState.Highlighted, false);
        }
    }
}
