using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Ability", menuName = "Game Module/Ability/Projectile Ability")]
public class ProjectileAbilitySO : AbilitySO
{
    public ProjectileAbilitySO()
    {
        description = "Launch a projectile to targets\n\n" +
                      "Can be used for fireball, missile etc.";
    }
    
    [Required] public Projectile projectilePrefab;
    [HideInInspector] public Vector3 projectileSpawnOffset;

    [SerializeReference,Tooltip("Used when spawn position is not the targets, but a different position calculated from the targets (e.g. predict player movement)")] 
    private IPositionCalculator positionCalculator;
    
    [SerializeField] bool matchCasterRotation = true;

    protected override void Apply(Transform me, List<IDamageable> targets, Action onComplete)
    {
        if (!SingletonRegistry<ProjectileManagerSO>.TryGet(out var projectileManager))
        {
            Debug.LogError("Projectile Manager not found");
            onComplete?.Invoke();
            return;
        }
        Vector3 rotatedOffset = me.rotation * projectileSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.rotation : Quaternion.identity;
        if (targetSelf)
        {
            var projectile = projectileManager.SpawnProjectile(projectilePrefab, 
                me.position + rotatedOffset, 
                rotatedRotation,
                me.transform, null, null);
            projectile.GetComponent<ProjectileEffect>().onComplete = onComplete;
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.position, targets == null? null : targets.Select(t=>t.Transform.position));
            var projectile = projectileManager.SpawnProjectile(projectilePrefab, 
                me.position + rotatedOffset, 
                rotatedRotation, 
                null, spawnPosition, null);
            projectile.GetComponent<ProjectileEffect>().onComplete = onComplete;
        }
        else
        {
            foreach (var target in targets)
            {
                var projectile = projectileManager.SpawnProjectile(projectilePrefab, 
                    me.position + rotatedOffset, 
                    rotatedRotation, 
                    target.Transform, null, null);
                projectile.GetComponent<ProjectileEffect>().onComplete = onComplete;
            }
        }
        
    }
    
    public void DryFire(Transform me, Vector3? targetPos, Vector3? direction, Action onComplete)
    {
        if (!SingletonRegistry<ProjectileManagerSO>.TryGet(out var projectileManager))
        {
            Debug.LogError("Projectile Manager not found");
            onComplete?.Invoke();
            return;
        }
        PlayVisualSoundEffects(me, null);
        if (!continuousCasting)
        {
            onComplete?.Invoke();
            onComplete = null;
        }
        
        Vector3 rotatedOffset = me.rotation * projectileSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.rotation : Quaternion.identity;
       
        if (targetSelf)
        {
            var projectile = projectileManager.SpawnProjectile(projectilePrefab, 
                me.position + rotatedOffset, 
                rotatedRotation,
                me.transform, null, null);
            projectile.GetComponent<ProjectileEffect>().onComplete = onComplete;
        }
        else if (positionCalculator != null && targetPos.HasValue)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.position, new List<Vector3>() {targetPos.Value});
            var projectile = projectileManager.SpawnProjectile(projectilePrefab, 
                me.position + rotatedOffset, 
                rotatedRotation, 
                null, spawnPosition, null);
            projectile.GetComponent<ProjectileEffect>().onComplete = onComplete;
        }
        else
        {
            var projectile = projectileManager.SpawnProjectile(projectilePrefab, 
                me.position + rotatedOffset, 
                rotatedRotation, 
                null, targetPos, direction);
            projectile.GetComponent<ProjectileEffect>().onComplete = onComplete;
        }
    }
}