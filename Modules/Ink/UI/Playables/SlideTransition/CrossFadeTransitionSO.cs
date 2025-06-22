using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "CrossFade_SO", menuName = "Game Module/Ink/Slide Transition/Cross Fade")]
public class CrossFadeTransitionSO : SlideTransitionBase
{
    [SerializeField] private float duration;
    public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage, Action onFinish)
    {
        CancellationTokenSource cts = null;       
        if (duration > 0)
        {
            cts = new CancellationTokenSource();
            Fade(frontImage,true, profile.color,duration, cts.Token, onFinish).Forget();
        }
        return cts;
    }
    
}