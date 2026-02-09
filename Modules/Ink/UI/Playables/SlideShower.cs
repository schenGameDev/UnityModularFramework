using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

public class SlideShower : PlayableGroup
{
    // manually fade in/out each slide
    // Play next profile without End last profile: cross-fade, using next profile inTime as reference
    [SerializeField] private SlideShowProfile[] profiles;
    [SerializeField] private Image frontImage;
    [SerializeField] private Image backImage;
    
    private SlideShowProfile _lastProfile;
    private CancellationTokenSource _cts;
    private Dictionary<string, SlideShowProfile> _profiles = new ();

    private void Awake()
    {
        var defaultColor = frontImage.color.SetAlpha(1);
        if (!profiles.IsEmpty())
        {
            profiles.ForEach(p=> p.Initialize(defaultColor));
        }
        _profiles = profiles.ToDictionary(p => p.name, p => p);
        if (disableOnAwake)
        {
            SetImage(frontImage, null);
            SetImage(backImage, null);
        }
    }

    public override void Play(Action<string> callback=null, string parameter=null)
    {
        base.Play(callback, parameter);
        CurrentState = parameter.IsEmpty()? CurrentState : parameter;
        if(CurrentState ==  null) return;
        _lastProfile = _profiles[CurrentState];
        SetImage(frontImage, _lastProfile);
        DisposeToken();
        _cts = _lastProfile.Enter(frontImage, _lastProfile==null? null : backImage, DisposeToken);
        OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
    }
    
    public override void End()
    {
        if(_lastProfile == null) return;
        DisposeToken();
        SetImage(frontImage, null);
        SetImage(backImage, _lastProfile);
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

    private void DisposeToken()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
    
    public override IEnumerable<string> GetStates() => profiles.Select(p => p.name);
#if UNITY_EDITOR
    #region Editor
    [Header("Test")]
    [Rename("Index"),SerializeField] private int editorIndex = -1;
    [Button("Preview")]
    private void PreviewImage()
    {
        if(editorIndex <0 || editorIndex >= profiles.Length) return;
        SetImage(frontImage, profiles[editorIndex]);
    }
    #endregion
#endif
}

[Serializable]
public class SlideShowProfile
{
    public string name;
    public Sprite sprite;
    public Color color = Color.clear;
    [SerializeField] private SlideTransitionBase transition;

    public void Initialize(Color defaultColor)
    {
        if(color==Color.clear) color = defaultColor;
    }

    public CancellationTokenSource Enter(Image frontImage, Image backImage, Action onFinish)
    {
        return transition ? transition.Enter(this, frontImage, backImage, onFinish) : null;
    }
}

