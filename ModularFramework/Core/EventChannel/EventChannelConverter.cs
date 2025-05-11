using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework
{
    public abstract class EventChannelConverter<X,Y>: ScriptableObject
    {
        [SerializeField] private EventChannel<X> from;
        [SerializeField] private EventChannel<Y> to;

        private void OnEnable()
        {
            from?.AddListener(Transfer);
        }

        private void OnDisable()
        {
            from?.RemoveListener(Transfer);
        }
        
        protected abstract Optional<Y> Convert(X message);

        private void Transfer(X message)
        {
            var converted = Convert(message);
            if(converted.HasValue) to?.Raise(converted.Get());
        }
    }
}