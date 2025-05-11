using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpriteFadeIn : SpriteBehaviour
{
    [SerializeField] private float fadeTime = 0.4f;
    
    private CancellationTokenSource _cts;
    private void OnEnable() {
        SetAlpha(0);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        FadeTask(true, fadeTime, _cts.Token).Forget();
    }

    private void OnDisable() {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
