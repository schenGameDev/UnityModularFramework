using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific frame value to zero.
    /// </summary>
    public class FrameCountdownTimer : Timer<FrameCountdownTimer> {
        public FrameCountdownTimer(int value) : base(value) { }
        public float DeltaTime {get; protected set;}

        protected override void CustomTick() {
            if (!IsRunning) return;
            if (CurrentFrameCount > 0) {
                CurrentFrameCount -= 1;
                DeltaTime += Time.deltaTime;
            }
            OnTick.Invoke();
            if (CurrentFrameCount <= 0) {
                Stop();
            }
        }

        protected override bool FinishCondition() => CurrentFrameCount <= 0;

        protected override void CustomReset()
        {
            DeltaTime = 0;
        }
    }
}