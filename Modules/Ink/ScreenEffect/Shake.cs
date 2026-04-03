using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModularFramework.Modules.Ink
{
    public class Shake : MonoBehaviour
    {
        private CancellationTokenSource _cts;
        private Vector3 _originalPosition;

        private void Awake()
        {
            _originalPosition = transform.position;
        }

        private void OnEnable()
        {
            Registry<Shake>.TryAdd(this);
        }

        private void OnDisable()
        {
            Registry<Shake>.Remove(this);
        }

        private void OnDestroy()
        {
            Registry<Shake>.Remove(this);
             try
             {
                 _cts?.Cancel();
                 _cts?.Dispose();
             }
             catch (ObjectDisposedException)
             {
                 // nothing
             }
        }


        public void Run(Vector2 positionOffsetStrength, float seconds)
        {
            
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            
            _cts = new CancellationTokenSource();
            ShakeTask(positionOffsetStrength, seconds, _cts.Token).Forget();
        }

        public void Cancel()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        async UniTaskVoid ShakeTask(Vector2 positionOffsetStrength, float duration, CancellationToken token)
        {
            float elapsed = 0f;
            float currentMagnitude = 1f;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                float x = (Random.value - 0.5f) * currentMagnitude * positionOffsetStrength.x;
                float y = (Random.value - 0.5f) * currentMagnitude * positionOffsetStrength.y;
                
                transform.position = _originalPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                currentMagnitude = (1 - (elapsed / duration)) * (1 - (elapsed / duration));

                bool cancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
                if (cancelled) break;
            }

            ResetPosition();
            _cts.Dispose();
            _cts = null;
        }

        void ResetPosition()
        {
            transform.position = _originalPosition;
        }

    }
}