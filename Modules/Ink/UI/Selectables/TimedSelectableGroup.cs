using System;
using UnityEngine;
using UnityEngine.UI;
using UnityTimer;

public class TimedSelectableGroup : SelectableGroup
{
    [SerializeField] private float time;
    
    [SerializeField] private Image progressBar;
    
    private CountdownTimer _timer;

    public override void Activate(InkChoice choiceInfo, bool showHiddenChoice)
    {
        base.Activate(choiceInfo, showHiddenChoice);
        _timer = new CountdownTimer(time);
        _timer.OnTimerStop += ()=>Select(lastIndex);
        _timer.Start();
    }

    public override void Reset()
    {
        base.Reset();
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}