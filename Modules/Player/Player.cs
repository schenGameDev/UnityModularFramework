using System;
using System.Collections.Generic;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Modules.Ability;
using UnityEngine;
using UnityTimer;

namespace UnityModularFramework.Modules.Player
{
    /// <summary>
    /// Player take damage and effects implementation
    /// </summary>
    [AddComponentMenu("Player/Player", 0), DisallowMultipleComponent]
    public class Player : Character,IDamageable
    {
        [Header("Status")]
        public List<StatusDef> playerStatus;
        private Dictionary<StatusKey, StatusDef> _statusDict;
        
        private EffectResolver _effectResolver;

        public DamageTarget TargetType { get; }
        public Transform Transform => transform;
        [Self,SerializeField] private PlayerMoveView playerMoveView;
        [Self(Flag.Optional),SerializeField] private PlayerUI playerUI;

    #if UNITY_EDITOR
        private void OnValidate()=> this.ValidateRefs();
        
    #endif

        private void Awake()
        {
            _effectResolver = new EffectResolver(this, 1);
            _effectResolver.onSpecialConditionChanged += ShowSpecialCondition;
            _statusDict = new Dictionary<StatusKey, StatusDef>();
            foreach(var status in playerStatus) 
            {
                status.Start();
                _statusDict.Add(status.key, status);
            }
        }

        private void OnEnable()
        {
            SingletonRegistry<Player>.Replace(this);
            DictSetRegistry<DamageTarget, Transform>.TryAdd(TargetType, Transform);
        }

        private void OnDisable()
        {
            SingletonRegistry<Player>.Clear();
            DictSetRegistry<DamageTarget, Transform>.Remove(TargetType, Transform);
        }
        
        private void OnDestroy()
        {
            SingletonRegistry<Player>.Clear();
            DictSetRegistry<DamageTarget, Transform>.Remove(TargetType, Transform);
            foreach(var status in playerStatus) 
            {
                status.Destroy();
            }
        }

        public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
        {
            playerUI?.ShowHighlightMarker(isAiming);
        }
        
        
        public void TakeDamage(float amount, DamageType damageType, Transform source)
        {
            if(damageType == DamageType.Physical) TakePhysicalDamage(amount);
        }

        #region Knock Back
        Action _knockBackCompleteCallback;
        public void KnockBack(Vector3 direction, float duration, float distance, Action onComplete)
        {
            direction.Normalize();
            Vector3 knockBackVelocity = direction * (distance / duration);
            // keep moving until duration is up or hit obstacle
            _knockBackCompleteCallback = onComplete;
            playerMoveView.ApplyExternalVelocity(knockBackVelocity, duration);
        }
        
        public void KnockBackComplete()
        {
            _knockBackCompleteCallback?.Invoke();
            _knockBackCompleteCallback = null;
        }
        

        #endregion

        public EffectResolver EffectResolver => _effectResolver;

        private void TakePhysicalDamage(float amount)
        {
            float healthAfter = ConsumeStatus(StatusKey.HEALTH, amount);
            playerUI?.UpdateHealthBar(_statusDict[StatusKey.HEALTH].Ratio);
            Debug.Log($"player has taken {amount} damage.");
            if (healthAfter <= 0)
            {
                Die();
            }
        }
        
        private void ShowSpecialCondition(SpecialCondition specialCondition, bool isAdded)
        {
                
        }

        private void Die()
        {
            // Handle death logic here
            Debug.Log("player has died.");
            _effectResolver.ResetState();
            _effectResolver.onSpecialConditionChanged -= ShowSpecialCondition;
            Destroy(gameObject);
        }


        #region Resource
        public enum StatusKey
        {
            NONE, HEALTH, STAMINA, MANA
        }

        [Serializable]
        public class StatusDef
        {
            public StatusKey key;
            public float maxAmount;
            [Min(0)] public float regrowRate;
            [Min(0)] public float regrowDelay; // not consumed for a period of time
            [ReadOnly] public float currentAmount;
            
            private float _regrowDelayTimer;
            
            public void Start()
            {
                currentAmount = maxAmount;
                if (regrowRate > 0)
                {
                    TimerManager.Tick += SelfRegenerate;
                }
            }

            public void Destroy()
            {
                if (regrowRate > 0) TimerManager.Tick -= SelfRegenerate;
            }

            public float ChangeAmount(float amount)
            {
                if (amount != 0)
                {
                    _regrowDelayTimer = 0;
                }
                currentAmount += amount;
                return currentAmount;
            }
            
            private void SelfRegenerate(float dt)
            {
                if (currentAmount >= maxAmount) return;
                if (_regrowDelayTimer < regrowDelay)
                {
                    _regrowDelayTimer += dt;
                }
                else
                {
                    currentAmount = Mathf.Min(currentAmount + regrowRate * dt, maxAmount);
                }
            }
            
            public float Ratio => currentAmount / maxAmount;
        }

        [Serializable]
        public struct StatusConsumptionDef
        {
            public StatusKey key;
            [HideField(nameof(key), StatusKey.NONE)] public float startAmount;
            [HideField(nameof(key), StatusKey.NONE)] public float sustainAmount;
            
            public bool NeedSustain => key != StatusKey.NONE && sustainAmount > 0;
        }
        
        public bool IsStatusEnough(StatusKey key, float amount)
        {
            if (key == StatusKey.NONE) return true; // no status required
            if (!_statusDict.TryGetValue(key, out var status)) return false;
            return status.currentAmount >= amount;
        }

        public float ConsumeStatus(StatusKey key, float amount)
        {
            if (key == StatusKey.NONE) return 100; // no status required
            if (!_statusDict.TryGetValue(key, out var status)) return 0;
            status.ChangeAmount(- amount);
            return status.currentAmount;
        }
        
        public float RegrowStatus(StatusKey key, float amount) => ConsumeStatus(key, - amount);
        #endregion
    }
}