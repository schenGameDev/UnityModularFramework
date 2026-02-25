using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    /// <summary>
    /// RegisterProjectile() to the Projectile Manager before Apply() or DryFire()
    /// </summary>
    [CreateAssetMenu(fileName = "Projectile Ability", menuName = "Game Module/Ability/Projectile Ability")]
    public class ProjectileAbilitySO : AbilitySO
    {
        public ProjectileAbilitySO()
        {
            description = "Launch a projectile to targets\n\n" +
                          "Can be used for fireball, missile etc.";
        }

        [Required,PropertyDropdown] public Projectile projectilePrefab;
        [SerializeField] private float maxRange;
        
        [SerializeReference, SubclassSelector]
        [Tooltip("Used when spawn position is not the targets, but a different position calculated from the targets (e.g. predict player movement)")]
        private IPositionCalculator positionCalculator;

        [SerializeField] bool matchCasterRotation = true;

        public override float AimRange() => maxRange;
        public override AimType AimMethod() => projectilePrefab.aimType;
        private uint _projectileId;

        protected override void Apply(Transform me, List<IDamageable> targets, Action onComplete)
        {
            if (!SingletonRegistry<ProjectileManagerSO>.TryGet(out var projectileManager))
            {
                Debug.LogError("Projectile Manager not found");
                onComplete?.Invoke();
                return;
            }

            Vector3 rotatedOffset = me.rotation * emitOffset;
            Quaternion rotatedRotation = matchCasterRotation ? me.rotation : Quaternion.identity;
            if (AimMethod() == AimType.Self)
            {
                var projectile = projectileManager.SpawnProjectile(_projectileId,
                    me.position + rotatedOffset,
                    rotatedRotation,
                    me.transform, null, null);
                if (projectile.effect != null) projectile.effect.onComplete = onComplete;
            }
            else if (positionCalculator != null)
            {
                Vector3 spawnPosition = positionCalculator.GetPosition(me.position,
                    targets == null ? null : targets.Select(t => t.Transform.position));
                var projectile = projectileManager.SpawnProjectile(_projectileId,
                    me.position + rotatedOffset,
                    rotatedRotation,
                    null, spawnPosition, null);
                if (projectile.effect != null) projectile.effect.onComplete = onComplete;
            }
            else
            {
                foreach (var target in targets)
                {
                    var projectile = projectileManager.SpawnProjectile(_projectileId,
                        me.position + rotatedOffset,
                        rotatedRotation,
                        target.Transform, null, null);
                    if (projectile.effect != null) projectile.effect.onComplete = onComplete;
                }
            }

        }

        public override void ReleaseDirection(Transform me, Vector3 direction, Action<AbilitySO> onComplete)
        {
            if (!SingletonRegistry<ProjectileManagerSO>.TryGet(out var projectileManager))
            {
                Debug.LogError("Projectile Manager not found");
                onComplete?.Invoke(this);
                return;
            }
            PlayVisualSoundEffects(me, null, null, new List<Vector3>() {direction});
            if (!continuousCasting)
            {
                onComplete?.Invoke(this);
                onComplete = null;
            }
        
            Vector3 rotatedOffset = me.rotation * emitOffset;
            Quaternion rotatedRotation = matchCasterRotation? me.rotation : Quaternion.identity;
            Projectile projectile;
            if (AimMethod() == AimType.Self)
            {
                projectile = projectileManager.SpawnProjectile(_projectileId, 
                    me.position + rotatedOffset, 
                    rotatedRotation,
                    me.transform, null, null);
            }
            else
            {
                projectile = projectileManager.SpawnProjectile(_projectileId, 
                    me.position + rotatedOffset, 
                    rotatedRotation, 
                    null, null, direction);
            }
            projectile.effect.onComplete = () => onComplete?.Invoke(this);
        }
        
        public override void ReleasePosition(Transform me, Vector3 targetPos,Action<AbilitySO> onComplete)
        {
            if (!SingletonRegistry<ProjectileManagerSO>.TryGet(out var projectileManager))
            {
                Debug.LogError("Projectile Manager not found");
                onComplete?.Invoke(this);
                return;
            }
            PlayVisualSoundEffects(me, null, new List<Vector3>() {targetPos}, null);
            if (!continuousCasting)
            {
                onComplete?.Invoke(this);
                onComplete = null;
            }

            Vector3 rotatedOffset = me.rotation * emitOffset;
            Quaternion rotatedRotation = matchCasterRotation ? me.rotation : Quaternion.identity;

            Projectile projectile;
            if (AimMethod() == AimType.Self)
            {
                projectile = projectileManager.SpawnProjectile(_projectileId,
                    me.position + rotatedOffset,
                    rotatedRotation,
                    me.transform, null, null);
            }
            else if (positionCalculator != null)
            {
                Vector3 spawnPosition =
                    positionCalculator.GetPosition(me.position, new List<Vector3>() { targetPos });
                projectile = projectileManager.SpawnProjectile(_projectileId,
                    me.position + rotatedOffset,
                    rotatedRotation,
                    null, spawnPosition, null);
            }
            else
            {
                projectile = projectileManager.SpawnProjectile(_projectileId,
                    me.position + rotatedOffset,
                    rotatedRotation,
                    null, targetPos, null);
            }
            if (projectile.effect != null) projectile.effect.onComplete = () => onComplete?.Invoke(this);
        }

        public void RegisterProjectile()
        {
            if (!SingletonRegistry<ProjectileManagerSO>.TryGet(out var projectileManager)) return;
            _projectileId = projectileManager.RegisterProjectile(projectilePrefab);
        }
        
#if UNITY_EDITOR
        [Button]
        private void Validate()
        {
            if (projectilePrefab == null) return;
            projectilePrefab.Validate();
            projectilePrefab.CalculateLifetime(maxRange);
        }
#endif
    }
}