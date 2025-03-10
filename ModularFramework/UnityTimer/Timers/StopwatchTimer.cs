using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    /// <summary>
    /// Timer that counts up from zero to infinity.  Great for measuring durations.
    /// </summary>
    public class StopwatchTimer : Timer {
        public StopwatchTimer() : base(0.0f) { }

        public override void Tick() {
            if (IsRunning) {
                CurrentTime += Time.deltaTime;
                base.Tick();
            }
        }

        public override bool IsFinished => false;
    }
}