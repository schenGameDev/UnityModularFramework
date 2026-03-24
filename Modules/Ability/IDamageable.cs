using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    public interface IDamageable
    {
        public void TakeDamage(float damageAmount, DamageType damageType, Transform source);

        public void TakeEffect(IEffect<IDamageable> effect, Transform source);

        public void TakeSpecialCondition(SpecialCondition specialCondition, Transform source);

        public void RemoveSpecialCondition(SpecialCondition specialCondition);

        public void AimedAtBy(bool isAiming, Transform attacker, string details = null);

        public Transform Transform { get; }

        public DamageTarget TargetType { get; }
    }
}