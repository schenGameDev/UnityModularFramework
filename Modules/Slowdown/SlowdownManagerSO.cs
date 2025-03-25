using System;
using UnityEngine;
using UnityEngine.Events;
using ModularFramework;

[CreateAssetMenu(fileName ="SlowdownManager_SO",menuName ="SO/SlowdownManager")]
public class SlowdownManagerSO : GameModule, ILive
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

    protected override void Reset() {
        base.Reset();
        CurrentSpeedModifier = 1;
        _endTime = 0;
        NPCSpeedChangeEvent.Raise(1);
    }

    public override void OnUpdate(float deltaTime)
    {
        if(!Live) return;

        if(_endTime>0 && Time.time >= _endTime) {
            Reset();
        }
    }
}