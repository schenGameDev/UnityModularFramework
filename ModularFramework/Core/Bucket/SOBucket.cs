using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework {
    /// <summary>
    /// The bucket keeps a list of ScriptableObjects, which can be fetched by the name
    /// </summary>
    public abstract class SOBucket<T> : ScriptableObject where T : ScriptableObject {
        [SerializeField] private T[] items;
        private Dictionary<string,T> _dictionary = new();

        public Optional<T> Get(string name) {
            if(_dictionary.IsEmpty()) ResetState();

            if(_dictionary.TryGetValue(name, out T value)) {
                return new Optional<T> (value);
            }
            DebugUtil.DebugError(name + " not found", this.name);
            return Optional<T>.None();
        }

        void OnEnable() => Clear();
        void OnDisable() => Clear();

        void Clear() =>  _dictionary.Clear();

        void ResetState() {
            if(items == null) {
                _dictionary = new();
            } else {
                _dictionary = items.ToDictionary(x=>x.name, x=>x);
            }
        }

        public bool ContainsKey(string key) {
            if(_dictionary.IsEmpty()) ResetState();
            return _dictionary.ContainsKey(key);
        }

        public void ForEach(Action<T> action) {
            items.ForEach(action);
        }
    }
}