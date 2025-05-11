using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class SlideShower : Playable
{
    // manually fade in/out each slide
    // Play next profile without End last profile: cross-fade, using next profile inTime as reference
    [SerializeField] private SlideShowProfile[] profiles;
    [SerializeField] private Image frontImage;
    [SerializeField] private Image backImage;
    
    private SlideShowProfile _lastProfile;
    private int _index = 0;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (disableOnAwake)
        {
            SetImage(frontImage, null);
            SetImage(backImage, null);
        }
    }

    public override void Play(Action<string> callback=null, string parameter=null)
    {
        base.Play(callback, parameter);
        _index = parameter.IsEmpty()? _index : int.Parse(parameter);
        
        SetImage(backImage, _lastProfile);
        
        _lastProfile = profiles[_index];

        _index += 1;
        SetImage(frontImage, _lastProfile);
        if (_lastProfile.inTime > 0)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _cts = new CancellationTokenSource();
            Fade(frontImage,true, _lastProfile.color,_lastProfile.inTime, _cts.Token).Forget();
        }
        OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
    }
    
    public override void End()
    {
        if(_lastProfile == null) return;
        SetImage(frontImage, null);
        SetImage(backImage, _lastProfile);
        if (_lastProfile.outTime > 0)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _cts = new CancellationTokenSource();
            Fade(backImage,false, _lastProfile.color, _lastProfile.outTime, _cts.Token).Forget();
        } 
        _lastProfile = null;
    }
    
    private void SetImage(Image image, SlideShowProfile profile)
    {
        if (profile == null)
        {
            image.enabled = false;
        } else if (profile.sprite)
        {
            image.sprite = profile.sprite;
            image.enabled = true;
        } else if (profile.color.a != 0)
        {
            image.enabled = true;
            image.color = profile.color;
        } else
        {
            image.enabled = false;
        }
    }
    
    private async UniTask Fade(Image image, bool isFadeIn, Color targetColor, float time, CancellationToken token) {
        float t = 0;
        float startAlpha = isFadeIn ? 0 : 1;
        bool isCancelled = false;
        while(t< time && !isCancelled) 
        {
            image.color = targetColor.SetAlpha(isFadeIn? math.min(1,t/time) : math.max(0,startAlpha-t/time));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
            if(isCancelled) break;
        }
        
        image.color = targetColor.SetAlpha(isFadeIn? 1: 0);
    }
}

[Serializable]
public class SlideShowProfile
{
    public Sprite sprite;
    public Color color = Color.white;
    public float inTime; // time to fade in, 0 means immediately appear
    public float outTime;
}