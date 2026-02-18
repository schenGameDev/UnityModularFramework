using System.IO;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Utility
{
    // [CreateAssetMenu(fileName = "TranslationBank_SO", menuName = "Game Module/Translation/Bank")]
    public class TranslationBank : ScriptableObject
    {
        [SerializeField,SerializedDictionary("Key","Value"),HideLabel]
        protected SerializedDictionary<uint,string> dictionary = new();
        public void Put(uint id, string text)
        {
            if (id == 0)
            {
                DebugUtil.DebugError("id is uninitialized (0)", this.name);
            }
            dictionary[id] = text;
        }
        
        public void Delete(uint id) => dictionary.Remove(id);
        
        public bool ContainsKey(uint id) => dictionary.ContainsKey(id);
        
#if UNITY_EDITOR
        [SerializeField,HideLabel,InlineButton(nameof(Import))] private TranslationBank other;

        private void Import()
        {
            other.dictionary.ForEach((id,text) =>
            {
                if (dictionary.ContainsKey(id))
                {
                    Debug.LogWarning($"Duplicate id {id} in {name}, skipped");
                    return;
                }
                dictionary[id] = text;
            });
            Debug.Log("Imported " + other.name);
            other = null;
        }
        
        [Button]
        private void GenerateSpreadSheet()
        {
            if (dictionary.IsEmpty()) return;
            string path = Application.dataPath + $"/{name}.csv";
            TextWriter tw = new StreamWriter(path, false);
            tw.WriteLine("id,text");
            dictionary.ForEach((id,text) =>
            {
                if (text.Contains(',') || text.Contains('"')) // need enclosure
                {
                    text = text.Replace("\"", "\"\"");
                    text = "\"" +text + "\"";
                }
                tw.WriteLine(id + "," + text);
            });
            tw.Close();
            Debug.Log("Translation draft bucket created at " + path);
        }
#endif
    }
}