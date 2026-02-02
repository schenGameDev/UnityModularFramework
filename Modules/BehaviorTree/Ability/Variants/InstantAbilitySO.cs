using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Instant Ability", menuName = "Game Module/Ability/Instant Ability")]
public class InstantAbilitySO : AbilitySO
{
    public InstantAbilitySO()
    {
        description = "Instantly impact one or more targets\n\n" +
                      "Can be used for melee attacks, instant spells, laser, buffs, etc.";
    }
    
    [SerializeReference] public List<IEffectFactory<IDamageable>> effects = new();

    protected override void Apply(Transform me, List<IDamageable> targets, Action onComplete)
    {
        if(applyOnSelf) Execute(me.GetComponent<IDamageable>(), onComplete);
        else Execute(targets, onComplete);
    }
    
    public void Execute(List<IDamageable> targets, Action onComplete)
    {
        if(targets == null) return;
        foreach (var effectFactory in effects)
        {
            var effect = effectFactory.Create();
            if(onComplete != null) effect.OnCompleted += (e) => onComplete();
            foreach (var target in targets)
            {
                if(effect.IsTargetValid(target))
                    target.TakeEffect(effect);
            }
            
        }
    }
    
    public void Execute(IDamageable target, Action onComplete)
    {
        if(target == null) return;
        foreach (var effectFactory in effects)
        {
            var effect = effectFactory.Create();
            if(onComplete != null) effect.OnCompleted += (e) => onComplete();
            target.TakeEffect(effect);
        }
    }
}