using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Commons;

namespace ModularFramework.Utility
{
    public static class Registry<T> where T : class
    {
        private static readonly HashSet<T> ITEMS = new();
        
        public static bool TryAdd(T item) =>  item!=null && ITEMS.Add(item);
        
        
        public static bool Remove(T item) => ITEMS.Remove(item);
        
        public static int RemoveWhere(Predicate<T> predicate) => ITEMS.RemoveWhere(predicate);

        public static bool Contains(T item) =>  ITEMS.Contains(item);
        
        public static void Clear() => ITEMS.Clear();

        public static IEnumerable<T> All => ITEMS;

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