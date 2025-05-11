using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class SpriteFadeOut : SpriteBehaviour
{
    [SerializeField] private float fadeTime = 0.4f;

    private CancellationTokenSource _cts;
    public void FadeOut()
    {
        SetAlpha(1);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        FadeTask(false, fadeTime, _cts.Token).Forget();
    }

}
