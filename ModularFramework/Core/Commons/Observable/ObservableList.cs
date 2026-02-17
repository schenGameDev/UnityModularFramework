using System;
using System.Collections;
using System.Collections.Generic;

namespace ModularFramework
{
    public class ObservableList<T> : IDisposable,IList<T>
    {
        private readonly IList<T> _list;
        private static readonly IEqualityComparer<T> COMPARER = EqualityComparer<T>.Default;
        
        public Action<int,T> OnAdd;
        public Action<int,T,T> OnItemChange;
        public Action<int,T> OnRemove;
        public Action OnClear;

        public ObservableList()
        {
            _list = new List<T>();
        }

        public ObservableList(IList<T> list)
        {
            _list = list;
        }

        public void Dispose() {
            Clear();
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item)
        {
            _list.Add(item);
            OnAdd?.Invoke(_list.Count - 1, item);
        }
        
        public void Clear()
        {
            _list.Clear();
            OnClear?.Invoke();
        }

        public bool Contains(T item) => IndexOf(item) != -1;
        
        public int IndexOf(T item)
        {
            for (int i = 0; i < _list.Count; ++i)
                if (COMPARER.Equals(item, _list[i]))
                    return i;
            return -1;
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            OnAdd?.Invoke(index, item);
        }
        
        public void InsertRange(int index, IEnumerable<T> range)
        {
            foreach (T entry in range)
            {
                Insert(index, entry);
                index++;
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            bool result = index >= 0;
            if (result)
                RemoveAt(index);

            return result;
        }
        
        public void RemoveAt(int index)
        {
             T oldItem = _list.RemoveAtAndReturn(index);
             OnRemove?.Invoke(index, oldItem);
        }

        public void RemoveRange(int index, int count)
        {
            for(int i = index + count - 1; i >= index; i-=1)
                RemoveAt(i);
        }

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        
        
        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (!COMPARER.Equals(_list[index], value))
                {
                    T oldItem = _list[index];
                    _list[index] = value;
                    OnItemChange?.Invoke(index, oldItem, value);
                }
            }
        }
        
        // better than default, this will not allocate to heap
        public struct Enumerator : IEnumerator<T>
        {
            readonly ObservableList<T> _list;
            int _index;

            public T Current { get; private set; }

            public Enumerator(ObservableList<T> list)
            {
                _list = list;
                _index = -1;
                Current = default;
            }

            public bool MoveNext()
            {
                if (++_index >= _list.Count)
                    return false;

                Current = _list[_index];
                return true;
            }

            public void Reset() => _index = -1;
            object IEnumerator.Current => Current;
            public void Dispose() { }
        }
    }
    
}