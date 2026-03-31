using EditorAttributes;
using ModularFramework.Modules.Ability;
using ModularFramework.Modules.Targeting;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Ability/Ground Effect"), RequireComponent(typeof(BTRunner))]
    public class BTAbilityGroundEffect : BTAbility
    {
        [SerializeField, DrawHandle]
        private Vector3 spawnEffectOffset;
        [SerializeField,Tooltip("if targets move out of range at time of release, they will not be targeted")]
        private bool verifyRangeAtDamageTime = true;
        protected override bool VerifyRangeAtDamageTime => verifyRangeAtDamageTime;
        
        private GroundEffectAbilitySO _groundEffectAbility;
        private ImpactEffect _impactEffect;
        
        
        private void Awake()
        {
            _groundEffectAbility = ability as GroundEffectAbilitySO;
            if (_groundEffectAbility != null)
            {
                _impactEffect = _groundEffectAbility.impactEffectPrefab;
            }
            ability.emitOffset = spawnEffectOffset;
        }

        protected override void WindUp()
        {
            base.WindUp();
            if (showAimArea && _impactEffect is not null)
            {
                worldUI?.ShowImpactAreaWorld(initialAimPosition, rangeFilter, true);
            }
        }

        protected override void CastComplete(AbilitySO abilitySo)
        {
            if (showAimArea && _impactEffect is not null)
            {
                worldUI?.HideImpactAreaWorld();
            }
            base.CastComplete(abilitySo);
        }

        protected override bool GetAimPosition()
        {
            if (!isCastingAbility) return false;
            if (targets == null || targets.Count == 0) return false;
            initialAimPosition = RangeFilter.GetClosestPointInGroundPosition(transform, targets[0].Transform, 
                rangeFilter.minMaxRange);
            return true;
        }
        
        #region Editor
        protected override ValidationCheck ValidateAbility()
        {
            var baseResult = base.ValidateAbility();
            if (!baseResult.PassedCheck) return baseResult;
            if (ability is GroundEffectAbilitySO)
            {
                return ValidationCheck.Pass();
            }
            return ValidationCheck.Fail("Ability is not GroundEffectAbilitySO");
            
        }
        
        #endregion
    }
}