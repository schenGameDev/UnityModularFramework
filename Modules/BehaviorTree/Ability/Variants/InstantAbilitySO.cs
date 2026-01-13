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

    protected override void Apply(EnemyAbility me, List<IDamageable> targets, Action onComplete)
    {
        if(applyOnSelf) Execute(me.GetComponent<IDamageable>(), onComplete);
        else Execute(targets, onComplete);
    }
}