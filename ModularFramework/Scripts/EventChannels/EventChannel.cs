using UnityEngine;
using UnityEngine.Events;

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
    protected UnityAction<T> action;
    private T _lastSend;
    [SerializeField] private bool _live = true;
    [SerializeField] private bool _log;
    public bool Live { get => _live; set => _live = value; }

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
            DebugUtil.DebugWarn("no event reader registered.");
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
    protected UnityAction action;
    [SerializeField] private bool _live = true;
    [SerializeField] private bool _log;
    public bool Live { get => _live; set => _live = value; }
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
            DebugUtil.DebugWarn("no event reader registered.");
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
#region Implementation


// [CreateAssetMenu(menuName = "Events/Transform Event Channel",fileName = "Channel")]
// public class TransformEventChannelSO : EventChannel<Transform> {}

// [CreateAssetMenu(menuName = "Events/Int Event Channel", fileName = "Channel")]
// public class IntEventChannelSO : EventChannel<int> {}

// [CreateAssetMenu(menuName = "Events/Float Event Channel", fileName = "Channel")]
// public class FloatEventChannelSO : EventChannel<float> {}



// [CreateAssetMenu(menuName = "Events/Float,Transform Event Channel", fileName = "Channel")]
// public class FloatTransformEventChannelSO : EventChannel<(float,Transform)> {}
#endregion
