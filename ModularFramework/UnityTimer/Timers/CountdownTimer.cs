using UnityEngine;
// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific value to zero.
    /// </summary>
    public class CountdownTimer : Timer<CountdownTimer> {
        public CountdownTimer(float value) : base(value) { }
        public float DeltaTime {get; protected set;}
        protected override void CustomTick() {
            if (!IsRunning) return;
            if(CurrentTime > 0) {
                CurrentTime -= Time.deltaTime;
                DeltaTime += Time.deltaTime;
            }
            OnTick.Invoke();
            if (CurrentTime <= 0) {
                Stop();
            }
        }

        protected override bool FinishCondition() => CurrentTime <= 0;

        protected override void CustomReset()
        {
            DeltaTime = 0;
        }
    }
}

// Under Start()
// timer = new CountdownTimer(spawnInterval);
//         timer.OnTimerStop += () => {
//             ...
//             timer.Start();
//         };
//         timer.Start();