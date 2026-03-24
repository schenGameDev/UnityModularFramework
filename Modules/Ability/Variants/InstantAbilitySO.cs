using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    [CreateAssetMenu(fileName = "Instant Ability", menuName = "Game Module/Ability/Instant Ability")]
    public class InstantAbilitySO : AbilitySO
    {
        public InstantAbilitySO()
        {
            description = "Instantly impact one or more targets\n\n" +
                          "Can be used for melee attacks, instant spells, laser, buffs, etc.";
        }
        
        [SerializeField] private float maxRange;
        [SerializeReference, SubclassSelector] 
        public List<IEffectFactory<IDamageable>> effects = new();

        public override AimType AimMethod() => AimType.Transform;
        public override float AimRange() => maxRange;
        
        protected override void Apply(Transform me, List<IDamageable> targets, Action onComplete)
        {
            Execute(targets, me, onComplete);
        }

        private void Execute(List<IDamageable> targets, Transform me, Action onComplete)
        {
            if (targets == null) return;
            foreach (var effectFactory in effects)
            {
                var effect = effectFactory.Create();
                if (onComplete != null) effect.OnCompleted += (e) => onComplete();
                foreach (var target in targets)
                {
                    if (!effectFactory.IsTargetValid(target)) continue;
                    target.TakeEffect(effect, me);
                }

            }
        }

        public void Execute(IDamageable target, Transform me, Action onComplete)
        {
            if (target == null) return;
            foreach (var effectFactory in effects)
            {
                if (!effectFactory.IsTargetValid(target)) continue;
                var effect = effectFactory.Create();
                if (onComplete != null) effect.OnCompleted += (e) => onComplete();
                target.TakeEffect(effect, me);
            }
        }
    }
}