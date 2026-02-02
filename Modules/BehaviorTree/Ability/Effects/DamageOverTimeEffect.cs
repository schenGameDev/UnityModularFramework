using System;
using EditorAttributes;
using UnityEngine;
using UnityTimer;

[Serializable]
public struct DamageOverTimeEffect : IEffect<IDamageable>
{
    public int damagePerTick;
    public DamageType damageType;
    public DamageTarget damageTarget;
    public DamageTarget ApplyTarget => damageTarget;
    public event Action<IEffect<IDamageable>> OnCompleted;

    private RepeatCountdownTimer _timer;
    private IDamageable _target;

    public DamageOverTimeEffect(DamageType damageType, DamageTarget damageTarget,int ticks, float tickInterval, int damagePerTick)
    {
        this.damagePerTick = damagePerTick;
        this.damageType = damageType;
        _timer = new RepeatCountdownTimer(tickInterval, ticks);
        this.damageTarget = damageTarget;
        _target = null;
        OnCompleted = null;
    }

    public void Apply(IDamageable target)
    {
        _target = target;
        _timer.OnTick += OnTick;
        _timer.OnTimerStop += CleanUp;
        _timer.Start();
    }

    private void OnTick()
    {
        _target.TakeDamage(damagePerTick, damageType);
    }

    public void Cancel()
    {
        _timer.Stop();
        CleanUp();
    }
    
    public bool IsTargetValid(IDamageable target)
    {
        return damageTarget.HasFlag(target.DamageTarget);
    }

    private void CleanUp()
    {
        _target = null;
        OnCompleted?.Invoke(this);
    }
}

[Serializable]
public class DamageOverTimeEffectFactory : IEffectFactory<IDamageable>
{
    [Min(0)] public int ticks;
    [Min(0), Suffix("s")] public float tickInterval;
    [Min(0)] public int damagePerTick;
    public DamageType damageType;
    public DamageTarget damageTarget;

    public IEffect<IDamageable> Create()
    {
        return new DamageOverTimeEffect(damageType, damageTarget, ticks, tickInterval, damagePerTick);
    }
}

[Serializable]
public class HealOverTimeEffectFactory : IEffectFactory<IDamageable>
{
    public DamageTarget healTarget = DamageTarget.All;
    public int ticks;
    public float tickInterval;
    [Min(0)] public int healPerTick;

    public IEffect<IDamageable> Create()
    {
        return new DamageOverTimeEffect(DamageType.Physical, healTarget, ticks, tickInterval, - healPerTick);
    }
}