using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Ground Effect Ability", menuName = "Game Module/Ability/Ground Effect Ability")]
public class GroundEffectAbilitySO : AbilitySO
{
    public GroundEffectAbilitySO()
    {
        description = "Instantly create a ground effect at position, targets are evaluated after ground effect is spawned\n\n" +
                      "Can be used for poison pool, aoe attack etc.";
    }
    
    [SerializeField] private AbilityGroundEffect groundEffectPrefab;
    [SerializeField] private Vector3 groundEffectSpawnOffset;
    [SerializeReference,Tooltip("Used when spawn position is not the targets, but a different position calculated from the targets (e.g. center of crowd)")] 
    private IPositionCalculator positionCalculator;
    [SerializeField] bool matchCasterRotation = true;

    protected override void Apply(EnemyAbility me, List<IDamageable> targets, Action onComplete)
    {
        Vector3 rotatedOffset = me.transform.rotation * groundEffectSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.transform.rotation : Quaternion.identity;
        
        if (targetSelf)
        {
            var groundEffect = Instantiate(groundEffectPrefab, me.transform.position + rotatedOffset, rotatedRotation);
            groundEffect.Setup(this, onComplete);
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.transform.position, targets == null? null : targets.Select(t=>t.Transform.position));
            var groundEffect = Instantiate(groundEffectPrefab, spawnPosition + rotatedOffset, rotatedRotation);
            groundEffect.Setup(this, onComplete);
        }
        else
        {
            foreach (var target in targets)
            {
                var groundEffect = Instantiate(groundEffectPrefab, target.Transform.position + rotatedOffset, rotatedRotation);
                groundEffect.Setup(this, onComplete);
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
        
        Vector3 rotatedOffset = me.transform.rotation * groundEffectSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.transform.rotation : Quaternion.identity;
        
        if (targetSelf)
        {
            var groundEffect = Instantiate(groundEffectPrefab, me.transform.position + rotatedOffset, rotatedRotation);
            groundEffect.Setup(this, onComplete);
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.transform.position, new List<Vector3>(){targetPos});
            var groundEffect = Instantiate(groundEffectPrefab, spawnPosition + rotatedOffset, rotatedRotation);
            groundEffect.Setup(this, onComplete);
        }
        else
        {
            var groundEffect = Instantiate(groundEffectPrefab, targetPos + rotatedOffset, rotatedRotation);
            groundEffect.Setup(this, onComplete);
        }
    }
}