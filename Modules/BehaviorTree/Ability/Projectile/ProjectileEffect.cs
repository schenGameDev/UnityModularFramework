using System;
using System.Collections.Generic;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(Projectile))]
public class ProjectileEffect : MonoBehaviour
{
    [SerializeField,PropertyDropdown] public ImpactEffect impactEffectPrefab;
    [SerializeReference,HideField(nameof(impactEffectPrefab))] public List<IEffectFactory<IDamageable>> effects = new();
    
    public Action onComplete;
    
    public void Arrive(Transform target, Vector3 hitPoint) 
    {
        // target like ground can be too big,
        // so we use hitPoint to spawn effect, and use target to get IDamageable
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity, 
                SingletonRegistry<ProjectileManagerSO>.Instance.effectParent);
        }
        else if (target != null)
        {
            Execute(target.GetComponent<IDamageable>());
        }
    }
    
    private void Execute(IDamageable target)
    {
        if (onComplete != null)
        {
            onComplete();
            onComplete = null;
        }
        if(target == null) return;
        foreach (var effectFactory in effects)
        {
            var effect = effectFactory.Create();
            if(effect.IsTargetValid(target))
                target.TakeEffect(effect);
        }
    }
}