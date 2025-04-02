using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Commons;

public static class DictionaryExtension {
    public static Optional<TValue> Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
        if(dict.TryGetValue(key, out TValue v)) {
            return v;
        }
        return Optional<TValue>.None();
    }
    
    public static TValue AddIfAbsent<TKey,TValue>(this IDictionary<TKey, TValue> dict,
                                                TKey key, TValue value) {
        if(dict.TryGetValue(key, out TValue v)) {
            return v;
        }
        dict.Add(key, value);
        return value;
    }

    public static TValue GetOrCreateDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        TValue ret;
        if (!dictionary.TryGetValue(key, out ret))
        {
            ret = new TValue();
            dictionary[key] = ret;
        }
        return ret;
    }

    public static bool AddOrCompute<TKey,TValue>(this IDictionary<TKey, TValue> dict,
                                                TKey key, TValue value, Action<TValue> action) {
        if(dict.TryGetValue(key, out TValue v)) {
            action(v);
            return false;
        }
        dict.Add(key, value);
        return true;
    }

    public static void RemoveWhere<TKey,TValue>(this IDictionary<TKey, TValue> dict, Predicate<TKey> match) {
        var toRemove = dict.Keys.Where(k=>match(k)).ToList();
        foreach (var key in toRemove) {
            dict.Remove(key);
        }
    }

    public static void RemoveIfValue<TKey,TValue>(this IDictionary<TKey, TValue> dict, Predicate<TValue> match) {
        var toRemove = dict.Where(e=>match(e.Value)).Select(e=>e.Key).ToList();
        foreach (var key in toRemove) {
            dict.Remove(key);
        }
    }

    public static void RemoveWhere<TKey,TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, TValue, bool> match) {
        var toRemove = dict.Where(e=>match(e.Key, e.Value)).Select(e=>e.Key).ToList();
        foreach (var key in toRemove) {
            dict.Remove(key);
        }
    }

    public static void RemoveAll<TKey,TValue>(this IDictionary<TKey,TValue> dict, IEnumerable<TKey> keys) {
        foreach (var key in keys) {
            dict.Remove(key);
        }
    }

    public static void RetainAll<TKey,TValue>(this IDictionary<TKey,TValue> dict, IEnumerable<TKey> keys) {
        var toRemove = new HashSet<TKey>(dict.Keys);
        toRemove.ExceptWith(keys);
        foreach (var key in toRemove) {
            dict.Remove(key);
        }
    }

    public static void RetainAll<TKey,TValue>(this IDictionary<TKey,TValue> dict, IEnumerable<TKey> keys, out Dictionary<TKey,TValue> removed) {
        var toRemove = new HashSet<TKey>(dict.Keys);
        removed = new();
        toRemove.ExceptWith(keys);
        foreach (var key in toRemove) {
            if(dict.Remove(key, out var v)) {
                removed.Add(key,v);
            }
        }
    }

    public static bool TrySetValue<TKey,TValue>(this IDictionary<TKey,TValue> dict, TKey key, TValue value) {
        if(dict.ContainsKey(key)) {
            dict[key] = value;
            return true;
        }
        return false;
    }

    public static bool AddOrOverwriteIf<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
        TValue value, Func<TValue, TValue, bool> comparison) where TKey : notnull
    {
        if (!dictionary.ContainsKey(key))
        {
        dictionary.Add(key, value);
        return true;
        }

        if (comparison(dictionary[key], value))
        {
        dictionary[key] = value;
        return true;
        }

        return false;
    }

    public static void ForEach<TKey,TValue>(this IDictionary<TKey, TValue> dict, Action<TKey, TValue> action) {
        dict.ForEach(e=>action(e.Key, e.Value));
    }

    public static string Join<TKey,TValue>(this IDictionary<TKey, TValue> dict) {
        return dict.Select(e=>string.Format("({0},{1})",e.Key, e.Value)).Join();
    }
}