using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework.Modules.Ability;
using ModularFramework.Modules.Targeting;
using Sisus.ComponentNames;
using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.BehaviorTree
{
    public abstract class BTAbility : MonoBehaviour, IUniqueIdentifiable, IReady
    {
        [Header("Targeting")] 
        [SerializeReference, SubclassSelector,Validate(nameof(ValidateSelector))]
        public ITransformTargetSelector targetSelector;

        public RangeFilter rangeFilter;

        [Tooltip("Number of targets"), Min(0)] 
        public int targetNumber = 1;
        
        public bool showAimArea;
        
        [Header("Casting")] 
        [Required, SerializeField, OnValueChanged(nameof(RenameComponent)),Validate(nameof(ValidateAbility))]
        protected AbilitySO ability;

        [SerializeField] protected string windUpAnimation;
        [SerializeField] protected string releaseAnimation;
        
        [Header("Runtime")] 
        [ShowInInspector, ReadOnly]
        protected bool isCastingAbility;

        [ReadOnly, ShowInInspector] private bool _isReady = true;

        public bool Ready => _isReady;

        protected List<IDamageable> targets;
        private Action<bool> _abilityReleaseCallback;
        [SerializeField, Self] protected BTRunner runner;
        [SerializeField, Self(Flag.Optional)] protected BTWorldUI worldUI;
        private CountdownTimer _cooldownTimer;
        protected Vector3 initialAimPosition;
        protected abstract bool VerifyRangeAtDamageTime { get; }

#if UNITY_EDITOR
        [SerializeField, ToggleGroup("Gizmos", nameof(gizmosColor))]
        private bool showGizmos = true;

        [SerializeField, HideProperty, HideLabel]
        private Color gizmosColor = Color.red;
        
        private void OnValidate() => this.ValidateRefs();
#endif

        private void Awake()
        {
            if (targetSelector == null)
            {
                Debug.LogWarning($"No Target Selector assigned to Ability {AbilityName}!");
            }
        }

        private void Start()
        {
            if (ability.cooldown > 0)
            {
                _cooldownTimer = new CountdownTimer(ability.cooldown);
                _cooldownTimer.OnTimerStart += () => _isReady = false;
                _cooldownTimer.OnTimerStop += () => _isReady = true;
            }
        }

        public void Cast(List<IDamageable> targets, Action<bool> callback)
        {
            if (isCastingAbility)
            {
                Debug.LogWarning($"Casting {AbilityName} is already active, cannot cast again");
                callback(false);
                return;
            }

            if (!_isReady)
            {
                Debug.LogWarning($"Ability {AbilityName} is not ready yet (on cooldown)");
                callback(false);
                return;
            }

            this.targets = targets;
            if (GetAimPosition())
            {
                runner.FaceTarget(initialAimPosition);
            }
            else
            {
                callback(false);
                return;
            }

            isCastingAbility = true;
            _abilityReleaseCallback = callback;
            WindUp();
        }

        protected virtual void WindUp()
        {
            runner.PlayAnim(windUpAnimation, Release);
            AimAtTargets();
            if (showAimArea)
            {
                worldUI?.ShowImpactAreaLocal(rangeFilter);
            }
        }

        protected virtual void Release()
        {
            targets = VerifyRangeAtDamageTime
                ? targets.Where(t =>
                {
                    if (!RangeFilter.WithinGroundRange(transform, t.Transform, rangeFilter.minMaxRange))
                    {
                        t.AimedAtBy(false, transform);
                        return false;
                    }

                    return true;
                }).ToList()
                : targets;
            if (targets.Count == 0)
            {
                Debug.LogWarning($"{AbilityName} Missed, targets not within range");
                ability.ReleasePosition(transform, initialAimPosition, CastComplete);
                return;
            }

            ability.Release(transform, targets, CastComplete);
        }

        /// <summary>
        /// for continuous casting end: one effect ends, complete the cast
        /// </summary>
        protected virtual void CastComplete(AbilitySO abilitySo)
        {
            if (showAimArea)
            {
                worldUI?.HideImpactAreaLocal();
            }
            runner.StopAnim(releaseAnimation);
            _abilityReleaseCallback?.Invoke(true);
            AimAtTargets(false);
            isCastingAbility = false;
            _abilityReleaseCallback = null;
            _cooldownTimer?.Start();
        }



        public void Interrupt()
        {
            if (!isCastingAbility) return;
            runner.StopAnim(windUpAnimation);
            if (showAimArea)
            {
                worldUI?.HideImpactAreaLocal();
                worldUI?.HideImpactAreaWorld();
                worldUI?.HideTrajectory();
            }
            runner.StopAnim(releaseAnimation);
            _abilityReleaseCallback?.Invoke(false);
            AimAtTargets(false);
            isCastingAbility = false;
            _abilityReleaseCallback = null;
            _cooldownTimer?.Restart();
        }

        private void AimAtTargets(bool on = true)
        {
            foreach (var target in targets)
            {
                target.AimedAtBy(on, transform);
            }
        }
        
        protected virtual bool GetAimPosition()
        {
            if (!isCastingAbility) return false;
            if (targets == null || targets.Count == 0) return false;
            initialAimPosition = targets[0].Transform.position;
            return true;
        }

        protected virtual void Update()
        {
            GetAimPosition();
        }
        

#if UNITY_EDITOR 
        private void OnDrawGizmos()
        {
            if (showGizmos && !Application.isPlaying)
            {
                Gizmos.color = gizmosColor;
                GizmosExtension.DrawPolygons(rangeFilter.GetRangeSector(transform));

            }
        }
#endif
        
        public bool TargetAtSelf => ability != null && ability.AimMethod() == AimType.Self;

        #region Editor
        protected virtual ValidationCheck ValidateAbility()
        {
            if (ability == null)
            {
                return ValidationCheck.Fail("Ability is missing");
            }

            return ValidationCheck.Pass();
        }

        protected ValidationCheck ValidateSelector()
        {
            if (targetSelector == null)
            {
                return ValidationCheck.Fail("TargetSelector is missing");
            }
            return ValidationCheck.Pass();
        }
        
        protected void RenameComponent() =>
            this.SetName($"Ability: {AbilityName}" + (ability && ability.continuousCasting ? " (Continuous)" : ""));
        #endregion

        private string AbilityName => ability == null
            ? "Unassigned"
            : ability.name.StartsWith("Ability_")
                ? ability.name[8..]
                : ability.name;

        public string UniqueId => AbilityName;
    }
}