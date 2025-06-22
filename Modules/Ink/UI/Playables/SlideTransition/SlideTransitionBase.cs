
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public abstract class SlideTransitionBase : ScriptableObject
{
    /// <summary>
    /// Define how this image enters scene
    /// </summary>
    public abstract CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage, Action onFinish);
    
    protected async UniTask Fade(Image image, bool isFadeIn, Color targetColor, float time, CancellationToken token, Action onFinish) {
        if (!image || time <= 0)
        {
            onFinish?.Invoke();
            return;
        }
        
        float t = 0;
        float startAlpha = isFadeIn ? 0 : 1;
  
        while(t< time) 
        {
            image.color = targetColor.SetAlpha(isFadeIn? math.min(1,t/time) : math.max(0,startAlpha-t/time));
            t+=Time.deltaTime;
            bool isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
            if(isCancelled) break;
        }
    
        image.color = targetColor.SetAlpha(isFadeIn? 1: 0);
        onFinish?.Invoke();
    }
    
    protected async UniTask Wait(Image image, float time, CancellationToken token, Action onFinish) {
        if (!image)
        {
            onFinish?.Invoke();
            return;
        }
        
        await UniTask.WaitForSeconds(time, cancellationToken: token).SuppressCancellationThrow();

        image.color = image.color.SetAlpha(1);
        onFinish?.Invoke();
    }
}