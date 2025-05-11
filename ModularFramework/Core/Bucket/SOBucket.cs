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
        [SerializeField] private T[] _items;
        private Dictionary<string,T> _dictionary = new();

        public Optional<T> Get(string name) {
            if(_dictionary.IsEmpty()) Reset();

            if(_dictionary.TryGetValue(name, out T value)) {
                return new Optional<T> (value);
            }
            DebugUtil.DebugError(name + " not found", this.name);
            return Optional<T>.None();
        }

        void OnEnable() => Clear();
        void OnDisable() => Clear();

        void Clear() =>  _dictionary.Clear();

        void Reset() {
            if(_items == null) {
                _dictionary = new();
            } else {
                _dictionary = _items.ToDictionary(x=>x.name, x=>x);
            }
        }

        public bool ContainsKey(string name) {
            if(_dictionary.IsEmpty()) Reset();
            return _dictionary.ContainsKey(name);
        }

        public void ForEach(Action<T> action) {
            _items.ForEach(i => action(i));
        }
    }
}