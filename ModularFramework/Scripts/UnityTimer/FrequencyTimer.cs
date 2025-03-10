using System;
using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    /// <summary>
    /// Timer that ticks at a specific frequency. (N times per second)
    /// </summary>
    public class FrequencyTimer : Timer {
        public int TicksPerSecond { get; private set; }
        float timeThreshold;

        public FrequencyTimer(int ticksPerSecond) : base(0f) {
            CalculateTimeThreshold(ticksPerSecond);
        }

        public override void Tick() {
            if (!IsRunning) return;
            if (CurrentTime >= timeThreshold) {
                CurrentTime -= timeThreshold;
            }
            base.Tick();
            if (CurrentTime < timeThreshold) {
                CurrentTime += Time.deltaTime;
            }
        }

        public override bool IsFinished => !IsRunning;

        public override void Reset(int newTicksPerSecond) {
            CalculateTimeThreshold(newTicksPerSecond);
            Reset();
        }

        void CalculateTimeThreshold(int ticksPerSecond) {
            TicksPerSecond = ticksPerSecond;
            timeThreshold = 1f / TicksPerSecond;
        }
    }
}