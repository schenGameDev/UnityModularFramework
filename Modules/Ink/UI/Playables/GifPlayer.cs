using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework;
using UnityEngine;
using UnityEngine.UI;

public class GifPlayer : Playable,IResetable
{
    [SerializeField] private Sprite[] frames;
    [SerializeField,Suffix("s")] private float interval = 0.2f;
    [SerializeField] private bool loop;
    private Image _image;
    private int _index;
    private CancellationTokenSource _cts;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        if(!disableOnAwake) Play();
    }

    public override void Play(Action<string> callback=null, string parameter=null)
    {
        base.Play(callback, parameter);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        PlayGif(_cts.Token).Forget();
    }
    
    public override void End()
    {
        _cts?.Cancel();
    }

    private async UniTaskVoid PlayGif(CancellationToken token)
    {
        float t = 0;
        _image.sprite = frames[_index++];
        if (loop) OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
        
        do
        {
            if (t >= interval)
            {
                t = 0;
                _image.sprite = frames[_index++];
                if (loop && _index >= frames.Length) _index = 0;
            }
            
            t+=Time.deltaTime;
            bool isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
            if(isCancelled) break;
        } while (loop || _index < frames.Length);
        
        _cts?.Dispose();
        _cts = null;
        if (!loop) OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
        if (TryGetComponent<SpriteFadeOut>(out var fadeOut))
        {
            fadeOut.FadeOut();
        }
        else
        {
            gameObject.SetActive(false);
        }
        ResetState();
    }
    #region IResetable
    public void ResetState()
    {
        _index = 0;
    }
    #endregion
    
    #region ISavable
    public override void Load()
    {
        _index = 0;
        base.Load();
    } 
    #endregion
}