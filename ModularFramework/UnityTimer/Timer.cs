using System;
using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    public abstract class Timer<T> : Timer where T : Timer<T>
    {
        protected Timer(float time) {
            initialTime = time;
            mode = Mode.TIME;
        }

        protected Timer(int frame) {
            initialFrameCount = frame;
            mode = Mode.FRAME;
        }

        public override bool IsFinished => ((T)this).FinishCondition();
        public override void Tick()
        {
            ((T)this).CustomTick();
        }

        public override void Reset()
        {
            if(mode == Mode.TIME) CurrentTime = initialTime;
            else CurrentFrameCount = initialFrameCount;
            ((T)this).CustomReset();
        }

        protected abstract bool FinishCondition();
        protected abstract void CustomTick();
        protected abstract void CustomReset();

    }
    public abstract class Timer : IDisposable {
        public float CurrentTime { get; protected set; }
        public int CurrentFrameCount { get; protected set; }

        public bool IsRunning { get; private set; }
        public abstract bool IsFinished { get; }

        protected enum Mode {FRAME, TIME}
        protected Mode mode;

        protected float initialTime;
        protected int initialFrameCount;

        public float Progress => Mathf.Clamp(mode == Mode.TIME? CurrentTime / initialTime : CurrentFrameCount / initialFrameCount, 0, 1);

        public Action OnTimerStart = delegate { };
        public Action OnTick = delegate { };
        public Action OnTimerStop = delegate { };
        

        public void Start() {
            Reset();

            if (!IsRunning) {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                OnTimerStart.Invoke();
            }
        }

        public void Stop() {
            if (IsRunning) {
                IsRunning = false;
                TimerManager.DeregisterTimer(this);
                OnTimerStop.Invoke();
            }
        }

        public abstract void Tick();

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public abstract void Reset();

        public virtual void Reset(float newTime) {
            initialTime = newTime;
            Reset();
        }

        public virtual void Reset(int newFrameCount) {
            initialFrameCount = newFrameCount;
            Reset();
        }

        ~Timer() {
            Dispose(false);
        }

        // Call Dispose to ensure deregistration of the timer from the TimerManager
        // when the consumer is done with the timer or being destroyed
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        private void Dispose(bool disposing) {
            if (_disposed) return;

            if (disposing) {
                TimerManager.DeregisterTimer(this);
            }

            _disposed = true;
        }
    }
}