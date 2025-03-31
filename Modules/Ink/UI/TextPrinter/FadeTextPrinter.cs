using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class FadeTextPrinter : TextPrinter
{
    [Header("Config")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private CancellationTokenSource _cts;
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public override void Skip()
    {
        _cts.Cancel();
    }

    public override void Print(string text, Action callback)
    {
        Done = false;
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        
        Textbox.text = text;
        FadeIn(callback,_cts.Token).Forget();
    }
    private async UniTaskVoid FadeIn(Action callback, CancellationToken token)
    {
        float t = 0;
        bool isCancelled = false;
        while(t< fadeInDuration && !isCancelled) 
        {
            Textbox.color.SetAlpha(math.min(1,t/fadeInDuration));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        
        Textbox.color.SetAlpha(1);
        
        if(displayDuration>0) await UniTask.WaitForSeconds(displayDuration, cancellationToken: token).SuppressCancellationThrow();
        if(fadeOutDuration <= 0) return;
        
        t = 0;
        while(t< fadeOutDuration && !isCancelled) 
        {
            Textbox.color.SetAlpha(math.max(0,1-t/fadeOutDuration));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        Textbox.color.SetAlpha(0);
        callback?.Invoke();
        Done = true;

    }
}