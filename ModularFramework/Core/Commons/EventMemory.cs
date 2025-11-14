using UnityEngine;

namespace ModularFramework.Commons
{
    /// <summary>
    /// Remember an event that happened with expiry time
    /// </summary>
    public class EventMemory : IResetable
    {
        private readonly float _expireTime;
        private float _lastTimestamp;
        public EventMemory(float expireTime)
        {
            _expireTime = expireTime;
            _lastTimestamp = - expireTime - 1;
        }

        public void Record()
        {
            _lastTimestamp = Time.time;
        }
        
        public void Reset()
        {
            _lastTimestamp = -_expireTime - 1;
        }
        
        public static implicit operator bool(EventMemory eventMemory) => eventMemory.HasHappened();

        private bool HasHappened()
        {
            return _lastTimestamp + _expireTime >= Time.time;
        }
    }
}