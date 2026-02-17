using System;
using System.Collections.Generic;
using System.Linq;

public static class CollectionsExtension {
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            collection.Add(item);
        }
    }
    
    public static bool ContainsAll<T>(this ICollection<T> collection, IEnumerable<T> others) {
        foreach (T i in others)
        {
            if(!collection.Contains(i)) return false;
        }
        return true;
    }
    
    public static bool ContainsAny<T>(this ICollection<T> collection, IEnumerable<T> others) {
        foreach (T i in others)
        {
            if(collection.Contains(i)) return true;
        }
        return false;
    }
    public static void RetainAll<T>(this ICollection<T> collection, IEnumerable<T> others) {
        var toKeep = others is HashSet<T>? others : new HashSet<T>(others);
        collection.RemoveWhere(v => !toKeep.Contains(v));
    }

    public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> others) {
        foreach(T i in others) collection.Remove(i);
    }

    public static int RemoveWhere<T>(this ICollection<T> collection, Predicate<T> match) {
        List<T> toRemove = new();
        foreach (var item in collection) {
            if(match(item)) {
                toRemove.Add(item);
            }
        }

        if (toRemove.Count == 0) return 0;
        foreach(var j in toRemove) collection.Remove(j);
        return collection.Count;
    }

    public static bool IsEmpty<T>(this ICollection<T> collection)
    {
        return collection==null || collection.Count==0;
    }

    public static bool NonEmpty<T>(this ICollection<T> collection)
    {
        return !IsEmpty(collection);
    }
}

