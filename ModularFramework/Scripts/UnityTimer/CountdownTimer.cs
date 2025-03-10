using UnityEngine;
// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    /// <summary>
    /// Timer that counts down from a specific value to zero.
    /// </summary>
    public class CountdownTimer : Timer {
        public CountdownTimer(float value) : base(value) { }
        public float DeltaTime {get; protected set;}
        public override void Tick() {
            if (!IsRunning) return;
            if(CurrentTime > 0) {
                CurrentTime -= Time.deltaTime;
                DeltaTime += Time.deltaTime;
            }
            base.Tick();
            if (CurrentTime <= 0) {
                Stop();
            }
        }

        public override bool IsFinished => CurrentTime <= 0;

        public override void Reset()
        {
            base.Reset();
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