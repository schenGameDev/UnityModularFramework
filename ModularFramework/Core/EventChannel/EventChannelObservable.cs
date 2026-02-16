using System;
using UnityEngine;

namespace ModularFramework {
    [Serializable]
    public class EventChannelObservable<T> : IDisposable {
        [SerializeField] T value;
        [SerializeField] EventChannel<T> onValueChanged;

        public T Value {
            get => value;
            set => Set(value);
        }

        public static implicit operator T(EventChannelObservable<T> observable) => observable.value;

        public EventChannelObservable(T value) {
            this.value = value;
        }
        
        public EventChannelObservable() {}

        public void Set(T newValue) {
            if (Equals(value, newValue)) return;
            value = newValue;
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