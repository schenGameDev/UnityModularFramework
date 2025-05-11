using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.Events;

public class SpriteBlink : SpriteBehaviour
{
    [SerializeField] private float fadeTime = 0.3f;
    [SerializeField] private float litTime = 0.2f;
    [SerializeField] private int blinkTimes = 3;
    [SerializeField] private bool runOnAwake;
    private CancellationTokenSource _cts;
    private void OnEnable() {
        if(runOnAwake) Blink();
    }

    private void OnDisable() {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    public void Blink()
    {
        
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        BlinkTask(_cts.Token).Forget();
    }

    private async UniTaskVoid BlinkTask(CancellationToken token)
    {
        float startAlpha = GetAlpha();
        var len = blinkTimes * 2;
        
        for (int i = 0; i < len; i++)
        {
            bool isFadeIn = GetAlpha() == 0;
            if(await UniTask.WhenAll(FadeTask(isFadeIn, fadeTime, token)).SuppressCancellationThrow()) break;
            if (isFadeIn)
            {
                if(await UniTask.WaitForSeconds(litTime, cancellationToken: token).SuppressCancellationThrow()) break;
            }
        }
        SetAlpha(startAlpha);
    }
}
