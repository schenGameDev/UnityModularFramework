using System;
using EditorAttributes;
using UnityEngine;
using UnityTimer;

[Serializable]
public struct SpecialConditionEffect : IEffect<IDamageable>
{
    public SpecialCondition specialCondition;
    public event Action<IEffect<IDamageable>> OnCompleted;
    public DamageTarget ApplyTarget => DamageTarget.All;
    private CountdownTimer _timer;
    private IDamageable _target;

    public SpecialConditionEffect(SpecialCondition specialCondition, float duration)
    {
        this.specialCondition = specialCondition;
        _timer = new CountdownTimer(duration);
        _target = null;
        OnCompleted = null;
    }

    public void Apply(IDamageable target)
    {
        _target = target;
        var sc = this;
        _timer.OnTimerStart += () => target.TakeSpecialCondition(sc.specialCondition);
        _timer.OnTimerStop += () =>
        {
            target.RemoveSpecialCondition(sc.specialCondition);
            sc.CleanUp();
        };
        _timer.Start();
    }


    public void Cancel()
    {
        _timer.Stop();
        _target.RemoveSpecialCondition(specialCondition);
        CleanUp();
    }
    
    public bool IsTargetValid(IDamageable target)
    {
        return true;
    }

    public void CleanUp()
    {
        _target = null;
        OnCompleted?.Invoke(this);
    }
}

[Serializable]
public class SpecialConditionEffectFactory : IEffectFactory<IDamageable>
{
    [Min(0), Suffix("s")] public float duration;
    public SpecialCondition specialCondition;

    public IEffect<IDamageable> Create()
    {
        return new SpecialConditionEffect(specialCondition, duration);
    }
}

public enum SpecialCondition
{
    Stunned,
    Chaosed,
    Silenced
}