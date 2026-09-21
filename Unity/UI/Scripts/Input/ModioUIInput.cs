using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Modio.Unity.UI.Input
{
    public static class ModioUIInput
    {
        public class InputPromptDisplayInfo
        {
            public List<Sprite> Icons { get; private set; }
            public List<string> TextPrompts { get; private set; }
            
            public bool InputHasListeners { get; private set; }
            public string LocTextOverride { get; private set; }

            public event Action<InputPromptDisplayInfo> OnUpdated;

            public virtual void UpdateInfo(List<string> textPrompts, List<Sprite> icons, bool hasListeners, string locTextOverride)
            {
                if (textPrompts != null && textPrompts.Count > 0)
                {
                    TextPrompts ??= new List<string>();
                    TextPrompts.Clear();
                    TextPrompts.AddRange(textPrompts);
                    AnyBindingsExist = true;
                }
                else
                {
                    TextPrompts?.Clear();
                }

                if (icons != null && icons.Count > 0)
                {
                    Icons ??= new List<Sprite>();
                    Icons.Clear();
                    Icons.AddRange(icons);
                    AnyBindingsExist = true;
                }
                else
                {
                    Icons?.Clear();
                }

                InputHasListeners = hasListeners;
                LocTextOverride = locTextOverride;

                OnUpdated?.Invoke(this);
            }

            /// <summary>
            /// Update if the input has listeners, without changing the visuals displaying it
            /// </summary>
            public void UpdateListenerInfo(bool hasListeners, string locTextOverride)
            {
                if (InputHasListeners == hasListeners) return;

                InputHasListeners = hasListeners;
                LocTextOverride = locTextOverride;

                OnUpdated?.Invoke(this);
            }
        }

        static readonly Dictionary<ModioAction, List<(Action action, int frameAdded, string locTextOverride)>> Handlers = new();
        static readonly Dictionary<ModioAction, InputPromptDisplayInfo> Prompts = new();
        static readonly List<Action> CachedHandlersForCurrentCall = new List<Action>();

        public static Func<Vector2> RawCursorProvider;

        public static bool IsUsingGamepad { get; private set; }
        public static bool SuppressNoInputListenerWarning { get; set; }
        public static bool AnyBindingsExist { get; private set; }
        public static event Action<bool> SwappedControlScheme;

        public enum ModioAction
        {
            Cancel,
            Subscribe,
            Report,
            Filter,
            Sort,
            Search,
            TabLeft,
            TabRight,
            BuyTokens,
            FilterLeft,
            FilterRight,
            FilterClear,
            MoreOptions,
            SearchClear,
            SearchPageLeft,
            SearchPageRight,
            MoreFromThisCreator,
            DeveloperMenu,
            Logout,
            MoreOptionsCollection,
            LocalCollections,
            DynamicSelect,
            ContextualFastAction,
            LibraryEnable,
            LibraryDisable,
        }

        public static void PressedAction(ModioAction action)
        {
            if (!Handlers.TryGetValue(action, out var actionHandlers)) return;

            //Use a copy of the actionHandlers, so mutations won't impact the list
            CachedHandlersForCurrentCall.Clear();

            foreach (var actionHandler in actionHandlers)
            {
                if (actionHandler.frameAdded != Time.frameCount) CachedHandlersForCurrentCall.Add(actionHandler.action);
            }

            foreach (Action handler in CachedHandlersForCurrentCall)
            {
                handler?.Invoke();
            }

            CachedHandlersForCurrentCall.Clear();
        }

        public static void AddHandler(ModioAction action, Action onPressed, string locTextOverride = null)
        {
            if (!SuppressNoInputListenerWarning)
            {
                Debug.LogWarning("Modio's input system appears to be running without an input listener. "
                                 + "You might not have controller and full keyboard support. Ensure you have ModioUI_InputCapture added to your scene"
                                 + "\nIf you are using Unity's InputSystem, you can extract the following file: "
                                 + "\"Assets\\Plugins\\ModioUI\\InputPackages\\InputSystem\\ModioInputListener_InputSystem.zip\"");

                SuppressNoInputListenerWarning = true;
            }

            if (!Handlers.TryGetValue(action, out var actionHandlers))
            {
                actionHandlers = new List<(Action action, int frameAdded, string locTextOverride)>();
                Handlers[action] = actionHandlers;
            }

            actionHandlers.Add((onPressed, Time.frameCount, locTextOverride));

            if (actionHandlers.Count == 1 || !string.IsNullOrEmpty(locTextOverride))
            {
                InputPromptDisplayInfo prompts = GetInputPromptDisplayInfo(action);
                prompts.UpdateListenerInfo(true, locTextOverride);
            }
        }

        public static void RemoveHandler(ModioAction action, Action onPressed)
        {
            if (!Handlers.TryGetValue(action, out var actionHandlers)) return;

            bool removed = false;

            for (var i = actionHandlers.Count - 1; i >= 0; i--)
            {
                if (actionHandlers[i].action == onPressed)
                {
                    actionHandlers.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed && actionHandlers.Count == 0)
            {
                InputPromptDisplayInfo prompts = GetInputPromptDisplayInfo(action);
                prompts.UpdateListenerInfo(false, null);
            }
        }

        public static void ControlSchemeChanged(bool isController)
        {
            IsUsingGamepad = isController;
            SwappedControlScheme?.Invoke(isController);
        }

        public static void SetButtonPrompts(ModioAction action, List<string> textPrompts, List<Sprite> icons)
        {
            InputPromptDisplayInfo prompts = GetInputPromptDisplayInfo(action);

            bool hasListeners = Handlers.TryGetValue(action, out var actionHandlers) && actionHandlers.Count > 0;
            
            string locTextOverride = !hasListeners ? null : actionHandlers.Select(act => act.locTextOverride).FirstOrDefault(s => s != null);

            prompts.UpdateInfo(textPrompts, icons, hasListeners, locTextOverride);
        }

        public static InputPromptDisplayInfo GetInputPromptDisplayInfo(ModioAction action)
        {
            if (!Prompts.TryGetValue(action, out var prompts))
            {
                prompts = new InputPromptDisplayInfo();
                Prompts[action] = prompts;
            }

            return prompts;
        }

        public static Vector2 GetRawCursor()
        {
            return RawCursorProvider?.Invoke() ?? Vector2.zero;
        }
    }
}
