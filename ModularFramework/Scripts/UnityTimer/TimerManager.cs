using System.Collections.Generic;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    public static class TimerManager {
        static readonly List<Timer> timers = new();
        static readonly List<Timer> sweep = new();

        public static void RegisterTimer(Timer timer) => timers.Add(timer);
        public static void DeregisterTimer(Timer timer) => timers.Remove(timer);

        public static void UpdateTimers() {
            if (timers.Count == 0) return;

            sweep.ReplaceWith(timers);
            foreach (var timer in sweep) {
                timer.Tick();
            }
        }

        public static void Clear() {
            sweep.ReplaceWith(timers);
            foreach (var timer in sweep) {
                timer.Dispose();
            }

            timers.Clear();
            sweep.Clear();
        }
    }
}