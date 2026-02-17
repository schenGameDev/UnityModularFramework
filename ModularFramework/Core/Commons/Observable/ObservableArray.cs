using System;
using System.Collections;
using System.Collections.Generic;

namespace ModularFramework
{
    /// <summary>
    /// Array of fixed length, can not be resized, but can be modified in place. Will not allocate to heap when enumerating.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableArray<T> : IDisposable,IEnumerable<T>
    {
        private readonly T[] _array;
        private static readonly IEqualityComparer<T> COMPARER = EqualityComparer<T>.Default;
        
        public Action<int,T,T> OnItemChange;

        public ObservableArray(int capacity)
        {
            _array = new T[capacity];
        }

        public ObservableArray(params T[] array)
        {
            _array = array;
        }

        public void Dispose() {
            for(int i = 0; i < _array.Length; i++)
                _array[i] = default;
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public bool Contains(T item) => IndexOf(item) != -1;
        
        public int IndexOf(T item)
        {
            for (int i = 0; i < _array.Length; i++)
                if (COMPARER.Equals(item, _array[i]))
                    return i;
            return -1;
        }

        public void CopyTo(T[] array, int arrayIndex) => _array.CopyTo(array, arrayIndex);
        
        public int Length => _array.Length;
        
        
        public T this[int index]
        {
            get => _array[index];
            set
            {
                if (!COMPARER.Equals(_array[index], value))
                {
                    T oldItem = _array[index];
                    _array[index] = value;
                    OnItemChange?.Invoke(index, oldItem, value);
                }
            }
        }
        
        // better than default, this will not allocate to heap
        public struct Enumerator : IEnumerator<T>
        {
            readonly ObservableArray<T> _array;
            int _index;

            public T Current { get; private set; }

            public Enumerator(ObservableArray<T> list)
            {
                _array = list;
                _index = -1;
                Current = default;
            }

            public bool MoveNext()
            {
                if (++_index >= _array.Length)
                    return false;

                Current = _array[_index];
                return true;
            }

            public void Reset() => _index = -1;
            object IEnumerator.Current => Current;
            public void Dispose() { }
        }
    }
    
}