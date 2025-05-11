using System;
using EditorAttributes;
using TMPro;
using UnityEngine;

namespace ModularFramework.Utility
{
    public class TranslationText : MonoBehaviour
    {
        [SerializeField,ReadOnly] protected string id;
        private string _text;
        private TextMeshProUGUI _tmp;
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
#endif

        private void Awake()
        {
            _tmp = GetComponent<TextMeshProUGUI>();
            _text = TranslationUtil.Translate(id, _tmp? _tmp.text : null);
        }

        protected virtual void Start()
        {
            if (_tmp) _tmp.text = _text;
        }
    }
}