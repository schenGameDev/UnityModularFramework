using AYellowpaper.SerializedCollections;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework {
    using System;
    using Commons;
    using Utility;
    /// <summary>
    /// The bucket keeps a &lt;string,T> dictionary, where T can be fetched by the key
    /// </summary>
    public abstract class CustomTypeBucket<T> : ScriptableObject {
        [SerializeField,SerializedDictionary("Key","Value"),HideLabel]
        protected SerializedDictionary<string,T> dictionary = new();

        public Optional<T> Get (string name) {
            if(dictionary.TryGetValue(name, out T value)) {
                return value;
            }
            DebugUtil.DebugError(name + " not found", this.name);
            return Optional<T>.None();
        }

        public bool ContainsKey(string name) {
            return dictionary.ContainsKey(name);
        }

        public void ForEach(Action<string, T> action) {
            dictionary.ForEach(e=>action(e.Key, e.Value));
        }
    }
}
