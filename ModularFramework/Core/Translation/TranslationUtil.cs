using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework.Utility
{
    public static class TranslationUtil
    {
        private static readonly Language DEFAULT_LANGUAGE = Language.ZH;
        private static readonly Dictionary<string,string> TRANSLATION_DICT = new();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            string savedLanguage = PlayerPrefs.GetString("Language");
            if (string.IsNullOrEmpty(savedLanguage))
            {
                Load(DEFAULT_LANGUAGE);
                return;
            }
            try
            {
                Language lang = EnumExtension.GetEnumValue<Language>(savedLanguage);
                Load(lang);
            }
            catch (Exception )
            {
                Load(DEFAULT_LANGUAGE);
                Debug.LogError("unknown language " + PlayerPrefs.GetString("Language"));
            }
            
            
        }

        public static void Load(Language language)
        {
            // spreadsheet name: [language].csv
            // spreadsheet has 2 column: id,text
            string path = "Localization/" + language;
            var translationDictionary = Resources.Load<TextAsset>(path);
            if (!translationDictionary)
            {
                DebugUtil.Warn($"file not found in {path}" );
                return;
            }
            
            TRANSLATION_DICT.Clear();
            string[] fields = translationDictionary.text.Split(new [] {",","\n"}, StringSplitOptions.None);
            for (int i = 2; i < fields.Length; i += 2) // skip header
            {
                string text = fields[i + 1];
                if (text[0] == '"') // enclose field
                {
                    text = text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
                }
                TRANSLATION_DICT.Add(fields[i], text);
            }
        }

        public static void SaveLanguagePref(Language language)
        {
            PlayerPrefs.SetString("Language", language.ToString());
        }

        public static string Translate(string key, string defaultValue = null)
        {
            return TRANSLATION_DICT.GetValueOrDefault(key, defaultValue ?? key);
        }
    }
    
    
    public enum Language // top 7 language on steam
    {
        ZH, // chinese
        EN, // english
        RU, // russian
        ES, // spanish
        PT, // portuguese
        DE, // german
        JA // japanese 
    } 
}