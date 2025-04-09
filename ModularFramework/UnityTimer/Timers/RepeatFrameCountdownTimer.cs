using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific frame value to zero then repeat.
    /// </summary>
    public class RepeatFrameCountdownTimer : Timer {
        private int _tickNTimes;
        private int _currentTriggeredTimes;
        public RepeatFrameCountdownTimer(int everyNFrame, int tickNTimes) : base(everyNFrame) {
            _tickNTimes = tickNTimes;
        }
        public float DeltaTime {get; protected set;}

        public override void Tick() {
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
                } else {
                    Reset();
                }
            }
        }

        public override bool IsFinished => CurrentFrameCount <= 0;

        public override void Reset()
        {
            base.Reset();
            DeltaTime = 0;
            _currentTriggeredTimes = 0;
        }
    }
}