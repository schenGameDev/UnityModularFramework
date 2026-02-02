using System;
using UnityEngine;

[Serializable]
public struct DamageEffect : IEffect<IDamageable>
{
    public int damageAmount;
    public DamageType damageType;
    public DamageTarget damageTarget;
    public DamageTarget ApplyTarget => damageTarget;
    public event Action<IEffect<IDamageable>> OnCompleted;

    public void Apply(IDamageable target)
    {
        target.TakeDamage(damageAmount, damageType);
        OnCompleted?.Invoke(this);
    }

    public void Cancel()
    {
        OnCompleted?.Invoke(this);
    }

    public bool IsTargetValid(IDamageable target)
    {
        return damageTarget.HasFlag(target.DamageTarget);
    }
}

[Serializable]
public class DamageEffectFactory : IEffectFactory<IDamageable>
{
    [Min(0)] public int damageAmount;
    public DamageType damageType;
    public DamageTarget damageTarget;

    public IEffect<IDamageable> Create()
    {
        return new DamageEffect
        {
            damageAmount = damageAmount,
            damageType = damageType,
            damageTarget = damageTarget
        };
    }
}

[Serializable]
public class HealEffectFactory : IEffectFactory<IDamageable>
{
    public DamageTarget healTarget = DamageTarget.All;
    [Min(0)] public int healAmount;

    public IEffect<IDamageable> Create()
    {
        return new DamageEffect
        {
            damageAmount =  - healAmount,
            damageType = DamageType.Physical,
            damageTarget = healTarget
        };
    }
}