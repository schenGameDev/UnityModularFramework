using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KBCore.Refs;
using ModularFramework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Marker))]
public class SpriteController : MonoBehaviour,IMark
{
    public enum TransitionStyle
    {
        HARD,
        FADE
    };
    //[SerializeField] private int index;
    [SerializeField] private float fadeTime = 0.3f;

    [SerializeField,Self(Flag.Optional)] private SpriteRenderer sr;
    [SerializeField,Self(Flag.Optional)] private Image img;
    private bool _isSpriteRenderer;

#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    
    protected void Awake() {
        _isSpriteRenderer = sr;
        SetAlpha(0);
    }

    protected void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private CancellationTokenSource _cts;
    private async UniTask Fade(bool isFadeIn, float time, CancellationToken token) {
        float t = 0;
        float startAlpha = GetAlpha();
        bool isCancelled = false;
        while(t< time && !isCancelled) 
        {
            SetAlpha(isFadeIn? math.min(1,t/time) : math.max(0,startAlpha-t/time));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        
        SetAlpha(isFadeIn? 1: 0);
    }
    
    private async UniTaskVoid FadeOutHardIn(Sprite newSprite, float time, CancellationToken token)
    {
        await Fade(false, time, token).SuppressCancellationThrow();
        UpdateSprite(newSprite);
    }
    
    private async UniTaskVoid FadeSwap(Sprite newSprite, float time, CancellationToken token) {
        float startAlpha = GetAlpha();
        bool isCancelled = false;
        if (startAlpha != 0)
        {
            isCancelled = await Fade(true, time, token).SuppressCancellationThrow();
        }
        UpdateSprite(newSprite);
        if (isCancelled)
        {
            SetAlpha(1);
        }
        else
        {
            await Fade(false, time, token);
        }
        
    }

    private void SetAlpha(float alpha)
    {
        if(_isSpriteRenderer) sr.color = sr.color.SetAlpha(alpha);
        else img.color = img.color.SetAlpha(alpha);
    }

    private float GetAlpha()
    {
        return _isSpriteRenderer? sr.color.a : img.color.a;
    }

    private void UpdateSprite(Sprite newSprite)
    {
        if(_isSpriteRenderer) sr.sprite = newSprite;
        else img.sprite =newSprite;
    }

    public void SwapImage(Sprite newSprite, TransitionStyle outStyle = TransitionStyle.FADE, TransitionStyle inStyle = TransitionStyle.FADE)
    {
        if ((!SpriteExists() || outStyle == TransitionStyle.HARD) && inStyle == TransitionStyle.HARD)
        {
            UpdateSprite(newSprite);
            SetAlpha(1);
            return;
        }
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        bool spriteExists = SpriteExists();
        if (spriteExists && outStyle == TransitionStyle.FADE && inStyle == TransitionStyle.FADE)
        {
            FadeSwap(newSprite, fadeTime, _cts.Token).Forget();
        } else if (!spriteExists || outStyle == TransitionStyle.HARD)
        {
            UpdateSprite(newSprite);
            Fade(true, fadeTime, _cts.Token).Forget();
        }
        else
        {
            FadeOutHardIn(newSprite, fadeTime, _cts.Token).Forget();
        }
    }
    
    private bool SpriteExists() => _isSpriteRenderer? sr.sprite != null : img.sprite != null;

    public void Clear(TransitionStyle outStyle = TransitionStyle.FADE)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        if (outStyle == TransitionStyle.HARD)
        {
            SetAlpha(0);
            return;
        }
        _cts = new CancellationTokenSource();
        Fade(false, fadeTime, _cts.Token).Forget();
    }
    
    #region IRegistrySO
    public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
    {
        if (alreadyRegisteredTypes.Contains(typeof(InkUIIntegrationSO))) return new ();
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Register(transform);
        return new () {typeof(InkUIIntegrationSO)};
    }

    public void UnregisterSelf()
    {
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Unregister(transform);
    }
    #endregion
}
