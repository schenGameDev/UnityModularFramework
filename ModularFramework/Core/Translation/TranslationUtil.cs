using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModularFramework.Utility
{
    public static class TranslationUtil
    {
        private static readonly Language DEFAULT_LANGUAGE = Language.ZH;
        private static readonly Dictionary<uint,string> TRANSLATION_DICT = new();
        private const string DRAFT_FOLDER_PATH = "Assets/UnityModularFramework/ModularFramework/Core/Translation/Draft";
        private static Language _currentLanguage;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            string savedLanguage = PlayerPrefs.GetString("Language");
            if (string.IsNullOrEmpty(savedLanguage))
            {
                _currentLanguage = DEFAULT_LANGUAGE;
                return;
            }
            try
            {
                _currentLanguage = EnumExtension.GetEnumValue<Language>(savedLanguage);
            }
            catch (Exception )
            {
                _currentLanguage = DEFAULT_LANGUAGE;
                Debug.LogError("unknown language " + savedLanguage);
            }
        }

        public static void Load(string sceneName)
        {
            // spreadsheet name: [language]_[scene].csv
            // spreadsheet has 2 column: id,text
            string path = "Localization/" + _currentLanguage + "_" + sceneName;
            var translationDictionary = Resources.Load<TextAsset>(path);
            if (!translationDictionary)
            {
                Debug.LogWarning($"file not found in {path}" );
                return;
            }
            
            TRANSLATION_DICT.Clear();
            string[] fields = translationDictionary.text.Split(new [] {",","\n"}, StringSplitOptions.None);
            for (int i = 2; i < fields.Length; i += 2) // skip header
            {
                uint key = uint.Parse(fields[i]);
                string text = fields[i + 1];
                if (text[0] == '"') // enclose field
                {
                    text = text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
                }
                TRANSLATION_DICT.Add(key, text);
            }
        }

        public static void SaveLanguagePref(Language language)
        {
            PlayerPrefs.SetString("Language", language.ToString());
        }

        public static string Translate(uint key, string defaultValue = null)
        {
            return TRANSLATION_DICT.GetValueOrDefault(key, 
                string.IsNullOrEmpty(defaultValue)? key.ToString() : defaultValue);
        }
        
#if UNITY_EDITOR        
        public static TranslationBank GetBank(string sceneName)
        {
            string path = $"{DRAFT_FOLDER_PATH}/Draft_{sceneName}.asset";
            var bank = AssetDatabase.LoadAssetAtPath<TranslationBank>(path);
            if (!bank)
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(DRAFT_FOLDER_PATH))
                {
                    Directory.CreateDirectory(DRAFT_FOLDER_PATH);
                    AssetDatabase.Refresh();
                }
                
                bank = ScriptableObject.CreateInstance<TranslationBank>();
                
                AssetDatabase.CreateAsset(bank, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"Create {path}");
            }
            return bank;
        }
#endif
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