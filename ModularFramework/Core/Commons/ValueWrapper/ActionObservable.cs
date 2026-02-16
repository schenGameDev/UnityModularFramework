using System;

namespace ModularFramework
{
    public class ActionObservable<T> : IDisposable
    {
        T value;
        Action<T> onValueChanged;
        
        public T Value {
            get => value;
            set => Set(value);
        }

        public static implicit operator T(ActionObservable<T> observable) => observable.value;

        public ActionObservable(T value, Action<T> callback) {
            this.value = value;
            onValueChanged = callback;
        }

        public void Set(T newValue) {
            if (Equals(value, newValue)) return;
            value = newValue;
            Invoke();
        }

        public void Invoke() {
            onValueChanged.Invoke(value);
        }


        public void Dispose() {
            onValueChanged = null;
            value = default;
        }
    }
}