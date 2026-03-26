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
            if(mode == Mode.TIME) currentTime = initialTime;
            else CurrentFrameCount = initialFrameCount;
            ((T)this).CustomReset();
        }

        protected abstract bool FinishCondition();
        protected abstract void CustomTick();
        protected abstract void CustomReset();

    }
    public abstract class Timer : IDisposable {
        public int CurrentFrameCount { get; protected set; }

        public bool IsRunning { get; private set; }
        public abstract bool IsFinished { get; }

        protected enum Mode {FRAME, TIME}
        protected Mode mode;

        protected float currentTime;
        protected float initialTime;
        protected int initialFrameCount;
        protected uint delayStartScheduled;

        public float Progress => 1 - Mathf.Clamp(mode == Mode.TIME? currentTime / initialTime : 1f * CurrentFrameCount / initialFrameCount, 0, 1);
        public float RemainingTime => currentTime;
        public float PassedTime => initialTime - currentTime;
        
        public Action OnTimerStart = delegate { };
        public Action OnTick = delegate { };
        public Action OnTimerStop = delegate { };
        

        /// <summary>
        /// Starts the timer from its initial time/frame count. Resets the timer, invokes the OnTimerStart event if it was not running previously.
        /// </summary>
        public void Start() {
            Reset();
            delayStartScheduled = 0;
            if (!IsRunning) {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                OnTimerStart.Invoke();
            }
        }
        
        /// <summary>
        /// Restarts the timer. If the timer is already running, resets the timer to its initial time/frame count and invokes the OnTimerStart event.
        /// If not running, starts the timer.
        /// </summary>
        public void Restart()
        {
            if (IsRunning)
            {
                Reset();
                delayStartScheduled = 0;
                OnTimerStart.Invoke();
            }
            else
            {
                Start();
            }
        }
        
        /// <summary>
        /// Schedules the timer to start after the specified delay in seconds.
        /// </summary>
        /// <param name="delay">The delay in seconds before the timer starts.</param>
        public void DelayStart(float delay)
        {
            delayStartScheduled = TimerManager.Schedule(delay, Start);
        }

        /// <summary>
        /// Stops the timer. If running, and invokes OnTimerStop. If a delayed start is scheduled, cancels it.
        /// </summary>
        public void Stop() {
            if (IsRunning) {
                IsRunning = false;
                TimerManager.DeregisterTimer(this);
                OnTimerStop.Invoke();
            }
            else if (delayStartScheduled > 0)
            {
                TimerManager.RemoveSchedule(delayStartScheduled);
                delayStartScheduled = 0;
            }
            else
            {
                TimerManager.DeregisterTimer(this);
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