using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using Void = EditorAttributes.Void;

public abstract class AbilitySO : ScriptableObject
{
    [Title(nameof(description),12,stringInputMode:StringInputMode.Dynamic),SerializeField]
    private Void titleHolder;
    protected string description;
    
    public AudioClip releaseSfx;
    public GameObject[] releaseVfx;
    
    [Tooltip("The ability is release at my position, it doesn't mean the effects are applied to me!!")]
    public bool targetSelf; 
    public bool applyOnSelf;
    
    [SerializeField,Tooltip("If true, i will stay in this state until end or interrupted")]
    public bool continuousCasting;
    
    
    [SerializeReference] public List<IEffectFactory<IDamageable>> effects = new();

    public void Release(BTAbility me, List<IDamageable> targets, Action onComplete)
    {
        PlayVisualSoundEffects(me, targets);
        Apply(me, targets, continuousCasting? onComplete : null);
        if (!continuousCasting && onComplete != null)
        {
            onComplete();
        }
    }

    protected abstract void Apply(BTAbility me, List<IDamageable> targets, Action onComplete);

    protected virtual void PlayVisualSoundEffects(BTAbility me, List<IDamageable> targets)
    {
        
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
                if(effect.IsTargetValid(target)) target.TakeEffect(effect);
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