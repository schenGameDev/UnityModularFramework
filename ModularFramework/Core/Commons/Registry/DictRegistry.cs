using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ModularFramework
{
    public static class DictRegistry<TKey,TValue> where TValue : class
    {
        private static readonly Dictionary<TKey,HashSet<TValue>> ITEMS = new();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd(TKey key, TValue item)
        {
            if (item==null) return false;
            if(ITEMS.TryGetValue(key, out var existingItems)) {
                return existingItems.Add(item);
            }
            ITEMS.Add(key, new HashSet<TValue>() {item});
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove(TKey key, TValue item)
            => ITEMS.TryGetValue(key, out var existingItems) && existingItems.Remove(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemoveWhere(TKey key, Predicate<TValue> predicate)
        {
            if (!ITEMS.TryGetValue(key, out var existingItems)) return 0;
            return existingItems.RemoveWhere(predicate);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(TKey key, TValue item) 
            => ITEMS.TryGetValue(key, out var existingItems) && existingItems.Contains(item);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(TKey key) => ITEMS.Remove(key);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearAll() => ITEMS.Clear();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<TValue> Get(TKey key) => ITEMS.TryGetValue(key, out var existingItems) ? existingItems : new HashSet<TValue>();
        
        public static Dictionary<TKey,HashSet<TValue>> All => ITEMS;
        
        public static IEnumerable<TValue> Get(TKey key, SelectionStrategy<TValue> strategy, params FilterStrategy<TValue>[] filters) 
            => strategy(Filter(key,filters));
        
        public static IEnumerable<TValue> Filter(TKey key, params FilterStrategy<TValue>[] filters)
        {
            return filters == null || filters.Length == 0 ? Get(key) : Get(key).Where(target => filters.All(filter => filter(target)));
        }
        
    }
}