using System;
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
            if (CurrentTime >= _timeThreshold) {
                CurrentTime -= _timeThreshold;
            }
            base.Tick();
            if (CurrentTime < _timeThreshold) {
                CurrentTime += Time.deltaTime;
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