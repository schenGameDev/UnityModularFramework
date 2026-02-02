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
    
    public void Arrive(Transform target)
    {
        if (target == null) return;
       
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, target.position, Quaternion.identity, 
                SingletonRegistry<ProjectileManagerSO>.Instance.effectParent);
        }
        else
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