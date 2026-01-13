using System;
using UnityEngine;

[Serializable]
public struct DamageEffect : IEffect<IDamageable>
{
    public int damageAmount;
    public DamageType damageType;
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
}

[Serializable]
public class DamageEffectFactory : IEffectFactory<IDamageable>
{
    [Min(0)] public int damageAmount;
    public DamageType damageType;

    public IEffect<IDamageable> Create()
    {
        return new DamageEffect
        {
            damageAmount = damageAmount,
            damageType = damageType
        };
    }
}

[Serializable]
public class HealEffectFactory : IEffectFactory<IDamageable>
{
    [Min(0)] public int healAmount;

    public IEffect<IDamageable> Create()
    {
        return new DamageEffect
        {
            damageAmount =  - healAmount,
            damageType = DamageType.Physical
        };
    }
}