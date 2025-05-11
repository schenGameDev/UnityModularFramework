using System;
using UnityEngine;

public class TimedPlayable : Playable
{
    [SerializeField] private bool skippable = false;
    [SerializeField] private bool dontDisableOnEnd = false;
    [SerializeField] protected float waitTime = -1;
    private bool _timeUp = false;
    public override void Play(Action<string> callback = null, string parameter = null) {
        base.Play(callback);
        if(waitTime>0) Invoke(nameof(End),waitTime);
    }
    
    public void EndEarly() {
        if(!skippable) return;
        End();
    }

    public override void End()
    {
        base.End();
        if(_timeUp && gameObject.activeSelf) gameObject.SetActive(false);
        _timeUp = true;
        if(!dontDisableOnEnd) gameObject.SetActive(false);
    }
}