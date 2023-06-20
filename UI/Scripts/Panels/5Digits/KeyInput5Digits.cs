using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ModIOBrowser.Implementation
{
    public class KeyInput5Digits : MonoBehaviour
    {
        public bool copyPasteMode;
        public bool debug = false;
        public string currentInputString;
        public int index = 0;

        int maxDigits;
        Action<string> onFinish;
        Action<string> renderOutput;
        List<KeyCode> keyCodes = new List<KeyCode>();
        Dictionary<KeyCode, string> keyCodeOverrides = new Dictionary<KeyCode, string>();
        
        public void Setup()
        {
            keyCodes.AddRange(GetRelevantKeys(KeyCode.Alpha0, KeyCode.Alpha9));
            keyCodes.AddRange(GetRelevantKeys(KeyCode.Keypad0, KeyCode.Keypad9));
            keyCodes.AddRange(GetRelevantKeys(KeyCode.A, KeyCode.Z));
            SetupKeyCodeStringOverrides();
        }

        IEnumerable<KeyCode> GetRelevantKeys(KeyCode begin, KeyCode end)
        {
            for(KeyCode k = begin; k <= end; k++)
                yield return k;
        }

        void SetupKeyCodeStringOverrides()
        {
            keyCodeOverrides.Add(KeyCode.Alpha0, "0");
            keyCodeOverrides.Add(KeyCode.Alpha1, "1");
            keyCodeOverrides.Add(KeyCode.Alpha2, "2");
            keyCodeOverrides.Add(KeyCode.Alpha3, "3");
            keyCodeOverrides.Add(KeyCode.Alpha4, "4");
            keyCodeOverrides.Add(KeyCode.Alpha5, "5");
            keyCodeOverrides.Add(KeyCode.Alpha6, "6");
            keyCodeOverrides.Add(KeyCode.Alpha7, "7");
            keyCodeOverrides.Add(KeyCode.Alpha8, "8");
            keyCodeOverrides.Add(KeyCode.Alpha9, "9");
            keyCodeOverrides.Add(KeyCode.Keypad0, "0");
            keyCodeOverrides.Add(KeyCode.Keypad1, "1");
            keyCodeOverrides.Add(KeyCode.Keypad2, "2");
            keyCodeOverrides.Add(KeyCode.Keypad3, "3");
            keyCodeOverrides.Add(KeyCode.Keypad4, "4");
            keyCodeOverrides.Add(KeyCode.Keypad5, "5");
            keyCodeOverrides.Add(KeyCode.Keypad6, "6");
            keyCodeOverrides.Add(KeyCode.Keypad7, "7");
            keyCodeOverrides.Add(KeyCode.Keypad8, "8");
            keyCodeOverrides.Add(KeyCode.Keypad9, "9");
        }

        public void NewSession(int maxDigits, Action<string> renderOutput, Action<string> onFinish)
        {
            this.maxDigits = maxDigits;
            this.onFinish = onFinish;
            this.renderOutput = renderOutput;
            SetIndex(0);
            currentInputString = new string(' ', maxDigits);
            this.renderOutput(currentInputString);
            gameObject.SetActive(true);
        }

        public void EndSession() => gameObject.SetActive(false);

        void AddToInput(KeyCode keyCode)
        {
            var s = keyCodeOverrides.ContainsKey(keyCode)
                ? keyCodeOverrides[keyCode]
                : keyCode.ToString();

            SetToInput(s);
        }

        void SetToInput(string s)
        {
            var stringBuilder = new StringBuilder(currentInputString);
            if(s.Length == 1)
            {
                stringBuilder[index] = s.ToUpper().ToCharArray().First();
                SetIndex(index + 1);
            }
            else
            {
                foreach(var item in s.ToUpper())
                {
                    stringBuilder[index] = item;
                    index++;
                    if(index >= maxDigits)
                    {
                        break;
                    }
                }
            }
            
            SetIndex(index);

            currentInputString = stringBuilder.ToString();
            if(currentInputString.Length > maxDigits)
            {
                currentInputString = currentInputString.Substring(0, maxDigits);
            }

            renderOutput(currentInputString);
        }

        void Update()
        {
            if(CopyPaste())
            {
                return;
            }

            if(Backspace())
            {
                return;
            }

            if(Enter())
            {
                return;
            }

            foreach(var key in keyCodes)
            {
                var inputDown = GetKeyUp(key);
                if(inputDown)
                {
                    AddToInput(key);
                }
            }
        }

        bool Enter()
        {
            if(GetKeyUp(KeyCode.Return) || GetKeyUp(KeyCode.KeypadEnter))
            {
                onFinish(currentInputString);
                return true;
            }
            return false;
        }

        bool Backspace()
        {
            if(GetKeyUp(KeyCode.Backspace))
            {
                StringBuilder stringBuilder = new StringBuilder(currentInputString);

                if(index < maxDigits - 1 || stringBuilder[index] == ' ')
                {                    
                    SetIndex(Math.Max(0, index - 1));
                }

                stringBuilder[index] = ' ';
                currentInputString = stringBuilder.ToString();
                renderOutput(currentInputString);

                return true;
            }
            return false;
        }

        public bool CopyPaste()
        {
            //Did the user press control this frame?
            if(GetKeyDown(KeyCode.LeftControl) || GetKeyDown(KeyCode.RightControl))
            {
                copyPasteMode = true;
            }
            //Did the user release control this frame?
            else if(GetKeyUp(KeyCode.LeftControl) || GetKeyUp(KeyCode.RightControl))
            {
                copyPasteMode = false;
                return false;
            }

            //What if the user was holding ctrl while tabbing in?
            if(!copyPasteMode
                && (GetKey(KeyCode.LeftControl) || GetKeyDown(KeyCode.RightControl)))
                copyPasteMode = true;

            if(copyPasteMode && GetKeyUp(KeyCode.V))
            {
                SetToInput(GUIUtility.systemCopyBuffer);
                return true;
            }

            return false;
        }

        public string GetValues() => currentInputString;

        public void SetIndex(int i) => index = Math.Min(Math.Max(i, 0), maxDigits - 1);     
        
#if ENABLE_INPUT_SYSTEM
        static Key KeyFromKeyCode(KeyCode keyCode)
        {
            for (KeyCode alphabetCode = KeyCode.A; alphabetCode <= KeyCode.Z; ++alphabetCode)
            {
                if (keyCode != alphabetCode)
                {
                    continue;
                }
 
                int offset = alphabetCode - KeyCode.A;
                return Key.A + offset;
            }

            switch(keyCode)
            {
                case KeyCode.Alpha0:
                case KeyCode.Keypad0:
                    return Key.Digit0;
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    return Key.Digit1;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    return Key.Digit2;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    return Key.Digit3;
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    return Key.Digit4;
                case KeyCode.Alpha5:
                case KeyCode.Keypad5:
                    return Key.Digit5;
                case KeyCode.Alpha6:
                case KeyCode.Keypad6:
                    return Key.Digit6;
                case KeyCode.Alpha7:
                case KeyCode.Keypad7:
                    return Key.Digit7;
                case KeyCode.Alpha8:
                case KeyCode.Keypad8:
                    return Key.Digit8;
                case KeyCode.Alpha9:
                case KeyCode.Keypad9:
                    return Key.Digit9;
                case KeyCode.LeftControl:
                    return Key.LeftCtrl;
                case KeyCode.RightControl:
                    return Key.RightCtrl;
                case KeyCode.KeypadEnter:
                case KeyCode.Return:
                    return Key.Enter;
                case KeyCode.Backspace:
                    return Key.Backspace;
                default:
                    // No suitable replacement found, return a slash so we ignore it
                    return Key.Slash;
            }
        }
#endif
 
        public static bool GetKeyDown(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            Key key = KeyFromKeyCode(keyCode);
            if(Keyboard.current != null && key != Key.Slash)
            {
                return Keyboard.current[key].wasPressedThisFrame;
            }
            return false;
#else
            return Input.GetKeyDown(keyCode);
#endif
        }
 
        public static bool GetKeyUp(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            Key key = KeyFromKeyCode(keyCode);
            if(Keyboard.current != null && key != Key.Slash)
            {
                return Keyboard.current[key].wasReleasedThisFrame;
            }
            return false;
#else
            return Input.GetKeyUp(keyCode);
#endif
        }
 
        public static bool GetKey(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            Key key = KeyFromKeyCode(keyCode);
            if (Keyboard.current != null && key != Key.Slash)
            {
                return Keyboard.current[key].isPressed;
            }
            return false;
#else
            return Input.GetKey(keyCode);
#endif
        }
 
        public static float GetAxis(string axis)
        {
#if ENABLE_INPUT_SYSTEM
            if (axis == "Mouse ScrollWheel" && Mouse.current != null)
            {
                return Mouse.current.scroll.ReadValue().y;
            }
            return 0f;
#else
            return Input.GetAxis(axis);
#endif
        }
    }
}
