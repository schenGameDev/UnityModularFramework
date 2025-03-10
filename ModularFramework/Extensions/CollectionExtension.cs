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

    public static void RetainAll<T>(this ICollection<T> collection, IEnumerable<T> others) {
        var toKeep = others is HashSet<T>? others : new HashSet<T>(others);
        collection.RemoveWhere(v => !toKeep.Contains(v));
    }

    public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> others) {
        foreach(T i in others) collection.Remove(i);
    }

    public static void RemoveWhere<T>(this ICollection<T> list, Predicate<T> match) {
        List<T> index = new();
        int i = 0;
        foreach (T item in list) {
            if(match(item)) {
                index.Add(item);
            }
            i+=1;
        }

        foreach(T j in index) list.Remove(j);
    }

    public static bool IsEmpty<T>(this ICollection<T> collection)
    {
        return collection.Count==0;
    }

    public static bool NonEmpty<T>(this ICollection<T> collection)
    {
        return collection.Count>0;
    }
}

