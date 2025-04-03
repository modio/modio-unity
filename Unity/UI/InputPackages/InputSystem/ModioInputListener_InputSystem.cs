using System.Collections.Generic;
using System.Linq;
using Modio.Unity.UI.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Modio.Unity.UI.InputSystem
{
    public class ModioInputListener_InputSystem : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        ModioUIBrowserControls _modioUIBrowserControls;
        ModioUIPromptIconResolver _modioUIPromptIconResolver;

        readonly Dictionary<ModioUIInput.ModioAction, InputAction> _actionMap =
            new Dictionary<ModioUIInput.ModioAction, InputAction>();
        int _lastInputFrame;
        string _deviceLayout;

        void Awake()
        {
            ModioUIInput.SuppressNoInputListenerWarning = true;

            _modioUIBrowserControls = new ModioUIBrowserControls();

            _modioUIPromptIconResolver = GetComponentInParent<ModioUIPromptIconResolver>();

            if(Application.isConsolePlatform)
                ModioUIInput.ControlSchemeChanged(true);

            var genericActions = _modioUIBrowserControls.Generic;

            Register(genericActions.Cancel,              ModioUIInput.ModioAction.Cancel);
            Register(genericActions.Cancel,              ModioUIInput.ModioAction.SearchClear);
            Register(genericActions.Subscribe,           ModioUIInput.ModioAction.Subscribe);
            Register(genericActions.Report,              ModioUIInput.ModioAction.Report);
            Register(genericActions.Filter,              ModioUIInput.ModioAction.Filter);
            Register(genericActions.Sort,                ModioUIInput.ModioAction.Sort);
            Register(genericActions.Search,              ModioUIInput.ModioAction.Search);
            Register(genericActions.TabLeft,             ModioUIInput.ModioAction.TabLeft);
            Register(genericActions.TabRight,            ModioUIInput.ModioAction.TabRight);
            Register(genericActions.BuyTokens,           ModioUIInput.ModioAction.BuyTokens);
            Register(genericActions.FilterLeft,          ModioUIInput.ModioAction.FilterLeft);
            Register(genericActions.FilterRight,         ModioUIInput.ModioAction.FilterRight);
            Register(genericActions.FilterClear,         ModioUIInput.ModioAction.FilterClear);
            Register(genericActions.MoreOptions,         ModioUIInput.ModioAction.MoreOptions);
            Register(genericActions.SearchPageLeft,      ModioUIInput.ModioAction.SearchPageLeft);
            Register(genericActions.SearchPageRight,     ModioUIInput.ModioAction.SearchPageRight);
            Register(genericActions.MoreFromThisCreator, ModioUIInput.ModioAction.MoreFromThisCreator);
            Register(genericActions.DeveloperMenu,       ModioUIInput.ModioAction.DeveloperMenu);

            ModioUIInput.RawCursorProvider = () => new Vector2(0, genericActions.ScrollDescription.ReadValue<float>());

            void Register(InputAction inputAction, ModioUIInput.ModioAction modioAction)
            {
                inputAction.performed += context => AttemptPress(modioAction);
                _actionMap[modioAction] = inputAction;
                UpdateBindings(inputAction, modioAction);
            }
        }

        void AttemptPress(ModioUIInput.ModioAction modioAction)
        {
            _lastInputFrame = Time.frameCount;
            ModioUIInput.PressedAction(modioAction);
        }

        void UpdateBindings(InputAction inputAction, ModioUIInput.ModioAction modioAction)
        {
            List<string> strings = null;
            List<Sprite> images = null;

            foreach (var inputActionBinding in inputAction.bindings)
            {
                var bindingDisplayString =
                    inputActionBinding.ToDisplayString(out var deviceLayout, out var controlPath);

                // If it's a gamepad, look up an icon for the control.
                Sprite icon = null;

                var isGamepadBinding = !string.IsNullOrEmpty(deviceLayout) &&
                                       !string.IsNullOrEmpty(controlPath) &&
                                       UnityEngine.InputSystem.InputSystem.IsFirstLayoutBasedOnSecond(deviceLayout, "Gamepad");

                if (ModioUIInput.IsUsingGamepad
                    && inputActionBinding is { groups: "Controller", path: "*/{Cancel}" })
                {
                    isGamepadBinding = true;
                    bool isNintendo = !string.IsNullOrEmpty(_deviceLayout) && UnityEngine.InputSystem.InputSystem.IsFirstLayoutBasedOnSecond(_deviceLayout, "NPad");
                    controlPath = isNintendo ? "buttonSouth" : "buttonEast";
                }

                if (isGamepadBinding != ModioUIInput.IsUsingGamepad)
                {
                    continue;
                }

                if (isGamepadBinding)
                {
                    icon = _modioUIPromptIconResolver.ResolveIcon(controlPath, Application.platform);

                    if (icon == null)
                    {
                        var forPlatform = RuntimePlatform.XboxOne;

                        if (!string.IsNullOrEmpty(_deviceLayout) && UnityEngine.InputSystem.InputSystem.IsFirstLayoutBasedOnSecond(_deviceLayout, "DualShockGamepad"))
                            forPlatform = RuntimePlatform.PS4;

                        icon = _modioUIPromptIconResolver.ResolveIcon(controlPath, forPlatform);
                    }
                }
                else
                {
                    string overrideDisplayString;
                    (icon, overrideDisplayString) = _modioUIPromptIconResolver.TryGetKeyboardIcon(controlPath);

                    if (!string.IsNullOrEmpty(overrideDisplayString)) bindingDisplayString = overrideDisplayString;
                }

                if (icon != null)
                {
                    images ??= new List<Sprite>();
                    images.Add(icon);
                }
                else
                {
                    strings ??= new List<string>();
                    strings.Add(bindingDisplayString);
                }
            }

            ModioUIInput.SetButtonPrompts(modioAction, strings, images);
        }

        void OnEnable()
        {
            _modioUIBrowserControls.Enable();

            InputUser.onChange += OnInputUserChanged;
        }

        void OnDisable()
        {
            _modioUIBrowserControls.Disable();

            InputUser.onChange -= OnInputUserChanged;
        }

        void Update()
        {
            if (ModioUIInput.IsUsingGamepad)
            {
                const float mouseSpeedThreshold = 100f;

                if (_modioUIBrowserControls.Generic.DetectMouseInput.WasPressedThisFrame() ||
                    Mathf.Abs(_modioUIBrowserControls.Generic.DetectMouseMovement.ReadValue<float>()) >
                    Time.unscaledDeltaTime * mouseSpeedThreshold)
                {
                    PlayerInput playerByIndex = PlayerInput.GetPlayerByIndex(0);
                    playerByIndex.SwitchCurrentControlScheme(_modioUIBrowserControls.KeyboardScheme.name);
                }
            }
        }

        void OnInputUserChanged(InputUser user, InputUserChange change, InputDevice device)
        {
            if (change == InputUserChange.ControlSchemeChanged)
            {
                if (user.controlScheme != null)
                {
                    bool isController = user.controlScheme.Value.name == "Controller";

                    // Don't allow keyboard prompts on console platforms
                    if (!isController && Application.isConsolePlatform)
                        return;

                    ModioUIInput.ControlSchemeChanged(isController);

                    _deviceLayout = user.pairedDevices.FirstOrDefault()?.layout;

                    UpdateAllBindings();
                }
            }
        }

        void UpdateAllBindings()
        {
            foreach (var actionPair in _actionMap)
            {
                UpdateBindings(actionPair.Value, actionPair.Key);
            }
        }
#endif
    }
}
