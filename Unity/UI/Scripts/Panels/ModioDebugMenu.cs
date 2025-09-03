using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Navigation;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModioDebugMenu : MonoBehaviour
    {
        [SerializeField]
        ModioUIButton _buttonPrefab;
        [SerializeField]
        ModioUIToggle _togglePrefab;
        [SerializeField]
        ModioInputFieldSelectionWrapper _textPrefab;
        [SerializeField]
        TMP_Text _labelPrefab;
        
        Action _onSetToDefaults;

        public void Awake()
        {
            _buttonPrefab.gameObject.SetActive(false);
            _togglePrefab.gameObject.SetActive(false);
            _textPrefab.gameObject.SetActive(false);
            if (_labelPrefab != null) _labelPrefab.gameObject.SetActive(false);
        }

        public void SetToDefaults() => _onSetToDefaults?.Invoke();
        
        public void AddButton(string text, Action onClick)
        {
            ModioUIButton button = Instantiate(_buttonPrefab, _buttonPrefab.transform.parent, false);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = text;
            button.onClick.AddListener(() => onClick());
        }

        public void AddToggle(string text, Func<bool> initialValueGetter, Action<bool> onToggle)
        {
            ModioUIToggle toggle = Instantiate(_togglePrefab, _buttonPrefab.transform.parent, false);
            toggle.gameObject.SetActive(true);
            toggle.GetComponentInChildren<TMP_Text>().text = text;
            _onSetToDefaults += () => toggle.isOn = initialValueGetter();
            toggle.onValueChanged.AddListener(b => onToggle(b));
        }

        public void AddLabel(string text)
        {
            if(_labelPrefab == null) return;
            TMP_Text label = Instantiate(_labelPrefab, _labelPrefab.transform.parent, false);
            label.gameObject.SetActive(true);
            label.text = text;
        }

        public void AddTextField(string text, Func<string> initialValueGetter, Action<string> onSubmitted)
        {
            ModioInputFieldSelectionWrapper wrapper = Instantiate(_textPrefab, _buttonPrefab.transform.parent, false);
            wrapper.gameObject.SetActive(true);
            wrapper.GetComponentInChildren<TMP_Text>().text = text;

            var inputField = wrapper.GetComponentInChildren<TMP_InputField>();
            
            _onSetToDefaults += () => inputField.text = initialValueGetter();
            inputField.onDeselect.AddListener(OnTextFieldSubmit);
            inputField.onSubmit.AddListener(OnTextFieldSubmit);

            void OnTextFieldSubmit(string s)
            {
                try
                {
                    onSubmitted(s);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    inputField.text = initialValueGetter();
                }
            }
        }

        public void AddTextField(string text, Func<int> initialValueGetter, Action<int> onSubmitted)
        {
            AddTextField(text,
                () => initialValueGetter().ToString(),
                s => onSubmitted(int.Parse(s)));
        }
        public void AddTextField(string text, Func<long> initialValueGetter, Action<long> onSubmitted)
        {
            AddTextField(text,
                () => initialValueGetter().ToString(),
                s => onSubmitted(int.Parse(s)));
        }
        
        public void AddAllMethodsOrPropertiesWithAttribute<T>(Func<T, bool> predicate = null) where T : Attribute
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in a.GetTypes())
                {
                    MethodInfo[] allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    PropertyInfo[] allProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    bool hasDoneTypeLabel = false;
                    
                    foreach (MethodInfo methodInfo in allMethods)
                    {
                        var attribute = methodInfo.GetCustomAttribute<T>();
                        if (attribute == null || (predicate != null && !predicate(attribute))) continue;

                        int paramCount = methodInfo.GetParameters().Length;

                        if (paramCount > 0)
                        {
                            Debug.LogError($"Can't handle method {methodInfo.Name} on type {type.Name} because it has more than one parameter");
                            continue;
                        }

                        if (!hasDoneTypeLabel)
                        {
                            AddLabel(type.Name);
                            hasDoneTypeLabel = true;
                        }

                        AddButton(Nicify($"{methodInfo.Name}"), () => methodInfo.Invoke(null, null));
                    }

                    foreach (var propertyInfo in allProperties)
                    {
                        var attribute = propertyInfo.GetCustomAttribute<T>();
                        if (attribute == null || (predicate != null && !predicate(attribute))) continue;

                        if (!hasDoneTypeLabel)
                        {
                            AddLabel(type.Name);
                            hasDoneTypeLabel = true;
                        }
                        
                        string propertyName = Nicify($"{propertyInfo.Name}");
                        if (propertyInfo.PropertyType == typeof(bool))
                            AddToggle(propertyName, 
                                                    () => (bool)propertyInfo.GetValue(null),
                                                    b => propertyInfo.SetValue(null, b));
                        
                        else if (propertyInfo.PropertyType == typeof(string))
                            HookUpField(o => (string)o, s => s);
                        else if (propertyInfo.PropertyType == typeof(int))
                            HookUpField(o => o.ToString(), s => int.Parse(s));
                        else if (propertyInfo.PropertyType == typeof(long))
                            HookUpField(o => o.ToString(), s => long.Parse(s));
                        else
                            Debug.LogWarning($"{nameof(ModioDebugMenu)} hit property of unhandled type {propertyInfo.PropertyType}");
                        
                        void HookUpField(Func<object, string> func1, Func<string, object> func2)
                        {
                            AddTextField(propertyName, 
                                                      () => func1(propertyInfo.GetValue(null)),
                                                      s => propertyInfo.SetValue(null, func2(s))
                            );
                        }
                    }
                }

            }
        }
        
        public static string Nicify(string name) => Regex.Replace(name, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", " $1");
    }
}
