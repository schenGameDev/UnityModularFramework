using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "CrossFade_SO", menuName = "Game Module/Ink/Slide Transition/Cross Fade")]
    public class CrossFadeTransitionSO : SlideTransitionBase
    {
        [SerializeField, Min(0)] private float duration;

        public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage,
            Action onFinish)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Fade(frontImage, true, profile.color, duration, cts.Token, onFinish).Forget();
            return cts;
        }

    }
}