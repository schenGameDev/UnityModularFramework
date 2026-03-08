using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ModularFramework.Commons;

namespace ModularFramework
{
    /// <summary>
    /// A static generic registry that stores unique items in a hash set with filtering and selection strategy support.
    /// </summary>
    /// <typeparam name="T">The type of items in the registry (must be a reference type).</typeparam>
    public static class Registry<T> where T : class
    {
        private static readonly HashSet<T> ITEMS = new();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd(T item) =>  item!=null && ITEMS.Add(item);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove(T item) => ITEMS.Remove(item);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemoveWhere(Predicate<T> predicate) => ITEMS.RemoveWhere(predicate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(T item) =>  ITEMS.Contains(item);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear() => ITEMS.Clear();

        public static IEnumerable<T> All => ITEMS;
        
        public static int Count => ITEMS.Count;

        public static Optional<T> GetFirst() =>  ITEMS.GetFirst();
        
        public static IEnumerable<T> Get(SelectionStrategy<T> strategy, params FilterStrategy<T>[] filters) => strategy(Filter(filters));
        
        public static IEnumerable<T> Filter(params FilterStrategy<T>[] filters)
        {
            return filters == null || filters.Length == 0 ? ITEMS : ITEMS.Where(target => filters.All(filter => filter(target)));
        }
        
    }
    
    public delegate IEnumerable<T> SelectionStrategy<T>(IEnumerable<T> items);
    public delegate bool FilterStrategy<in T>(T item);
}