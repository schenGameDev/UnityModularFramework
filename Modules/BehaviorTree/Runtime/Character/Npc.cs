using System.Collections.Generic;
using ModularFramework.Modules.Ability;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("NPC/NPC", 0), DisallowMultipleComponent]
    public class Npc : Character, IDamageable
    {
        public int maxHealth = 500;

        [Header("Runtime")] public bool isStunned;
        public bool isFallen;
        public Transform tauntedBy;
        public float health;

        public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
        {

        }

        public Transform Transform => transform;
        public virtual DamageTarget TargetType => DamageTarget.NPC;

        protected virtual void OnEnable()
        {
            DictSetRegistry<DamageTarget, Transform>.TryAdd(TargetType, Transform);
        }

        protected virtual void OnDisable()
        {
            DictSetRegistry<DamageTarget, Transform>.Remove(TargetType, Transform);
        }

        protected virtual void OnDestroy()
        {
            DictSetRegistry<DamageTarget, Transform>.Remove(TargetType, Transform);
        }

        private void Start()
        {
            health = maxHealth;
        }



        public void FaceTarget(Vector3 targetPosition)
        {
            var faceDirection = targetPosition - transform.position;
            faceDirection.y = 0;
            transform.forward = faceDirection.normalized;
        }

        List<IEffect<IDamageable>> _activeEffects = new();

        public void TakeEffect(IEffect<IDamageable> effect, Transform source)
        {
            effect.OnCompleted += RemoveEffect;
            _activeEffects.Add(effect);
            effect.Apply(this,source);
        }

        void RemoveEffect(IEffect<IDamageable> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            _activeEffects.Remove(effect);
        }

        public void TakeSpecialCondition(SpecialCondition specialCondition, Transform source)
        {
            Debug.Log("Taking special condition: " + specialCondition);
        }

        public void RemoveSpecialCondition(SpecialCondition specialCondition)
        {
            Debug.Log("Removing special condition: " + specialCondition);
        }

        public void TakeDamage(float damageAmount, DamageType damageType, Transform source)
        {
            if (damageType == DamageType.Physical)
            {
                health -= damageAmount;
                if (health <= 0)
                {
                    Die();
                }
            }

        }

        private void Die()
        {
            Debug.Log("NPC has died.");

            foreach (var effect in _activeEffects)
            {
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel();
            }
            _activeEffects.Clear();
            Destroy(gameObject);
        }
    }
}