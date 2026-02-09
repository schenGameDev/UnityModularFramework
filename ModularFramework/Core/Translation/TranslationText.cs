using System;
using EditorAttributes;
using KBCore.Refs;
using TMPro;
using UnityEngine;

namespace ModularFramework.Utility
{
    public class TranslationText : MonoBehaviour,IUniqueIdentifiable
    {
        [SerializeField,ReadOnly] protected string id;
        public string UniqueId => id;
        private string _text;
        [SerializeField,Self] private TextMeshProUGUI tmp;
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

            var text = GetDraftText();
            draftBucket.Put(id, text);
            saved = true;
            Debug.Log("Save success");
        }

        protected virtual string GetDraftText() => GetComponent<TextMeshProUGUI>().text;
        
        private void RegenerateId()
        {
            id = (DateTime.Now - new DateTime(2025, 1, 1)).TotalMilliseconds.ToString("0");
        }

        private void OnValidate() => this.ValidateRefs();
#endif

        private void Awake()
        {
            _text = TranslationUtil.Translate(id, tmp? tmp.text : null);
        }

        protected virtual void Start()
        {
            if (tmp) tmp.text = _text;
        }
    }
}