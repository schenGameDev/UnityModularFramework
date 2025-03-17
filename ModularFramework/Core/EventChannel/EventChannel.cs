using UnityEngine;
using UnityEngine.Events;
using ModularFramework.Utility;

namespace ModularFramework {
    public interface IEventChannel : ILive {
        public void Register() {
            GameRunner.Instance?.RegisterEventChannel(this);
        }

        public void Unregister() {
            GameRunner.Instance?.UnregisterEventChannel(this);
        }
    }

    public abstract class EventChannel<T> : ScriptableObject, IEventChannel, IResetable
    {
        protected static string NO_EVENT_READER = "no event reader registered.";

        protected UnityAction<T> action;
        private T _lastSend;
        [SerializeField] private bool _log;
        [field: SerializeField] public bool Live { get; set; } = true; // can be used to pause channel while listener handles action, then unpause once done

        public void Raise(T param)
        {
            _lastSend = param;
            _isRequestable = true;
            if(Live && IsEventSubscribed()) {
                if(_log) DebugUtil.DebugLog("Triggered", name);
                Invoke(param);
            } else if(_log) {
                DebugUtil.DebugLog("Blocked", name);
            }
        }

        protected virtual void Invoke(T param)
        {
            action.Invoke(param);
        }

        public bool TryRequest(out T storedValue) {
            storedValue = _lastSend;
            return IsRequestable;
        }

        private bool _isRequestable;
        protected virtual bool IsRequestable => Live && _isRequestable;

        protected virtual bool IsEventSubscribed() {
            if(action==null) {
                DebugUtil.DebugWarn(NO_EVENT_READER);
                return false;
            }
            return true;
        }

        public virtual void AddListener(UnityAction<T> action) {
            (this as IEventChannel).Register();
            this.action += action;
        }

        public virtual void RemoveListener(UnityAction<T> action) {
            this.action -= action;
            if(this.action==null) {
                (this as IEventChannel).Unregister();
            }
        }

        public void Reset()
        {
            _isRequestable = false;
        }
    }

    [CreateAssetMenu(menuName = "Event Channel/Event Channel",fileName = "Channel")]
    public class EventChannel : ScriptableObject,IEventChannel {
        protected static string NO_EVENT_READER = "no event reader registered.";

        protected UnityAction action;
        [SerializeField] private bool _log;
        [field: SerializeField] public bool Live { get; set; } = true;
        public void Raise()
        {
            if(Live && IsEventSubscribed()) {
                if(_log) DebugUtil.DebugLog("Triggered", name);
                Invoke();
            } else if(_log) {
                DebugUtil.DebugLog("Blocked", name);
            }
        }

        protected virtual void Invoke() {
            action.Invoke();
        }

        protected virtual bool IsEventSubscribed() {
            if(action==null) {
                DebugUtil.DebugWarn(NO_EVENT_READER);
                return false;
            }
            return true;
        }

        public virtual void AddListener(UnityAction action) {
            (this as IEventChannel).Register();
            this.action += action;
        }

        public virtual void RemoveListener(UnityAction action) {
            this.action -= action;
            if(this.action==null) {
                (this as IEventChannel).Unregister();
            }
        }
    }
}