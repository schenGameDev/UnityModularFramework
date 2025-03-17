using UnityEngine;
using UnityEngine.Events;
using ModularFramework.Utility;

namespace ModularFramework {
    public abstract class TransferEventChannel<T> : EventChannel<T> {
        [SerializeField] EventChannel<T>[] _receivers;
        protected override void Invoke(T param)
        {
            _receivers.ForEach(r => r.Raise(param));
        }
        protected override bool IsRequestable => false;

        protected override bool IsEventSubscribed() {
            if(_receivers==null || _receivers.IsEmpty()) {
                DebugUtil.DebugWarn("no event reader registered.");
                return false;
            }
            return true;
        }
        static readonly string WARNING = "TransferEventChannel does not support Listener.";
        public override void AddListener(UnityAction<T> action) => DebugUtil.Warn(WARNING);
        public override void RemoveListener(UnityAction<T> action) => DebugUtil.Warn(WARNING);
    }

    [CreateAssetMenu(menuName = "Event Channel/Transfer Channel",fileName = "Channel")]
    public class TransferEventChannel : EventChannel {
        [SerializeField] EventChannel[] _receivers;
        protected override void Invoke()
        {
            _receivers.ForEach(r => r.Raise());
        }

        protected override bool IsEventSubscribed() {
            if(_receivers==null || _receivers.IsEmpty()) {
                DebugUtil.DebugWarn(NO_EVENT_READER);
                return false;
            }
            return true;
        }

        static readonly string WARNING = "TransferEventChannel does not support Listener.";
        public override void AddListener(UnityAction action) => DebugUtil.Warn(WARNING);
        public override void RemoveListener(UnityAction action) => DebugUtil.Warn(WARNING);
    }
}