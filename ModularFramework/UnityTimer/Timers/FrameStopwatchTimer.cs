using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts up from zero to infinity.  Great for measuring durations.
    /// </summary>
    public class FrameStopwatchTimer : Timer {
        public FrameStopwatchTimer() : base(0) { }

        public override void Tick() {
            if (IsRunning) {
                CurrentFrameCount += 1;
                base.Tick();
            }
        }

        public override bool IsFinished => false;
    }
}