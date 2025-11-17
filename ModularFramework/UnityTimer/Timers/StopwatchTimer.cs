using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    /// <summary>
    /// Timer that counts up from zero to infinity.  Great for measuring durations.
    /// </summary>
    public class StopwatchTimer : Timer<StopwatchTimer> {
        public StopwatchTimer() : base(0.0f) { }

        protected override void CustomTick() {
            if (IsRunning) {
                CurrentTime += Time.deltaTime;
                OnTick.Invoke();
            }
        }

        protected override bool FinishCondition() => false;

        protected override void CustomReset() { }
    }
}