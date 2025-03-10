using System;
using UnityEngine;

namespace ModularFramework {
    [Serializable]
    public class Observable<T> {
        [SerializeField] T value;
        [SerializeField] EventChannel<T> onValueChanged;

        public T Value {
            get => value;
            set => Set(value);
        }

        public static implicit operator T(Observable<T> observable) => observable.value;

        public Observable(T value, EventChannel<T> callback = null) {
            this.value = value;
            onValueChanged = callback;
        }

        public void Set(T value) {
            if (Equals(this.value, value)) return;
            this.value = value;
            Invoke();
        }

        public void Invoke() {
            onValueChanged.Raise(value);
        }


        public void Dispose() {
            onValueChanged = null;
            value = default;
        }
    }
}