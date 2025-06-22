using System;
using System.Threading;
using EditorAttributes;
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
        var defaultColor = frontImage.color.SetAlpha(1);
        if (!profiles.IsEmpty())
        {
            profiles.ForEach(p=> p.Initialize(defaultColor));
        }
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
        
        _lastProfile = profiles[_index++];
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
}

[Serializable]
public class SlideShowProfile
{
    public Sprite sprite;
    public Color color = Color.clear;
    [SerializeReference] private SlideTransitionBase transition;

    public void Initialize(Color defaultColor)
    {
        if(color==Color.clear) color = defaultColor;
    }

    public CancellationTokenSource Enter(Image frontImage, Image backImage, Action onFinish)
    {
        return transition ? transition.Enter(this, frontImage, backImage, onFinish) : null;
    }
}

