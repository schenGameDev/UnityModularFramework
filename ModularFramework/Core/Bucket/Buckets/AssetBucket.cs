using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework
{
    [CreateAssetMenu(fileName = "AssetBucket_SO", menuName = "Bucket/Asset Bucket")]
    public class AssetBucket : ScriptableObject
    {
        [SerializeField] private AssetIdentity[] items;
        private Dictionary<uint, AssetIdentity> _dictionary = new();

        public Optional<T> Get<T>(uint assetId) where T : Component {
            if(_dictionary.IsEmpty()) ResetState();

            if(_dictionary.TryGetValue(assetId, out var asset)) {
                return new Optional<T> (asset.GetComponent<T>());
            }
            DebugUtil.DebugError(name + " not found", this.name);
            return Optional<T>.None();
        }
        
        public Optional<AssetIdentity> Get(uint assetId) {
            if(_dictionary.IsEmpty()) ResetState();

            if(_dictionary.TryGetValue(assetId, out var asset)) {
                return new Optional<AssetIdentity>(asset);
            }
            DebugUtil.DebugError(name + " not found", this.name);
            return Optional<AssetIdentity>.None();
        }

        void OnEnable() => Clear();
        void OnDisable() => Clear();

        void Clear() =>  _dictionary.Clear();

        void ResetState() {
            if(items == null) {
                _dictionary = new();
            } else {
                _dictionary = items.ToDictionary(x=>x.assetId, x=>x);
            }
        }

        public bool ContainsKey(uint assetId) {
            if(_dictionary.IsEmpty()) ResetState();
            return _dictionary.ContainsKey(assetId);
        }
        
        public void ForEach(Action<AssetIdentity> action) {
            Array.ForEach(items, action);
        }
    }
}