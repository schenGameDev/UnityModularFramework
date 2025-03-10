using System;

namespace ModularFramework.Commons {
    public struct Vector<T> : IEquatable<Vector<T>> where T : class {
        public T From;
        public T To;

        public Vector(T from, T to) {
            From = from;
            To = to;
        }

        public override int GetHashCode()
        {
            return (From, To).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj !=null && obj is Vector<T> other && this.Equals(other);
        }

        public bool Equals(Vector<T> tr) {
            return From == tr.From && To == tr.To;
        }
    }
}