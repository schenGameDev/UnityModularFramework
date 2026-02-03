using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestTargetInAbilityRangeNode : SwitchNode
{
    public string abilityName;
    private BTAbility _btAbility;
    private List<Transform> targets = new ();
    public override void Prepare()
    {
        _btAbility = GetComponentInMe<BTAbility>(abilityName);
        if (_btAbility == null)
        {
            Debug.LogError($"Ability of {abilityName} component not found on {tree.Me.name}");
        }
    }
    protected override void OnEnter()
    {
        targets.Clear();
        base.OnEnter();
        tree.blackboard.Add(BTBlackboard.KEYWORD_ABILITY_NAME, abilityName);
        if (targets is { Count: > 0 })
        {
            tree.blackboard.Add(BTBlackboard.KEYWORD_TARGET, targets);
        }
    }
    
    protected override bool Condition()
    {
        return IsTargetInRange();
    }
    
    private bool IsTargetInRange()
    {
        targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        if (targets == null) return false;
        targets = ITransformTargetFilter.Filter(targets, tree.Me, _btAbility.rangeFilter)?.ToList();
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