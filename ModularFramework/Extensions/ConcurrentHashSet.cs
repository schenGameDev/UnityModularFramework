using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class ConcurrentHashSet<T> {
    private readonly ConcurrentDictionary<T,byte> dictionary = new();
    public bool TryAdd(T item)
        {
            return dictionary.TryAdd(item, new());
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(T item)
        {
            return dictionary.ContainsKey(item);
        }

        public bool TryRemove(T item)
        {
            return dictionary.TryRemove(item, out byte value);
        }
        public HashSet<T> ToHashSet() {
            return new HashSet<T>(dictionary.Keys);
        }

    public int Count => dictionary.Count;
}