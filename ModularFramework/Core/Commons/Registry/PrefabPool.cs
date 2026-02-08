using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ModularFramework
{
    /// <summary>
    /// Generic object pooling system for Unity prefabs that manages multiple pools indexed by asset IDs,
    /// the asset ID is provided by AssetIdentity component attached to prefab's root.
    /// If not, the pool will support only one prefab of type T
    /// </summary>
    /// <typeparam name="T">Unity Component</typeparam>
    public static class PrefabPool<T> where T : Component 
    {
        private static readonly Dictionary<uint,ObjectPool<T>> POOL = new();
        /// <summary>
        /// Get an instance from the pool associated with the given assetId.
        /// An exception will be thrown if the assetId is not registered.
        /// Consider using TryGet instead if you want to handle these cases gracefully.
        /// </summary>
        /// <param name="assetId">Provided by AssetIdentity</param>
        /// <returns></returns>
        public static T Get(uint assetId = 0) => POOL[assetId].Get();
        
        public static bool TryGet(uint assetId, out T instance)
        {
            if (POOL.TryGetValue(assetId, out var pool))
            {
                instance = pool.Get();
                return true;
            }
            instance = null;
            return false;
        }

        public static bool Release(T instance, uint assetId = 0)
        {
            if (!POOL.TryGetValue(assetId, out var pool)) return false;
            pool.Release(instance);
            return true;
        }

        public static uint Register(T prefab, Func<uint,T,ObjectPool<T>> createPoolFunc)
        {
            if (POOL.Count == 1 && POOL.ContainsKey(0))
            {
                Debug.Log($"prefab pool of {typeof(T).Name} is in single prefab mode, new prefab will be ignored");
                return 0;
            }
            
            uint assetId;
            if (prefab.TryGetComponent<AssetIdentity>(out var assetIdentity))
            {
                assetId = assetIdentity.assetId;
                // don't use TryAdd because value generating function
                // will be called even if key already exists
                if(POOL.ContainsKey(assetId)) return assetId;
            }
            else
            {
                Debug.LogWarning($"PrefabPool of {typeof(T).Name} will be in single prefab mode.");
                assetId = 0;
            }
            
            POOL.Add(assetId, createPoolFunc(assetId, prefab));

            return assetId;
        }
        
        public static void Clear()
        {
            foreach (var pool in POOL.Values)
            {
                try
                {
                    pool.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // already disposed before
                }
            }
            POOL.Clear();
        }
    }
}