using System;
using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    public abstract class Timer : IDisposable {
        public float CurrentTime { get; protected set; }
        public int CurrentFrameCount { get; protected set; }

        public bool IsRunning { get; private set; }

        protected enum Mode {FRAME, TIME}
        protected Mode mode;

        protected float initialTime;
        protected int initialFrameCount;

        public float Progress => Mathf.Clamp(mode == Mode.TIME? CurrentTime / initialTime : CurrentFrameCount / initialFrameCount, 0, 1);

        public Action OnTimerStart = delegate { };
        public Action OnTick = delegate { };
        public Action OnTimerStop = delegate { };

        protected Timer(float time) {
            initialTime = time;
            mode = Mode.TIME;
        }

        protected Timer(int frame) {
            initialFrameCount = frame;
            mode = Mode.FRAME;
        }

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

        public virtual void Tick() {
            if (IsRunning) OnTick.Invoke();
        }

        public abstract bool IsFinished { get; }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public virtual void Reset() {
            if(mode == Mode.TIME) CurrentTime = initialTime;
            else CurrentFrameCount = initialFrameCount;
        }

        public virtual void Reset(float newTime) {
            initialTime = newTime;
            Reset();
        }

        public virtual void Reset(int newFrameCount) {
            initialFrameCount = newFrameCount;
            Reset();
        }

        bool disposed;

        ~Timer() {
            Dispose(false);
        }

        // Call Dispose to ensure deregistration of the timer from the TimerManager
        // when the consumer is done with the timer or being destroyed
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposed) return;

            if (disposing) {
                TimerManager.DeregisterTimer(this);
            }

            disposed = true;
        }
    }
}