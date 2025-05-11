using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using UnityEngine;

/// <summary>
/// Play Gif once enabled
/// </summary>
public class SimpleGifPlayer : SpriteBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField,Suffix("s")] private float interval = 0.2f;
    [SerializeField] private bool loop;
    private int _index;
    private CancellationTokenSource _cts;

    private void OnEnable() {
        _index = 0;
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        PlayGif(_cts.Token).Forget();
    }
    
    private void OnDisable()
    {
        End();
    }
    private async UniTaskVoid PlayGif(CancellationToken token)
    {
        float t = 0;
        SetSprite(frames[_index++]);
        do
        {
            if (t >= interval)
            {
                t = 0;
                SetSprite(frames[_index++]);
                if (loop && _index >= frames.Length) _index = 0;
            }
            
            t+=Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token);
        } while (loop || _index < frames.Length);
    }

    public void End()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}