using System.Collections.Generic;
using UnityEngine;

namespace Modio.Unity.UI.Input
{
    public class ModioUIPromptIconResolver : MonoBehaviour
    {
        [SerializeField] PlatformSprites[] _platforms;
        [SerializeField] KeyboardMapping[] _keyboardMappings;

        public (Sprite icon, string displayAsText) TryGetKeyboardIcon(string controlPath)
        {
            foreach (var keyboardMapping in _keyboardMappings)
            {
                if (keyboardMapping.controlPath == controlPath)
                    return (keyboardMapping.icon, keyboardMapping.displayAsText);
            }

            return default;
        }

        public Sprite ResolveIcon(string controlPath, RuntimePlatform forControllerType)
        {
            foreach (PlatformSprites platform in _platforms)
            {
                if (platform.forControllerTypes.Contains(forControllerType)) return platform.GetSprite(controlPath);
            }

            return null;
        }

        [System.Serializable]
        class KeyboardMapping
        {
            public string controlPath;
            public Sprite icon;
            public string displayAsText;
        }

        [System.Serializable]
        class PlatformSprites
        {
            [SerializeField] public List<RuntimePlatform> forControllerTypes;

            public Sprite buttonSouth;
            public Sprite buttonNorth;
            public Sprite buttonEast;
            public Sprite buttonWest;
            public Sprite startButton;
            public Sprite selectButton;
            public Sprite leftTrigger;
            public Sprite rightTrigger;
            public Sprite leftShoulder;
            public Sprite rightShoulder;
            public Sprite dpad;
            public Sprite dpadUp;
            public Sprite dpadDown;
            public Sprite dpadLeft;
            public Sprite dpadRight;
            public Sprite leftStick;
            public Sprite rightStick;
            public Sprite leftStickPress;
            public Sprite rightStickPress;

            public Sprite GetSprite(string controlPath)
            {
                // From the input system, we get the path of the control on device. So we can just
                // map from that to the sprites we have for gamepads.
                switch (controlPath)
                {
                    case "buttonSouth":     return buttonSouth;
                    case "buttonNorth":     return buttonNorth;
                    case "buttonEast":      return buttonEast;
                    case "buttonWest":      return buttonWest;
                    case "start":           return startButton;
                    case "select":          return selectButton;
                    case "leftTrigger":     return leftTrigger;
                    case "rightTrigger":    return rightTrigger;
                    case "leftShoulder":    return leftShoulder;
                    case "rightShoulder":   return rightShoulder;
                    case "dpad":            return dpad;
                    case "dpad/up":         return dpadUp;
                    case "dpad/down":       return dpadDown;
                    case "dpad/left":       return dpadLeft;
                    case "dpad/right":      return dpadRight;
                    case "leftStick":       return leftStick;
                    case "rightStick":      return rightStick;
                    case "leftStickPress":  return leftStickPress;
                    case "rightStickPress": return rightStickPress;
                }

                return null;
            }
        }
    }
}
