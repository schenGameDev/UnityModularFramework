using System;
using System.Collections;
using System.Collections.Generic;

namespace ModularFramework
{
    public class ObservableSet<T> : IDisposable, ISet<T>
    {
        private readonly ISet<T> _set;

        public Action<T> OnAdd;
        public Action<T> OnRemove;
        public Action OnClear;

        public ObservableSet()
        {
            _set = new HashSet<T>();
        }

        public ObservableSet(ISet<T> set)
        {
            _set = set;
        }

        public void Dispose()
        {
            Clear();
        }

        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Add(T item)
        {
            if (_set.Add(item))
            {
                OnAdd?.Invoke(item);
                return true;
            }

            return false;
        }

        void ICollection<T>.Add(T item)
        {
            if (_set.Add(item))
                OnAdd?.Invoke(item);
        }

        public void Clear()
        {
            _set.Clear();
            OnClear?.Invoke();
        }

        public bool Contains(T item) => _set.Contains(item);

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == this)
            {
                Clear();
                return;
            }

            // remove every element in other from this
            foreach (T element in other)
                Remove(element);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other is ISet<T> otherSet)
                IntersectWithSet(otherSet);
            else
            {
                HashSet<T> otherAsSet = new HashSet<T>(other);
                IntersectWithSet(otherAsSet);
            }
        }

        private void IntersectWithSet(ISet<T> otherSet)
        {
            List<T> elements = new List<T>(_set);

            foreach (T element in elements)
                if (!otherSet.Contains(element))
                    Remove(element);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == this)
                Clear();
            else
                foreach (T element in other)
                    if (!Remove(element))
                        Add(element);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other != this)
                foreach (T element in other)
                    Add(element);
        }

        public bool Remove(T item)
        {
            if (_set.Remove(item))
            {
                OnRemove?.Invoke(item);
                return true;
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

        public int Count => _set.Count;
        public bool IsReadOnly => false;
    }

}