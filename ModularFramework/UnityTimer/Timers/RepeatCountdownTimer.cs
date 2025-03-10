using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific value to zero then repeat.
    /// </summary>
    public class RepeatCountdownTimer : Timer {
        private int _tickNTimes;
        private int _currentTriggeredTimes;
        public RepeatCountdownTimer(float everyNSeconds, int tickNTimes) : base(everyNSeconds) {
            _tickNTimes = tickNTimes;
        }
        public float DeltaTime {get; protected set;}
        public override void Tick() {
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

        public override bool IsFinished => CurrentTime <= 0;

        public override void Reset()
        {
            base.Reset();
            DeltaTime = 0;
            _currentTriggeredTimes = 0;
        }
    }
}