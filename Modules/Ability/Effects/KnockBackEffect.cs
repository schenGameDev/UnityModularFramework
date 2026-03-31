using System;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    [Serializable]
    public struct KnockBackEffect : IEffect<IDamageable>
    {
        public float duration;
        public float speed;
        public float massModifier;
        public DamageTarget damageTarget;

        public DamageTarget ApplyTarget => damageTarget;

        public event Action<IEffect<IDamageable>> OnCompleted;

        public KnockBackEffect(DamageTarget damageTarget, float duration, float speed, float massModifier)
        {
            this.damageTarget = damageTarget;
            this.duration = duration;
            this.speed = speed;
            this.massModifier = massModifier;
            OnCompleted = null;
        }

        public void Apply(IDamageable target, Transform source)
        {
            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            OnCompleted?.Invoke(this);
        }

        public float GetKnockbackDistance(float targetMass)
        {
            return Mathf.Max(0, duration * speed - targetMass * massModifier);
        }
    }

    [Serializable]
    public class KnockBackEffectFactory : IEffectFactory<IDamageable>
    {
        [Header("distance=speed*duration-m*massModifier")]
        [Min(0)] public float duration;
        [Min(0)] public float speed;
        [Min(0)] public float massModifier;
        public DamageTarget damageTarget;

        public IEffect<IDamageable> Create()
        {
            return new KnockBackEffect
            {
                duration = duration,
                speed = speed,
                massModifier = massModifier,
                damageTarget = damageTarget
            };
        }

        public bool IsTargetValid(IDamageable target)
        {
            return damageTarget.HasFlag(target.TargetType);
        }

        public DamageTarget ApplyTarget => damageTarget;
    }
}