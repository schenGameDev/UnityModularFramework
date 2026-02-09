using System.IO;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Utility
{
    [CreateAssetMenu(fileName = "TranslationDraft_SO", menuName = "Game Module/Translation/Draft")]
    public class TranslationDraftBucket : Bucket
    {
        public void Put(string id, string text)
        {
            if (string.IsNullOrEmpty(id))
            {
                DebugUtil.DebugError("id is null or empty", this.name);
            }
            dictionary[id] = text;
        }
        
        public void Delete(string id) => dictionary.Remove(id);
#if UNITY_EDITOR        
        [Button]
        private void GenerateSpreadSheet()
        {
            if (dictionary.IsEmpty()) return;
            string path = Application.dataPath + "/translationDraft.csv";
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
    }
#endif
}