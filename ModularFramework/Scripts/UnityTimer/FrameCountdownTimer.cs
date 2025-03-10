using UnityEngine;

namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific value to zero.
    /// </summary>
    public class FrameCountdownTimer : Timer {
        public FrameCountdownTimer(int value) : base(value) { }
        public float DeltaTime {get; protected set;}

        public override void Tick() {
            if (!IsRunning) return;
            if (CurrentFrameCount > 0) {
                CurrentFrameCount -= 1;
                DeltaTime += Time.deltaTime;
            }
            base.Tick();
            if (CurrentFrameCount <= 0) {
                Stop();
            }
        }

        public override bool IsFinished => CurrentFrameCount <= 0;

        public override void Reset()
        {
            base.Reset();
            DeltaTime = 0;
        }
    }
}