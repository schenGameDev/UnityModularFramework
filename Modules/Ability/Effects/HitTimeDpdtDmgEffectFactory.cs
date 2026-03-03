using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Modules.Ability;
using UnityEngine;

[Serializable]
public class HitTimeDpdtDmgEffectFactory : IEffectFactory<IDamageable>
{
    
    [Header("x(t)=baseDamage+multiplier*t^exponent")]
    
    [Min(0)] public float baseDamage;
    [Min(0)] public float multiplier;
    [Min(1)] public float exponent;
    public DamageType damageType;
    public DamageTarget damageTarget;
    
    private Dictionary<Transform, int> _lastHitTargets = new();

    public IEffect<IDamageable> Create()
    {
        Debug.LogError("Use CreateAndApply instead of Create to properly track hit times on targets.");
        return new DamageEffect
        {
            damageAmount = baseDamage,
            damageType = damageType,
            damageTarget = damageTarget
        };
    }
    
    public void CreateAndApply(List<IDamageable> targets, float tickInterval, Action onComplete = null)
    {
        List<Transform> validTargets = new();
        foreach (var target in targets)
        {
            if (!IsTargetValid(target)) continue;
            var damageAmount = GetDamageAmount(target.Transform, tickInterval);
            var effect = new DamageEffect
            {
                damageAmount = damageAmount,
                damageType = damageType,
                damageTarget = damageTarget
            };
            if (onComplete != null)
            {
                effect.OnCompleted += (e) => onComplete.Invoke();
                onComplete = null;
            }
            validTargets.Add(target.Transform);
            target.TakeEffect(effect);
        }
                
        var keys = _lastHitTargets.Keys.ToList();
        foreach (var target in keys.Where(target => !validTargets.Contains(target)))
        {
            _lastHitTargets.Remove(target);
        }
    }
    
    private float GetDamageAmount(Transform target, float tickInterval)
    {
        int hitTicks = _lastHitTargets.GetValueOrDefault(target, 0);
        
        _lastHitTargets[target] = hitTicks + 1;
        
        if (hitTicks == 0) return baseDamage;
        if (Mathf.Approximately(exponent, 1)) return multiplier * tickInterval;
        return multiplier * (Mathf.Pow(hitTicks * tickInterval, exponent) - Mathf.Pow((hitTicks - 1) * tickInterval, exponent));
    }
    
    public bool IsTargetValid(IDamageable target)
    {
        return damageTarget.HasFlag(target.TargetType);
    }
    
    public DamageTarget ApplyTarget => damageTarget;
    
    public void Reset()
    {
        _lastHitTargets.Clear();
    }
}