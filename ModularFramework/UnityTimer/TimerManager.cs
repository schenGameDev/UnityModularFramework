using System;
using System.Collections.Generic;
using ModularFramework.Utility;
using UnityEngine;

// https://github.com/adammyhre/Unity-Improved-Timers.git
namespace UnityTimer {
    public static class TimerManager {
        static readonly List<Timer> timers = new();
        static readonly List<Timer> sweep = new();

        public static void RegisterTimer(Timer timer) => timers.Add(timer);
        public static void DeregisterTimer(Timer timer) => timers.Remove(timer);

        public static void UpdateTimers() {
            Tick?.Invoke(Time.deltaTime);
            CheckScheduledActions();
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
        /// <summary>
        ///  Before Update()
        /// </summary>
        public static Action<float> Tick;
        
        static List<ScheduledAction> actionQueue = new();
        public static void Schedule(float delay, Action action)
        {
            Schedule(new ScheduledAction(delay, action));
        }
        
        public static void Schedule(ScheduledAction scheduledAction)
        {
            actionQueue.Add(scheduledAction);
        }
        
        public static void RemoveSchedule(uint id)
        {
            for (var i = 0; i < actionQueue.Count; i++)
            {
                var action = actionQueue[i];
                if (action.id != id) continue;
                actionQueue.RemoveAt(i);
                break;
            }
        }
        
        private static void CheckScheduledActions()
        {
            while (actionQueue.Count > 0)
            {
                var scheduledAction = actionQueue[0];
                if (scheduledAction.executeTime <= Time.time)
                {
                    scheduledAction.action.Invoke();
                    actionQueue.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }
    }
    
    public struct ScheduledAction
    {
        public uint id;
        public float executeTime;
        public Action action;
        
        public ScheduledAction(float delay, Action action)
        {
            id = MathUtil.GenerateUniqueId();
            this.executeTime = Time.time + delay;
            this.action = action;
        }
    }
}