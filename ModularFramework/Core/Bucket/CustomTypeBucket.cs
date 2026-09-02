using System;
using System.Collections.Generic;
using EditorAttributes;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework {
    /// <summary>
    /// The bucket keeps a &lt;string,T> dictionary, where T can be fetched by the key
    /// </summary>
    public abstract class CustomTypeBucket<T> : ScriptableObject {
#pragma warning disable UAC1016
        [SerializeField,DictionaryDisplay(keyLabel = "Key", valueLabel = "Value"),HideLabel]
        protected Dictionary<string,T> dictionary = new();
#pragma warning restore UAC1016

        public Optional<T> Get (string key) {
            if(dictionary.TryGetValue(key, out T value)) {
                return value;
            }
            DebugUtil.DebugError(key + " not found", this.name);
            return Optional<T>.None();
        }

        public bool ContainsKey(string key) {
            return dictionary.ContainsKey(key);
        }

        public void ForEach(Action<string, T> action) {
            dictionary.ForEach(e=>action(e.Key, e.Value));
        }
    }
}
