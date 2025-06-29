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

    public static bool RemoveWhere<T>(this ICollection<T> list, Predicate<T> match) {
        List<T> index = new();
        int i = 0;
        foreach (T item in list) {
            if(match(item)) {
                index.Add(item);
            }
            i+=1;
        }
        if(index.Count == 0) return false;
        foreach(T j in index) list.Remove(j);
        return true;
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

