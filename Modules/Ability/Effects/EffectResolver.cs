using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.Ability
{
    public class EffectResolver : IResetable
    {
        public Action<SpecialCondition,bool> onSpecialConditionChanged;
        
        private Autowire<SpecialConditionSystem> _specialConditionManager = new ();
        private readonly IDamageable _me;
        private readonly float _mass;
        private readonly List<IEffect<IDamageable>> _activeEffects = new();
        private readonly Dictionary<SpecialCondition, LimitedRepeatTimer> _specialConditionTimers = new();
        
        public EffectResolver(IDamageable me, float mass)
        {
            _me = me;
            _mass = mass;
        }

        public void ResetState()
        {
            foreach (var effect in _activeEffects)
            {
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel();
            }
            _activeEffects.Clear();


            foreach (var timer in _specialConditionTimers.Values)
            {
                timer.Dispose();
            }
            _specialConditionTimers.Clear();
        }

        public void TakeEffect(IEffect<IDamageable> effect, Transform source)
        {
            effect.OnCompleted += RemoveEffect;
            _activeEffects.Add(effect);
            if (effect is KnockBackEffect knockBackEffect)
            {
                var knockbackDirection = _me.Transform.position - source.position;
                knockbackDirection.y = 0; // knockback only in horizontal direction
                var knockbackDistance = Mathf.Max(0,
                    knockBackEffect.speed * knockBackEffect.duration + knockBackEffect.massModifier * _mass);
                _me.KnockBack(knockbackDirection, knockBackEffect.duration, knockbackDistance, () => RemoveEffect(effect));
            }
            else
            {
                effect.Apply(_me, source);
            }
        }
        
        private void RemoveEffect(IEffect<IDamageable> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            _activeEffects.Remove(effect);
        }

        public void TakeSpecialCondition(SpecialCondition specialCondition, float duration, Transform source)
        {
            if(_specialConditionTimers.TryGetValue(specialCondition, out var timer))
            {
                var extendedTime = duration - timer.RemainingTime;
                if (extendedTime > 0)
                {
                    timer.Extend(extendedTime);
                }
            }
            else
            {
                var newTimer = _specialConditionManager.Get().GetTimer(_me,specialCondition, duration);
                newTimer.Start();
                _specialConditionTimers.Add(specialCondition, newTimer);
                onSpecialConditionChanged?.Invoke(specialCondition, true);
            }
        }

        public void RemoveSpecialCondition(SpecialCondition specialCondition)
        {
            if (!_specialConditionTimers.TryGetValue(specialCondition, out var timer)) return;
            timer?.Dispose();
            _specialConditionTimers.Remove(specialCondition);
            onSpecialConditionChanged?.Invoke(specialCondition, false);
        }
    }
}