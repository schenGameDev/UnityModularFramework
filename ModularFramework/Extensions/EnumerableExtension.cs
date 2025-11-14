using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Commons;

public static class EnumerableExtension {
    public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action) {
        foreach (var item in sequence) action(item);
    }
    
    public static void ForEachOrdered<T>(this IEnumerable<T> sequence, Action<int,T> action)
    {
        int index = 0;
        foreach (var item in sequence)
        {
            int i = index++;
            action(i,item);
        }
    }
    
    public static IEnumerable<T> Peek<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        return Iterator();

        IEnumerable<T> Iterator() // C# 7 Local Function
        {
        foreach(var item in source)
        {
            action(item);
            yield return item;
        }
        }
    }

    public static bool None<T>(this IEnumerable<T> sequence, Predicate<T> match) {
        foreach (var item in sequence) {
            if(match(item)) return false;
        }
        return true;
    }

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
    {
        return source.Skip(Math.Max(0, source.Count() - N));
    }

    public static string Join<T>(this IEnumerable<T> source) {
        return string.Join(",", source);
    }

    public static Optional<T> GetFirst<T>(this IEnumerable<T> source)
    {
        return source.First();
    }
}