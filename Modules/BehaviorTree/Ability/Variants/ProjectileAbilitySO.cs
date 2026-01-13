using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Ability", menuName = "Game Module/Ability/Projectile Ability")]
public class ProjectileAbilitySO : AbilitySO
{
    public ProjectileAbilitySO()
    {
        description = "Launch a projectile to targets\n\n" +
                      "Can be used for fireball, missile etc.";
    }
    
    [SerializeField] private AbilityProjectile projectilePrefab;
    [SerializeField] private Vector3 projectileSpawnOffset;

    [SerializeReference,Tooltip("Used when spawn position is not the targets, but a different position calculated from the targets (e.g. predict player movement)")] 
    private IPositionCalculator positionCalculator;
    
    [SerializeField] bool matchCasterRotation = true;

    protected override void Apply(EnemyAbility me, List<IDamageable> targets, Action onComplete)
    {
        
        Vector3 rotatedOffset = me.transform.rotation * projectileSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.transform.rotation : Quaternion.identity;
       
        if (targetSelf)
        {
            var projectile = Instantiate(projectilePrefab, me.transform.position + rotatedOffset, rotatedRotation);
            projectile.Setup(this,me.transform.GetComponent<IDamageable>(), onComplete);
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.transform.position, targets == null? null : targets.Select(t=>t.Transform.position));
            var projectile = Instantiate(projectilePrefab, spawnPosition + rotatedOffset, rotatedRotation);
            projectile.Setup(this, spawnPosition, onComplete);
        }
        else
        {
            foreach (var target in targets)
            {
                var projectile = Instantiate(projectilePrefab, me.transform.position + rotatedOffset,rotatedRotation);
                projectile.Setup(this, target, onComplete);
            }
        }
    }
    
    public void DryFire(EnemyAbility me, Vector3 targetPos, Action onComplete)
    {
        PlayVisualSoundEffects(me, null);
        if (!continuousCasting)
        {
            onComplete?.Invoke();
            onComplete = null;
        }
        
        Vector3 rotatedOffset = me.transform.rotation * projectileSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.transform.rotation : Quaternion.identity;
       
        if (targetSelf)
        {
            var projectile = Instantiate(projectilePrefab, me.transform.position + rotatedOffset, rotatedRotation);
            projectile.Setup(this,me.transform.GetComponent<IDamageable>(), onComplete);
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.transform.position, new List<Vector3>() {targetPos});
            var projectile = Instantiate(projectilePrefab, spawnPosition + rotatedOffset, rotatedRotation);
            projectile.Setup(this, spawnPosition, onComplete);
        }
        else
        {
            var projectile = Instantiate(projectilePrefab, me.transform.position + rotatedOffset,rotatedRotation);
            projectile.Setup(this, targetPos, onComplete);
        }
    }
}