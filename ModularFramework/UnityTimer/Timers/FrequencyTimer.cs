using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    /// <summary>
    /// Timer that ticks at a specific frequency. (N times per second)
    /// </summary>
    public class FrequencyTimer : Timer<FrequencyTimer> {
        public int TicksPerSecond { get; private set; }
        float _timeThreshold;

        public FrequencyTimer(int ticksPerSecond) : base(0f) {
            CalculateTimeThreshold(ticksPerSecond);
        }

        protected override void CustomTick() {
            if (!IsRunning) return;
            if (currentTime >= _timeThreshold) {
                currentTime -= _timeThreshold;
            }
            base.Tick();
            if (currentTime < _timeThreshold) {
                currentTime += Time.deltaTime;
            }
        }

        protected override bool FinishCondition() => !IsRunning;

        public override void Reset(int newTicksPerSecond) {
            CalculateTimeThreshold(newTicksPerSecond);
            Reset();
        }
        protected override void CustomReset() { }
        void CalculateTimeThreshold(int ticksPerSecond) {
            TicksPerSecond = ticksPerSecond;
            _timeThreshold = 1f / TicksPerSecond;
        }
    }
}