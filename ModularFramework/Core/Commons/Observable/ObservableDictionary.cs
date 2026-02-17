using System;
using System.Collections;
using System.Collections.Generic;

namespace ModularFramework
{
    public class ObservableDictionary<TKey, TValue> : IDisposable, IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dictionary;

        public Action<TKey, TValue> OnAdd;

        /// <summary>
        /// in-place change in value will not trigger OnItemChange
        /// </summary>
        public Action<TKey, TValue, TValue> OnItemChange;

        public Action<TKey, TValue> OnRemove;
        public Action OnClear;

        public ObservableDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public void Dispose()
        {
            Clear();
        }

        public ICollection<TKey> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            OnAdd?.Invoke(key, value);
        }

        public void Clear()
        {
            _dictionary.Clear();
            OnClear?.Invoke();
        }

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool Contains(KeyValuePair<TKey, TValue> item) => TryGetValue(item.Key, out TValue val) &&
                                                                 EqualityComparer<TValue>.Default.Equals(val,
                                                                     item.Value);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array Index Out of Range");

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(
                    "The number of items in the SyncDictionary is greater than the available space from arrayIndex to the end of the destination array");

            int i = arrayIndex;
            foreach (KeyValuePair<TKey, TValue> item in _dictionary)
            {
                array[i] = item;
                i++;
            }
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.TryGetValue(key, out TValue oldItem) && _dictionary.Remove(key))
            {
                OnRemove?.Invoke(key, oldItem);
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool result = _dictionary.Remove(item.Key);
            if (result)
                OnRemove?.Invoke(item.Key, item.Value);

            return result;
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;


        public TValue this[TKey i]
        {
            get => _dictionary[i];
            set
            {
                if (ContainsKey(i))
                {
                    TValue oldItem = _dictionary[i];
                    _dictionary[i] = value;
                    OnItemChange?.Invoke(i, oldItem, value);
                }
                else
                {
                    _dictionary[i] = value;
                    OnAdd?.Invoke(i, value);
                }
            }
        }
    }
}