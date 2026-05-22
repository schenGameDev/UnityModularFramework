using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class ListExtension {
    private static Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list)
    { // New Fisher-Yates
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AddAndReturn<T>(this IList<T> list, T ele) {
        list.Add(ele);
        return ele;
    }

    public static T InsertAndReturn<T>(this IList<T> list, int index, T ele) {
        list.Insert(index, ele);
        return ele;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RemoveAtAndReturn<T>(this IList<T> list, int n) {
        var ele = list[n];
        list.RemoveAt(n);
        return ele;
    }

    public static bool RemoveRange<T>(this IList<T> list, int index, int count)
    {
        if (index < 0 || index >= list.Count)
            return false;
    
        if (count < 0 || index + count > list.Count)
            return false;
    
        // Remove in reverse order to avoid index shifting issues
        for (int i = index + count - 1; i >= index; i--)
        {
            list.RemoveAt(i);
        }
    
        return true;
    }
    public static List<T> RemoveRangeAndReturn<T>(this IList<T> list, int index, int length)
    {
        if (index < 0 || index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
    
        if (length < 0 || index + length > list.Count)
            throw new ArgumentOutOfRangeException(nameof(length));
    
        List<T> removed = new List<T>(length);
    
        // Collect elements first
        for (int i = 0; i < length; i++)
        {
            removed.Add(list[index + i]);
        }
    
        // Remove in reverse order to avoid index shifting issues
        for (int i = index + length - 1; i >= index; i--)
        {
            list.RemoveAt(i);
        }
    
        return removed;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Pop<T>(this IList<T> list) {
        var ele = list[^1];
        list.RemoveAt(list.Count - 1);
        return ele;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    
    public static int RemoveWhere<T>(this IList<T> list, Predicate<T> match) {
        List<int> toRemove = new List<int>();
        for (int i = 0; i < list.Count; ++i)
            if (match(list[i]))
                toRemove.Add(i);

        foreach (int index in toRemove)
            list.RemoveAt(index);

        return toRemove.Count;
    }

}