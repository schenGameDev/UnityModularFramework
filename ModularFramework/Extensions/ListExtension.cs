using System;
using System.Collections.Generic;
using System.Linq;

public static class ListExtension {
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    { // New Fisher-Yates
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static T AddAndReturn<T>(this IList<T> list, T ele) {
        list.Add(ele);
        return ele;
    }

    public static T InsertAndReturn<T>(this IList<T> list, int index, T ele) {
        list.Insert(index, ele);
        return ele;
    }

    public static T RemoveAtAndReturn<T>(this IList<T> list, int n) {
        var ele = list[n];
        list.RemoveAt(n);
        return ele;
    }

    public static T Pop<T>(this IList<T> list) {
        var ele = list[^1];
        list.RemoveAt(list.Count - 1);
        return ele;
    }

    public static List<int> FindAllIndex<T>(this IList<T> list, T target) {
        int count = 0;
        List<int> found = new();
        foreach(T i in list) {
            if(target.Equals(i)) {
                found.Add(count);
            }
            count+=1;
        }
        return found;
    }
    /// <summary>
    /// Compute each element in the list with the given function
    /// </summary>
    public static void Compute<T>(this IList<T> source, Func<T,T> action) {
        for(int i=0;i<source.Count; i+=1) {
            source[i] = action(source[i]);
        }
    }

    /// <summary>
    /// Replace the first occurrence of oldValue with newValue
    /// </summary>
    public static int Replace<T>(this IList<T> source, T oldValue, T newValue)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var index = source.IndexOf(oldValue);
        if (index != -1)
            source[index] = newValue;
        return index;
    }

    /// <summary>
    /// replace all occurrences of oldValue with newValue
    /// </summary>
    public static void ReplaceAll<T>(this IList<T> source, T oldValue, T newValue)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        int index = -1;
        do
        {
            index = source.IndexOf(oldValue);
            if (index != -1)
                source[index] = newValue;
        } while (index != -1);
    }
    /// <summary>
    /// Clear list and replace with other list
    /// </summary>
    public static void ReplaceWith<T>(this IList<T> source, IList<T> other) {
        source.Clear();
        source.AddRange(other);
    }
    // public static IEnumerable<T> Replace<T>(this IEnumerable<T> source, T oldValue, T newValue)
    // {
    //     if (source == null)
    //         throw new ArgumentNullException(nameof(source));

    //     return source.Select(x => EqualityComparer<T>.Default.Equals(x, oldValue) ? newValue : x);
    // }
    
    public static List<T> AsSingletonList<T>(this T source)
    {
        return new List<T> { source };
    }
}