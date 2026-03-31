using EditorAttributes;
using ModularFramework.Modules.Ability;
using ModularFramework.Modules.Targeting;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Ability/Projectile"), RequireComponent(typeof(BTRunner))]
    public class BTAbilityProjectile : BTAbility
    {
        [SerializeField, DrawHandle]
        private Vector3 spawnEffectOffset;
        [SerializeField,Tooltip("if targets move out of range at time of release, they will not be targeted")]
        private bool verifyRangeAtDamageTime = true;
        [SerializeField, ShowField(nameof(showAimArea))]
        private uint trajectoryAssetId;
        protected override bool VerifyRangeAtDamageTime => verifyRangeAtDamageTime;
        
        private ProjectileAbilitySO _projectileAbility;
        private ImpactEffect _impactEffect;
        private Projectile _projectile;
        

        private void Awake()
        {
            _projectileAbility = ability as ProjectileAbilitySO;
            
            
            if ( _projectileAbility != null)
            {
                _projectileAbility.RegisterProjectile();
                ability.emitOffset = spawnEffectOffset;
                var pe = _projectileAbility.projectilePrefab.GetComponent<ProjectileEffect>();
                if (pe.impactEffectPrefab != null) _impactEffect = pe.impactEffectPrefab;
                _projectile = _projectileAbility.projectilePrefab;
            } 
            
        }

        protected override void Update()
        {
            base.Update();
            if (isCastingAbility && showAimArea)
            {
                worldUI?.UpdateTrajectory(_projectile.PredictTrajectory(transform.position + transform.rotation * spawnEffectOffset, 
                    initialAimPosition));
            }
        }

        protected override void WindUp()
        {
            base.WindUp();
            if (showAimArea)
            {
                if (_impactEffect is not null)
                {
                    worldUI?.ShowImpactAreaWorld(initialAimPosition, rangeFilter, true);
                }
                worldUI?.ShowTrajectory(trajectoryAssetId, 
                    _projectile.PredictTrajectory(transform.position + transform.rotation * spawnEffectOffset, 
                        initialAimPosition));
            }
        }


        protected override void CastComplete(AbilitySO abilitySo)
        {
            if (showAimArea)
            {
                if (_impactEffect is not null)
                {
                    worldUI?.HideImpactAreaWorld();
                }

                worldUI?.HideTrajectory();
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
            if (ability is ProjectileAbilitySO)
            {
                return ValidationCheck.Pass();
            }
            return ValidationCheck.Fail("Ability is not ProjectileAbilitySO");
            
        }
        
        #endregion
    }
}