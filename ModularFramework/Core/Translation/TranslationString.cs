using System;
using EditorAttributes;
using UnityEngine;
using Random = System.Random;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ModularFramework.Utility
{
    [Serializable]
    public class TranslationString
    {
        [ReadOnly] public uint id;
        [HideLabel,OnValueChanged(nameof(Save))]
        public string text;
        
        public static implicit operator string(TranslationString translationString) 
            => TranslationUtil.Translate(translationString.id, translationString.text);
        
#if UNITY_EDITOR
        [SerializeField,HideProperty] private TranslationBank bank;
        
        private void Save()
        {
            if (string.IsNullOrEmpty(text))
            {
                Clean();
                return;
            }
            
            if (bank == null)
            {
                bank = TranslationUtil.GetBank(EditorSceneManager.GetActiveScene().name);
            }
            if(id == 0)
            {
                id =RegenerateId();
                if (bank.ContainsKey(id))
                {
                    Debug.LogWarning($"Duplicate id: {id}");
                    return;
                }
            }
            
            bank.Put(id, text);
        }

        public void Clean()
        {
            if (bank == null)
            {
                bank = TranslationUtil.GetBank(EditorSceneManager.GetActiveScene().name);
            }
            bank.Delete(id);
            id = 0;
            text = string.Empty;
            Debug.Log("Clean success");
        }

        private uint RegenerateId()
        {
            var random = new Random();
            uint thirtyBits = (uint) random.Next(1 << 30);
            uint twoBits = (uint) random.Next(1 << 2);
            return (thirtyBits << 2) | twoBits;
        }
#endif
    }
}