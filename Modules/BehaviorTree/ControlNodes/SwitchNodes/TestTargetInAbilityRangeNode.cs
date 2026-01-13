using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModularFramework;

public class TestTargetInAbilityRangeNode : SwitchNode,IReady
{
    public string abilityName;
    private EnemyAbility _enemyAbility;
    public bool Ready => _enemyAbility is null || _enemyAbility.Ready;
    private List<Transform> targets = new ();
    protected override void OnEnter()
    {
        targets.Clear();
        base.OnEnter();
        if (targets is { Count: > 0 })
        {
            tree.blackboard.Add(BTBlackboard.KEYWORD_TARGET, targets);
            tree.blackboard.Add(BTBlackboard.KEYWORD_ABILITY_NAME, abilityName);
        }
    }
    
    protected override bool Condition()
    {
        _enemyAbility ??= GetComponentInMe<EnemyAbility>(abilityName);
        if (_enemyAbility == null)
        {
            Debug.LogError($"EnemyAbility of {abilityName} component not found on {tree.Me.name}");
            return false;
        }
        return IsTargetInRange();
    }
    
    private bool IsTargetInRange()
    {
        targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        if (targets == null) return false;
        targets = ITransformTargetFilter.Filter(targets, tree.Me, _enemyAbility.rangeFilter)?.ToList();
        return targets?.Count > 0;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as TestTargetInAbilityRangeNode;
        clone.abilityName = abilityName;
        return clone;
    }
    
    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(abilityName) || base.ToString().Contains(abilityName)? "" : $" ({abilityName})");
    }

    TestTargetInAbilityRangeNode()
    {
        description = "Test if target is in host ability range \n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
}