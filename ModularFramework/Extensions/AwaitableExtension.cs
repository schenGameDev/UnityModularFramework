using System;
using UnityEngine;
using UnityEngine.Events;

namespace ModularFramework
{
    public static class AwaitableExtension
    {
        public static Awaitable AsAwaitable(this UnityEvent unityEvent)
        {
            if(unityEvent == null) throw new ArgumentNullException(nameof(unityEvent));
            var source = new AwaitableCompletionSource();
            UnityAction handler = null;
            handler = () =>
            {
                unityEvent.RemoveListener(handler);
                source.TrySetResult();
            };
            unityEvent.AddListener(handler);
            return source.Awaitable;
        }
        
        public static Awaitable<T> AsAwaitable<T>(this UnityEvent<T> unityEvent)
        {
            if(unityEvent == null) throw new ArgumentNullException(nameof(unityEvent));
            var source = new AwaitableCompletionSource<T>();
            UnityAction<T> handler = null;
            handler = value =>
            {
                unityEvent.RemoveListener(handler);
                source.TrySetResult(value);
            };
            unityEvent.AddListener(handler);
            return source.Awaitable;
        }
        
        // Func<bool> condition = () => progress>=1f;
        // await condition.WaitUntil(pollIntervalMs:100);
        // Debug.Log("Completed");
        // Above in Start(), then increment progress in Update()
        public static Awaitable WaitUntil(this Func<bool> condition, int pollIntervalMs = 33) // 30 FPS evaluate condition every frame
        {
            if(condition == null) throw new ArgumentNullException(nameof(condition));
            if(pollIntervalMs < 0) throw new ArgumentOutOfRangeException(nameof(pollIntervalMs), "Poll interval must be >= 0");
            
            var source = new AwaitableCompletionSource();

            if (condition())
            {
                source.SetResult();
                return source.Awaitable;
            }
            
            var interval = TimeSpan.FromMilliseconds(pollIntervalMs);

            async void Poll()
            {
                while (!condition())
                {
                    await Awaitable.WaitForSecondsAsync((float)interval.TotalSeconds);
                }
                source.SetResult();
            }
            Poll();
            return source.Awaitable;
        }
    }
}