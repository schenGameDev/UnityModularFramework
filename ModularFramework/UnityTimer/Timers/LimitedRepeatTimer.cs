using UnityEngine;

namespace UnityTimer
{
    /// <summary>
    /// Interval timer with a maximum trigger count. Triggers every M seconds, up to N total times.
    /// </summary>
    public class LimitedRepeatTimer : Timer<LimitedRepeatTimer> {
        private int _totalTriggeredTimes;
        public LimitedRepeatTimer(float everyNSeconds, int totalTriggeredTimes) : base(everyNSeconds) {
            _totalTriggeredTimes = totalTriggeredTimes;
        }
        public float DeltaTime {get; protected set;}
        protected override void CustomTick() {
            if (!IsRunning) return;

            if (CurrentTime > 0) {
                CurrentTime -= Time.deltaTime;
            }

            DeltaTime += Time.deltaTime;

            if (CurrentTime <= 0)
            {
                _totalTriggeredTimes -= 1;
                OnTick.Invoke();
                Reset();
                if (_totalTriggeredTimes <= 0)
                {
                    Stop();
                }
            }
        }

        protected override bool FinishCondition() => CurrentTime <= 0;

        protected override void CustomReset()
        { 
            DeltaTime = 0;
        }
    }
}