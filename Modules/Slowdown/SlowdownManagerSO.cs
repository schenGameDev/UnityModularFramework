using System;
using UnityEngine;
using UnityEngine.Events;
using ModularFramework;

[CreateAssetMenu(fileName ="SlowdownManager_SO",menuName ="Game Module/Slowdown")]
public class SlowdownManagerSO : GameModule<SlowdownManagerSO>, ILive
{
    [Header("Config")]
    public EventChannel<float> NPCSpeedChangeEvent;
    public float TimeFreezeTime;

    public bool Live { get => _live; set => _live = Live; }

    [SerializeField] private bool _live;

    [Range(0,2)] public float CurrentSpeedModifier = 1;
    public float TimeFreezeSpeedModifier = 0.1f;
    private float _endTime = 0;

    public SlowdownManagerSO() {
        updateMode = UpdateMode.EVERY_N_FRAME;
    }

    protected override void OnAwake()
    {
        Reset();
    }

    protected override void OnStart() { }

    protected override void OnUpdate()
    {
        if(!Live) return;

        if(_endTime>0 && Time.time >= _endTime) {
            Reset();
        }
    }
    
    protected override void OnDestroy() { }
    protected override void OnDraw() { }

    public void TimeFreeze(float time) {
        SlowDown(0, time);
    }

    public void SlowDown(float time) {
        CurrentSpeedModifier = TimeFreezeSpeedModifier;
        _endTime = Time.time + time;
        NPCSpeedChangeEvent.Raise(CurrentSpeedModifier);
    }

    public void SlowDown(float modifier, float time) {
        CurrentSpeedModifier = modifier;
        _endTime = Time.time + time;
        NPCSpeedChangeEvent.Raise(CurrentSpeedModifier);
    }

    private void Reset() {
        CurrentSpeedModifier = 1;
        _endTime = 0;
        NPCSpeedChangeEvent.Raise(1);
    }
}