using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts up from zero to infinity.  Great for measuring durations.
    /// </summary>
    public class FrameStopwatchTimer : Timer<FrameStopwatchTimer> {
        public FrameStopwatchTimer() : base(0) { }

        protected override void CustomTick() {
            if (IsRunning) {
                CurrentFrameCount += 1;
                OnTick.Invoke();
            }
        }

        protected override void CustomReset() { }

        protected override bool FinishCondition() => false;
    }
}