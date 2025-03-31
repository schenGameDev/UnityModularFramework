using System;
using EditorAttributes;
using TMPro;
using UnityEngine;

namespace ModularFramework.Utility
{
    public class TranslationText : MonoBehaviour
    {
        [SerializeField,ReadOnly] protected string id;
        [SerializeField] protected string text;
        
#if UNITY_EDITOR
        [SerializeField] private bool saved;
        [SerializeField] private TranslationDraftBucket draftBucket;
        [Button]
        private void Save()
        {
            if(!saved)
            {
                RegenerateId();
                if (draftBucket.ContainsKey(id))
                {
                    Debug.LogWarning($"Duplicate id: {id}");
                    return;
                }
            }
            
            draftBucket.Put(id, text);
            saved = true;
            Debug.Log("Save success");
        }
        
        private void RegenerateId()
        {
            id = (DateTime.Now.Millisecond - new DateTime(2025, 1, 1).Millisecond).ToString("x");
        }
#endif

        private void Awake()
        {
            text = TranslationUtil.Translate(id);
        }

        protected virtual void Start()
        {
            var tmp = GetComponent<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = text;
            }
        }
    }
}