using System;
using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.Input
{
    
    /// <summary>
    /// InputEventListener listen to ActionTiming event channels, and invoke OnLongPress if the event is held for more than longPressDelay seconds.
    /// </summary>
    [Serializable]
    public class LongPressAction : IDisposable
    {
        public Action OnLongPress;
        public float longPressDelay = 0.3f;
        private float _pressedTime;
        private uint _scheduledId;
        
        public void InputEventListener(ActionTiming inputEvent)
        {
            CancelLongPressEvent();
            if (inputEvent is ActionTiming.STARTED or ActionTiming.PERFORMED)
            {
                ScheduleLongPressEvent();
            }
        }

        public void InputEventListener((ActionTiming, Vector3) inputEvent)
        {
            CancelLongPressEvent();
            if (inputEvent.Item1 is ActionTiming.STARTED or ActionTiming.PERFORMED)
            {
                ScheduleLongPressEvent();
            }
        }
        
        public void InputEventListener((ActionTiming, Vector2) inputEvent)
        {
            CancelLongPressEvent();
            if (inputEvent.Item1 is ActionTiming.STARTED or ActionTiming.PERFORMED)
            {
                ScheduleLongPressEvent();
            }
        }
        
        private void ScheduleLongPressEvent()
        {
            _pressedTime = Time.time;
            _scheduledId = TimerManager.Schedule(longPressDelay, OnLongPress);
        }

        private void CancelLongPressEvent()
        {
            _pressedTime = 0;
            if (_scheduledId > 0)
            {
                TimerManager.RemoveSchedule(_scheduledId);
                _scheduledId = 0;
            }
        }

        public bool IsLongPress()
        {
            return _pressedTime > 0 && Time.time - _pressedTime > longPressDelay;
        }

        public void Dispose()
        {
            CancelLongPressEvent();
        }
    }
}