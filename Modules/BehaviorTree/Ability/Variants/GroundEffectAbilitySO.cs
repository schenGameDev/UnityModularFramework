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
    
    [SerializeField] private ImpactEffect impactEffectPrefab;
    [HideInInspector] public Vector3 groundEffectSpawnOffset;
    [SerializeReference,SubclassSelector]
    [Tooltip("Used when spawn position is not the targets, but a different position calculated from the targets (e.g. center of crowd)")] 
    private IPositionCalculator positionCalculator;
    [SerializeField] bool matchCasterRotation = true;

    protected override void Apply(Transform me, List<IDamageable> targets, Action onComplete)
    {
        Vector3 rotatedOffset = me.rotation * groundEffectSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.rotation : Quaternion.identity;
        
        if (targetSelf)
        {
            var groundEffect = Instantiate(impactEffectPrefab, me.position + rotatedOffset, rotatedRotation);
            groundEffect.onComplete = onComplete;
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.position, targets?.Select(t=>t.Transform.position));
            var groundEffect = Instantiate(impactEffectPrefab, spawnPosition + rotatedOffset, rotatedRotation);
            groundEffect.onComplete = onComplete;
        }
        else
        {
            foreach (var target in targets)
            {
                var groundEffect = Instantiate(impactEffectPrefab, target.Transform.position + rotatedOffset, rotatedRotation);
                groundEffect.onComplete = onComplete;
            }
        }
    }
    
    public void DryFire(Transform me, Vector3 targetPos, Action onComplete)
    {
        PlayVisualSoundEffects(me, null);
        if (!continuousCasting)
        {
            onComplete?.Invoke();
            onComplete = null;
        }
        
        Vector3 rotatedOffset = me.rotation * groundEffectSpawnOffset;
        Quaternion rotatedRotation = matchCasterRotation? me.rotation : Quaternion.identity;
        
        if (targetSelf)
        {
            var groundEffect = Instantiate(impactEffectPrefab, me.position + rotatedOffset, rotatedRotation);
            groundEffect.onComplete = onComplete;
        }
        else if (positionCalculator != null)
        {
            Vector3 spawnPosition = positionCalculator.GetPosition(me.position, new List<Vector3>(){targetPos});
            var groundEffect = Instantiate(impactEffectPrefab, spawnPosition + rotatedOffset, rotatedRotation);
            groundEffect.onComplete = onComplete;
        }
        else
        {
            var groundEffect = Instantiate(impactEffectPrefab, targetPos + rotatedOffset, rotatedRotation);
            groundEffect.onComplete = onComplete;
        }
    }
}