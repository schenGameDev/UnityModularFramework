using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "Pan_SO", menuName = "Game Module/Ink/Slide Transition/Pan")]
    public class WipeTransitionSO : SlideTransitionBase
    {
        [SerializeField, Min(0)] private float duration;
        [SerializeField] private Direction direction;
        [SerializeField] private bool isDark;
        [SerializeField, ShowField(nameof(isDark))] private Vector2 darkInOutDuration;
        [SerializeField, ShowField(nameof(isDark))] private float darkDuration;
        [SerializeField, ShowField(nameof(isDark))] private Image curtainPrefab;

        private Image _curtain;

        public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage,
            Action onFinish)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            if (isDark)
            {
                DarkInOut(frontImage, onFinish, cts);
            }
            else
            {
                Wipe(frontImage, onFinish, cts);
            }
            
            return cts;
        }

        private void DarkInOut(Image frontImage, Action onFinish, CancellationTokenSource cts)
        {
            if (_curtain == null)
            {
                _curtain = Instantiate(curtainPrefab, frontImage.canvas.transform);
            }
            
            UniTaskUtil.Tween(progress => _curtain.fillAmount = progress, darkInOutDuration.x, cts.Token)
                .ContinueWith(() => UniTaskUtil.Wait(darkDuration, cts.Token))
                .ContinueWith(() => UniTaskUtil.Tween(progress => _curtain.fillAmount = 1 - progress, darkInOutDuration.y, cts.Token))
                .ContinueWith(() =>onFinish?.Invoke())
                .Forget();
        }
        
        private void Wipe(Image frontImage, Action onFinish, CancellationTokenSource cts) 
        {
            frontImage.rectTransform.anchoredPosition = GetEnterPosition();
            UniTaskUtil.MoveUI(frontImage.rectTransform, Vector2.zero, duration, cts.Token)
                .ContinueWith(() => onFinish?.Invoke())
                .Forget();   
        }

        private Vector2 GetEnterPosition() => direction switch
        {
            Direction.LEFT_TO_RIGHT => new Vector2(-Screen.width * 0.5f, 0),
            Direction.RIGHT_TO_LEFT => new Vector2(Screen.width * 0.5f, 0),
            Direction.TOP_TO_BOTTOM => new Vector2(0, Screen.height * 0.5f),
            Direction.BOTTOM_TO_TOP => new Vector2(0, -Screen.height * 0.5f),
            _ => Vector2.zero
        };
    }
}