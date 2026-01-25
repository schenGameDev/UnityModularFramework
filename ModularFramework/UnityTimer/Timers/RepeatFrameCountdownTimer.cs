using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific frame value to zero then call OnTick() for the next N frames, then repeat.
    /// </summary>
    public class RepeatFrameCountdownTimer : Timer<RepeatFrameCountdownTimer> {
        private int _tickNTimes;
        private int _currentTriggeredTimes;
        public RepeatFrameCountdownTimer(int everyNFrame, int tickNTimes) : base(everyNFrame) {
            _tickNTimes = tickNTimes;
        }
        public float DeltaTime {get; protected set;}

        protected override void CustomTick() {
            if (!IsRunning) return;

            if (CurrentFrameCount > 0) {
                CurrentFrameCount -= 1;
            }

            DeltaTime += Time.deltaTime;

            if (CurrentFrameCount <= 0) {
                if(_currentTriggeredTimes < _tickNTimes) {
                    _currentTriggeredTimes += 1;
                    OnTick.Invoke();
                    DeltaTime = 0;
                } 
                if (_currentTriggeredTimes == _tickNTimes)
                {
                    Reset();
                }
            }
        }

        protected override bool FinishCondition() => CurrentFrameCount <= 0;

        protected override void CustomReset()
        {
            DeltaTime = 0;
            _currentTriggeredTimes = 0;
        }
    }
}