using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ModularFramework.Utility;

[CreateAssetMenu(fileName = "Pan_SO", menuName = "Game Module/Ink/Slide Transition/Pan")]
public class PanTransitionSO : SlideTransitionBase
{
    enum Direction
    {
        LEFT_TO_RIGHT, RIGHT_TO_LEFT, TOP_TO_BOTTOM, BOTTOM_TO_TOP
    }

    [SerializeField] private float duration;
    [SerializeField] private Direction direction;
    public override CancellationTokenSource Enter(SlideShowProfile profile, Image frontImage, Image backImage, Action onFinish)
    {
        CancellationTokenSource cts = null;       
        if (duration > 0)
        {
            cts = new CancellationTokenSource();
            frontImage.rectTransform.anchoredPosition = GetEnterPosition();
            PhysicsUtil.MoveUI(backImage.rectTransform, GetExitPosition(), duration,cts.Token)
                .ContinueWith(() => backImage.rectTransform.anchoredPosition=Vector2.zero)
                .Forget();
            PhysicsUtil.MoveUI(backImage.rectTransform, Vector2.zero, duration,cts.Token)
                .ContinueWith(() => frontImage.rectTransform.anchoredPosition=Vector2.zero)
                .Forget();
            
        }
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