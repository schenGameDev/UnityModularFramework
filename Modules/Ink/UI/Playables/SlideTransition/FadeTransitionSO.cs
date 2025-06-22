using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Fade_SO", menuName = "Game Module/Ink/Slide Transition/Fade")]
public class FadeTransitionSO : SlideTransitionBase
{
    [SerializeField] private float outTime, intervalTime, inTime;

    public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage, Action onFinish)
    {
        if (outTime <= 0 && intervalTime <= 0 && inTime <= 0) return null;
        CancellationTokenSource cts = new CancellationTokenSource();
        
        frontImage.color = frontImage.color.SetAlpha(0);
        
        Fade(backImage,false, profile.color, outTime, cts.Token, null)
            .ContinueWith(()=>Wait(frontImage, intervalTime, cts.Token, null))
            .ContinueWith(()=>Fade(frontImage,true, profile.color, inTime, cts.Token, onFinish))
            .Forget();
        return cts;
    }
}