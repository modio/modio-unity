using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace ModIOBrowser.Implementation
{
    public class KeyInput5Digits : MonoBehaviour
    {
        public bool copyPasteMode;
        public bool debug = false;
        public string currentInputString;
        public int index = 0;

        private int maxDigits;
        private Action<string> onFinish;
        private Action<string> renderOutput;
        private List<KeyCode> keyCodes = new List<KeyCode>();
        private Dictionary<KeyCode, string> keyCodeOverrides = new Dictionary<KeyCode, string>();
        
        public void Setup()
        {
            keyCodes.AddRange(GetRelevantKeys(KeyCode.Alpha0, KeyCode.Alpha9));
            keyCodes.AddRange(GetRelevantKeys(KeyCode.A, KeyCode.Z));
            SetupKeyCodeStringOverrides();
        }

        private IEnumerable<KeyCode> GetRelevantKeys(KeyCode begin, KeyCode end)
        {
            for(KeyCode k = begin; k < end; k++)
                yield return k;
        }

        private void SetupKeyCodeStringOverrides()
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

        private void AddToInput(KeyCode keyCode)
        {
            var s = keyCodeOverrides.ContainsKey(keyCode)
                ? keyCodeOverrides[keyCode]
                : keyCode.ToString();

            SetToInput(s);
        }

        private void SetToInput(string s)
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

            for(int i = 0; i < keyCodes.Count; i++)
            {
                var inputDown = Input.GetKeyUp(keyCodes[i]);
                if(inputDown)
                {
                    AddToInput(keyCodes[i]);
                }
            }
        }

        private bool Enter()
        {
            if(Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
            {
                onFinish(currentInputString);
                return true;
            }
            return false;
        }

        private bool Backspace()
        {
            if(Input.GetKeyUp(KeyCode.Backspace))
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
            if(Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                copyPasteMode = true;
            }
            //Did the user release control this frame?
            else if(Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
            {
                copyPasteMode = false;
                return false;
            }

            //What if the user was holding ctrl while tabbing in?
            if(!copyPasteMode
                && (Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)))
                copyPasteMode = true;

            if(copyPasteMode && Input.GetKeyUp(KeyCode.V))
            {
                SetToInput(GUIUtility.systemCopyBuffer);
                return true;
            }

            return false;
        }

        public string GetValues() => currentInputString;

        public void SetIndex(int i) => index = Math.Min(Math.Max(i, 0), maxDigits - 1);            
    }
}
