using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Fade_SO", menuName = "Game Module/Ink/Print Style/Fade")]
public class Fade : PrintStyleBase
{
    [Header("Config")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private CancellationTokenSource _cts;
    
    public override void OnDestroy()
    {
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

    public override void OnSkip()
    {
        _cts.Cancel();
    }

    public override void OnPrint(string text, Action callback=null)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        Prepare();
        Printer.textbox.text = GetFinalText(text);
        Printer.Done = false;
        FadeIn(callback,_cts.Token).Forget();
    }
    private async UniTaskVoid FadeIn(Action callback, CancellationToken token)
    {
        float t = 0;
        bool isCancelled = false;
        while(t< fadeInDuration && !isCancelled) 
        {
            Printer.textbox.color = Printer.textbox.color.SetAlpha(math.min(1,t/fadeInDuration));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        
        Printer.textbox.color = Printer.textbox.color.SetAlpha(1);
        
        if(displayDuration>0) await UniTask.WaitForSeconds(displayDuration, cancellationToken: token).SuppressCancellationThrow();
        if(fadeOutDuration <= 0) return;
        
        t = 0;
        while(t< fadeOutDuration && !isCancelled) 
        {
            Printer.textbox.color = Printer.textbox.color.SetAlpha(math.max(0,1-t/fadeOutDuration));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        Printer.textbox.color = Printer.textbox.color.SetAlpha(0);
        callback?.Invoke();
        Finish();
    }
}