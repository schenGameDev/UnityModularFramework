using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific value to zero then repeat.
    /// </summary>
    public class RepeatCountdownTimer : Timer<RepeatCountdownTimer> {
        private int _tickNTimes;
        private int _currentTriggeredTimes;
        public RepeatCountdownTimer(float everyNSeconds, int tickNTimes) : base(everyNSeconds) {
            _tickNTimes = tickNTimes;
        }
        public float DeltaTime {get; protected set;}
        protected override void CustomTick() {
            if (!IsRunning) return;

            if (CurrentTime > 0) {
                CurrentTime -= Time.deltaTime;
            }

            DeltaTime += Time.deltaTime;

            if (CurrentFrameCount <= 0) {
                if(_currentTriggeredTimes < _tickNTimes) {
                    _currentTriggeredTimes += 1;
                    OnTick.Invoke();
                    DeltaTime = 0;
                } else {
                    Reset();
                }
            }
        }

        protected override bool FinishCondition() => CurrentTime <= 0;

        protected override void CustomReset()
        { 
            DeltaTime = 0;
            _currentTriggeredTimes = 0;
        }
    }
}