using System;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    [Serializable]
    public struct SpecialConditionEffect : IEffect<IDamageable>
    {
        public SpecialCondition specialCondition;
        public event Action<IEffect<IDamageable>> OnCompleted;
        public DamageTarget damageTarget;
        public DamageTarget ApplyTarget => damageTarget;
        private float _duration;

        public SpecialConditionEffect(SpecialCondition specialCondition, float duration, DamageTarget target)
        {
            this.specialCondition = specialCondition;
            _duration = duration;
            damageTarget = target;
            OnCompleted = null;
        }

        public void Apply(IDamageable target, Transform source)
        {
            var sc = this;
            target.EffectResolver.TakeSpecialCondition(sc.specialCondition, _duration, source);
            OnCompleted?.Invoke(this); // immediately invoked
        }


        public void Cancel()
        {
            OnCompleted?.Invoke(this); // instant, can't cancel
        }
    }

    [Serializable]
    public class SpecialConditionEffectFactory : IEffectFactory<IDamageable>
    {
        [Min(0), Suffix("s")] public float duration;
        public SpecialCondition specialCondition;
        public DamageTarget damageTarget;

        public IEffect<IDamageable> Create()
        {
            return new SpecialConditionEffect(specialCondition, duration, damageTarget);
        }
        
        public bool IsTargetValid(IDamageable target)
        {
            return true;
        }
        
        public DamageTarget ApplyTarget => damageTarget;
    }

    public enum SpecialCondition
    {
        Bleed,
        Stunned,
        Chaosed,
        Silenced
    }
}