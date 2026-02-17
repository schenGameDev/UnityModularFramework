using System;

namespace ModularFramework
{
    public class Observable<T> : IDisposable
    {
        private T _value;
        public Action<T> OnValueChanged;
        
        public T Value {
            get => _value;
            set => Set(value);
        }

        public static implicit operator T(Observable<T> observable) => observable._value;

        public Observable(T value)
        {
            _value = value;
        }

        public Observable()
        {
        }

        public void Set(T newValue) {
            if (Equals(_value, newValue)) return;
            _value = newValue;
            OnValueChanged?.Invoke(_value);
        }
        
        public void Dispose() {
            OnValueChanged = null;
            _value = default;
        }
    }
}