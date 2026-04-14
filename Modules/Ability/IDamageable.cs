using System;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    public interface IDamageable
    {
        public void TakeDamage(float damageAmount, DamageType damageType, Transform source);
        
        public void KnockBack(Vector3 knockBackDirection, float duration, float knockbackDistance, Action onComplete);

        public EffectResolver EffectResolver { get; }

        public void AimedAtBy(bool isAiming, Transform attacker, string details = null);

        public Transform Transform { get; }

        public DamageTarget TargetType { get; }
    }
}