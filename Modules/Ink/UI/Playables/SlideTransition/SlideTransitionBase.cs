using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    public abstract class SlideTransitionBase : ScriptableObject
    {
        /// <summary>
        /// Define how this image enters scene
        /// </summary>
        public abstract CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage,
            Action onFinish);

        protected async UniTask Fade(Image image, bool isFadeIn, Color targetColor, float time, CancellationToken token,
            Action onFinish)
        {
            if (!image || time <= 0)
            {
                onFinish?.Invoke();
                return;
            }
            float startAlpha = isFadeIn ? 0 : 1;

            await UniTaskUtil.Tween(t => 
                    image.color = targetColor.SetAlpha(isFadeIn 
                        ? math.min(1, t / time) : math.max(0, startAlpha - t / time)),
                time, token);

            image.color = targetColor.SetAlpha(isFadeIn ? 1 : 0);
            onFinish?.Invoke();
        }
        
        protected enum Direction
        {
            LEFT_TO_RIGHT,
            RIGHT_TO_LEFT,
            TOP_TO_BOTTOM,
            BOTTOM_TO_TOP
        }
    }
}