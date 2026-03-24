using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ModularFramework.Utility
{
    public static class UniTaskUtil
    {
        /// <summary>
        /// Smoothly moves a Transform from its current position to a target position over a specified duration using linear interpolation.
        /// </summary>
        /// <param name="tf">The Transform to move</param>
        /// <param name="target">The target world position to move to</param>
        /// <param name="seconds">Duration in seconds for the movement</param>
        /// <param name="token">Cancellation token to cancel the operation</param>
        public static async UniTask Move(Transform tf, Vector3 target, float seconds, CancellationToken token) {
            var currentPos = tf.position;
            await Tween(t => tf.position = Vector3.Lerp(currentPos, target, t),
                seconds, token);
            tf.position = target;
        }
		
        /// <summary>
        /// Smoothly moves a RectTransform's anchored position to a target position over a specified duration using linear interpolation.
        /// </summary>
        /// <param name="tf">The RectTransform to move</param>
        /// <param name="targetAnchor">The target anchored position to move to</param>
        /// <param name="seconds">Duration in seconds for the movement</param>
        /// <param name="token">Cancellation token to cancel the operation</param>
        public static async UniTask MoveUI(RectTransform tf, Vector2 targetAnchor, float seconds, CancellationToken token) {
            var currentPos = tf.anchoredPosition;
            await Tween(t => tf.anchoredPosition = Vector2.Lerp(currentPos, targetAnchor, t), 
                seconds, token);
            tf.anchoredPosition = targetAnchor;
        }
		
        /// <summary>
        /// Runs an action repeatedly over a specified duration, passing in a normalized progress value (0 to 1).
        /// </summary>
        /// <param name="task">Action that takes in a parameter for progress between 0 and 1</param>
        /// <param name="seconds">Duration in seconds to run the action over</param>
        /// <param name="token">Cancellation token to cancel the operation</param>
        public static async UniTask Tween(Action<float> task, float seconds, CancellationToken token) {
            var t = 0f;
            while(t <= 1f)
            {
                t += Time.deltaTime / seconds;
                task(t);
                await UniTask.NextFrame(cancellationToken:token);
            }
            task(1);
        }

        /// <summary>
        /// Waits for a specified duration before continuing execution, with support for cancellation.
        /// </summary>
        /// <param name="time">Time in seconds to wait</param>
        /// <param name="token">Cancellation token to cancel the wait operation</param>
        public static async UniTask Wait(float time, CancellationToken token)
        {
            await UniTask.WaitForSeconds(time, cancellationToken: token).SuppressCancellationThrow();
        }

    }
}