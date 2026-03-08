using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ModularFramework
{
    /// <summary>
    /// A static generic registry that stores key-value pairs in a dictionary with thread-safe add operations and query methods.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the registry.</typeparam>
    /// <typeparam name="TValue">The type of values in the registry (must be a reference type).</typeparam>
    public static class DictRegistry<TKey,TValue> where TValue : class
    {
        private static readonly Dictionary<TKey,TValue> ITEMS = new();
        /// <summary>
        /// This method will not add the item if the key already exists, and return false. Otherwise, it will add the item and return true.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd(TKey key, TValue item)
        {
            return item != null && ITEMS.TryAdd(key, item);
        }
        
        public static void Replace(TKey key, TValue item)
        {
            if (item == null) return;
            ITEMS[key] = item;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove(TKey key) => ITEMS.Remove(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemoveWhere(Predicate<TValue> predicate) => ITEMS.Values.RemoveWhere(predicate);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(TKey key) => ITEMS.ContainsKey(key);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear() => ITEMS.Clear();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get(TKey key) => ITEMS.GetValueOrDefault(key);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue(TKey key, out TValue value) => ITEMS.TryGetValue(key, out value);
        
        public static Dictionary<TKey,TValue> All => ITEMS;
        
    }
}