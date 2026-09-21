using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Modio.API;
using UnityEngine;

namespace Modio.Unity.UI.Components.Localization
{
    public class ModioUILocalizationManager : MonoBehaviour
    {
        public delegate string LocalizationHandler(string key, string isoLanguageCode);

        static LocalizationHandler customLocalizationHandler;

        static string _languageCode;
        static List<Dictionary<string, string>> _languageTables;
        static Dictionary<string, string> _currentTable;

        [SerializeField] TextAsset _locTable;
        [SerializeField] bool _setCurrentSystemCulture = true;

        public static bool LocalizationExists => customLocalizationHandler != null || _languageTables != null && _languageTables.Count > 0;
        public static bool LocalizationReady => customLocalizationHandler != null || _currentTable != null;

        /// <summary>
        /// Call the passed method when the language is set. Calls immediately if already set
        /// </summary>
        public static event Action LanguageSet
        {
            add
            {
                LanguageSetInternal += value;
                if (_currentTable != null || customLocalizationHandler != null) value.Invoke();
            }
            remove => LanguageSetInternal -= value;
        }

        static event Action LanguageSetInternal;
        public static CultureInfo CultureInfo { get; private set; } = new CultureInfo("en");

        /// <summary>
        /// Set this to bypass mod.io's basic CSV parser and use your own loc provider
        /// </summary>
        public static void SetCustomHandler(LocalizationHandler handler)
        {
            customLocalizationHandler = handler;

            if (customLocalizationHandler != null) LanguageSetInternal?.Invoke();
        }

        public void SetLanguageCode(string isoCode)
        {
            if (string.IsNullOrEmpty(isoCode)) isoCode = "en";

            _languageCode = isoCode;

            try 
            { 
                CultureInfo = new CultureInfo(isoCode);
            }
            catch (CultureNotFoundException)
            {
                ModioLog.Warning?.Log($"Language code {isoCode} not found by CultureInfo. Using default culture.");
                CultureInfo = new CultureInfo("en");
            }

            if (_setCurrentSystemCulture) CultureInfo.CurrentCulture = CultureInfo;

            if (_languageTables != null)
            {
                foreach (var languageTable in _languageTables)
                {
                    
                    if (languageTable.TryGetValue(ModioUILocalizationKeys.LanguageCode, out var code) &&
                        isoCode == code)
                    {
                        _currentTable = languageTable;
                        LanguageSetInternal?.Invoke();

                        break;
                    }
                }
            }
        }

        public static string GetLocalizedText(string key, bool errorIfMissing = true)
        {
            if (customLocalizationHandler != null) return customLocalizationHandler(key, _languageCode);

            //We've been called too early; this should be called again in response to the language being set
            if (_currentTable == null) return errorIfMissing ? key : null;

            if (_currentTable.TryGetValue(key, out var entry)) return entry;

            if (!errorIfMissing)
                return null;

            Debug.LogError($"Missing localized key {key} for language {_languageCode}");

            return $"MISSING KEY {key}";
        }

        void Awake()
        {
            var allEntries = _locTable.text.Split('\n');

            _languageTables = null;

            foreach (var entry in allEntries)
            {
                var trimmedEntry = entry;
                //prevent the last column getting a \r tacked on
                if (entry.EndsWith("\r")) trimmedEntry = entry.Substring(0, entry.Length - 1);

                // Column 0 is the key, columns 1..n are the per-language values
                var fields = ParseCsvLine(trimmedEntry);

                if (fields.Count < 2) continue;

                var entryKey = fields[0];

                if (_languageTables == null)
                {
                    _languageTables = new List<Dictionary<string, string>>();

                    for (int i = 1; i < fields.Count; i++)
                    {
                        _languageTables.Add(
                            new Dictionary<string, string>
                            {
                                { entryKey, fields[i] },
                            }
                        );
                    }

                    continue;
                }

                for (int i = 1; i < fields.Count && i - 1 < _languageTables.Count; i++)
                {
                    _languageTables[i - 1].Add(entryKey, fields[i]);
                }

                // Error if inconsistent number of columns in the CSV file
                if (fields.Count - 1 != _languageTables.Count)
                {
                    ModioLog.Error?.Log($"Inconsistent number of columns in localization CSV file for entry {entryKey}. Expected {_languageTables.Count + 1}, got {fields.Count}.");
                }
            }

            //Ensure a language is set
            if (ModioServices.TryResolve(out ModioSettings settings)) SetLanguageCode(settings.DefaultLanguage);

            ModioClient.OnInitialized += OnPluginInitialized;
        }

        // Parses a single CSV row into its fields. Handles quoted fields containing
        // commas, and doubled quotes ("") as an escaped quote inside a quoted field.
        static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c != '"')
                        current.Append(c);
                    else if (i + 1 < line.Length && line[i + 1] == '"') // escaped quote ("")
                    {
                        current.Append('"');
                        i++; // skip the next quote
                    }
                    else
                        inQuotes = false;
                }
                else if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }

            fields.Add(current.ToString());
            return fields;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= OnPluginInitialized;
        }

        void OnPluginInitialized()
        {
            SetLanguageCode(ModioAPI.LanguageCodeResponse);
        }
    }
}
