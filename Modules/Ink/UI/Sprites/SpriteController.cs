using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using ModularFramework.Utility;
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

    private SpriteRenderer _sr;
    private Image _img;
    private bool _isSpriteRenderer;

    protected void Awake() {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr) _isSpriteRenderer = true;
        else _img = GetComponent<Image>();
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
        if(_isSpriteRenderer) _sr.color = _sr.color.SetAlpha(alpha);
        else _img.color = _img.color.SetAlpha(alpha);
    }

    private float GetAlpha()
    {
        return _isSpriteRenderer? _sr.color.a : _img.color.a;
    }

    private void UpdateSprite(Sprite newSprite)
    {
        if(_isSpriteRenderer) _sr.sprite = newSprite;
        else _img.sprite =newSprite;
    }

    public void SwapImage(Sprite newSprite, TransitionStyle outStyle = TransitionStyle.FADE, TransitionStyle inStyle = TransitionStyle.FADE)
    {
        if ((!_img.sprite || outStyle == TransitionStyle.HARD) && inStyle == TransitionStyle.HARD)
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
        if (_img.sprite && outStyle == TransitionStyle.FADE && inStyle == TransitionStyle.FADE)
        {
            FadeSwap(newSprite, fadeTime, _cts.Token).Forget();
        } else if (!_img.sprite || outStyle == TransitionStyle.HARD)
        {
            UpdateSprite(newSprite);
            Fade(true, fadeTime, _cts.Token).Forget();
        }
        else if(_img.sprite)
        {
            FadeOutHardIn(newSprite, fadeTime, _cts.Token).Forget();
        }
       
    }

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
