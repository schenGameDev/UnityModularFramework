using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "Fade_SO", menuName = "Game Module/Ink/Slide Transition/Fade")]
    public class FadeTransitionSO : SlideTransitionBase
    {
        [SerializeField, Min(0)] private float outTime;
        [SerializeField, Min(0)] private float intervalTime;
        [SerializeField, Min(0)] private float inTime;

        public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage,
            Action onFinish)
        {
            if (outTime <= 0 && intervalTime <= 0 && inTime <= 0) return null;
            CancellationTokenSource cts = new CancellationTokenSource();

            frontImage.color = frontImage.color.SetAlpha(0);

            Fade(backImage, false, profile.color, outTime, cts.Token, null)
                .ContinueWith(() => Wait(frontImage, intervalTime, cts.Token, null))
                .ContinueWith(() => Fade(frontImage, true, profile.color, inTime, cts.Token, onFinish))
                .Forget();
            return cts;
        }
        
        private async UniTask Wait(Image image, float time, CancellationToken token, Action onFinish)
        {
            if (!image)
            {
                onFinish?.Invoke();
                return;
            }

            await UniTask.WaitForSeconds(time, cancellationToken: token).SuppressCancellationThrow();

            image.color = image.color.SetAlpha(1);
            onFinish?.Invoke();
        }
    }
}