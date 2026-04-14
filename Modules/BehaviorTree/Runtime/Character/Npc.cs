using System;
using KBCore.Refs;
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
        private EffectResolver _effectResolver;
        private LayerMask _groundLayer;
        [Self,SerializeField] private BTAbility[] abilities;
        [Self,SerializeField] private AstarAI astarAI;
        
#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
        {

        }

        public Transform Transform => transform;
        public virtual DamageTarget TargetType => DamageTarget.NPC;


        private void Awake()
        {
            _effectResolver = new EffectResolver(this, 1);
            _effectResolver.onSpecialConditionChanged += ShowSpecialCondition;
            _groundLayer = LayerMask.GetMask(EnvironmentConstants.LAYER_GROUND);
        }

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

        #region Knock Back

        private float _knockBackTimer;
        private bool IsKnockBacking => _knockBackTimer > 0;
        private Action _knockBackOnComplete;
        public void KnockBack(Vector3 direction, float duration, float distance, Action onComplete)
        {
            direction.Normalize();
            astarAI.AddExternalVelocity(direction * (distance / duration));
            _knockBackTimer = duration;
            _knockBackOnComplete = onComplete;
            foreach (var ability in abilities)
            {
                ability.Interrupt();
            }
        }

        public void KnockBackComplete()
        {
            _knockBackOnComplete?.Invoke();
            _knockBackOnComplete = null;
            _knockBackTimer = 0;
            astarAI.AddExternalVelocity(Vector3.zero);
        }
        private void OnCollisionEnter(Collision collision) // object hit during movement, not including other physics impact
        {
            if(collision.gameObject.IsInLayer(_groundLayer)) return;
            if (IsKnockBacking) KnockBackComplete();
        }
        private void FixedUpdate()
        {
            if (IsKnockBacking)
            {
                _knockBackTimer -= Time.fixedDeltaTime;
                if (_knockBackTimer <= 0)
                {
                    KnockBackComplete();
                }
            }
        }

        #endregion
        
        private void ShowSpecialCondition(SpecialCondition specialCondition, bool isAdded)
        {
            
        }

        public EffectResolver EffectResolver => _effectResolver;

        private void Die()
        {
            Debug.Log("NPC has died.");

            _effectResolver.ResetState();
            _effectResolver.onSpecialConditionChanged -= ShowSpecialCondition;
            Destroy(gameObject);
        }
    }
}