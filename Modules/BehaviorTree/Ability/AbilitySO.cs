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
    
    public void Release(Transform me, List<IDamageable> targets, Action onComplete)
    {
        PlayVisualSoundEffects(me, targets);
        Apply(me, targets, continuousCasting? onComplete : null);
        if (!continuousCasting && onComplete != null)
        {
            onComplete();
        }
    }

    protected abstract void Apply(Transform me, List<IDamageable> targets, Action onComplete);

    protected virtual void PlayVisualSoundEffects(Transform me, List<IDamageable> targets)
    {
        
    }
}