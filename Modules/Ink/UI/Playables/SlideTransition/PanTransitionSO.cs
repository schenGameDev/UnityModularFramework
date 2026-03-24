using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "Pan_SO", menuName = "Game Module/Ink/Slide Transition/Pan")]
    public class PanTransitionSO : SlideTransitionBase
    {
        [SerializeField, Min(0)] private float duration;
        [SerializeField] private Direction direction;

        public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage,
            Action onFinish)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            frontImage.rectTransform.anchoredPosition = GetEnterPosition();
            UniTaskUtil.MoveUI(backImage.rectTransform, GetExitPosition(), duration, cts.Token)
                .ContinueWith(() => backImage.rectTransform.anchoredPosition = Vector2.zero)
                .Forget();
            UniTaskUtil.MoveUI(frontImage.rectTransform, Vector2.zero, duration, cts.Token)
                .ContinueWith(() => onFinish?.Invoke())
                .Forget();

            return cts;
        }

        private Vector2 GetEnterPosition() => direction switch
        {
            Direction.LEFT_TO_RIGHT => new Vector2(-Screen.width * 0.5f, 0),
            Direction.RIGHT_TO_LEFT => new Vector2(Screen.width * 0.5f, 0),
            Direction.TOP_TO_BOTTOM => new Vector2(0, Screen.height * 0.5f),
            Direction.BOTTOM_TO_TOP => new Vector2(0, -Screen.height * 0.5f),
            _ => Vector2.zero
        };

        private Vector2 GetExitPosition() => direction switch
        {
            Direction.LEFT_TO_RIGHT => new Vector2(Screen.width * 0.5f, 0),
            Direction.RIGHT_TO_LEFT => new Vector2(-Screen.width * 0.5f, 0),
            Direction.TOP_TO_BOTTOM => new Vector2(0, -Screen.height * 0.5f),
            Direction.BOTTOM_TO_TOP => new Vector2(0, Screen.height * 0.5f),
            _ => Vector2.zero
        };
    }
}