using System.Linq;
using UnityEngine;

public class TestTargetInRangeNode : SwitchNode
{
    public string rangeName;
    private BTRange _btRange;
    
    protected override bool Condition()
    {
        _btRange??= GetComponentInMe<BTRange>(rangeName);
        if (_btRange == null)
        {
            Debug.LogError($"EnemyRange of {_btRange} component not found on {tree.Me.name}");
            return false;
        }
        return IsTargetInRange();
    }
    
    private bool IsTargetInRange()
    {
        if (_btRange == null)
        {
            Debug.LogError($"EnemyRange of {rangeName} component not found on {tree.Me.name}");
            return false;
        }
        var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        if (targets == null) return false;
        targets = ITransformTargetFilter.Filter(targets, tree.Me, _btRange.targetFilters)?.ToList();
        return targets?.Count > 0;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as TestTargetInRangeNode;
        clone.rangeName = rangeName;
        return clone;
    }
    
    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(rangeName) || base.ToString().Contains(rangeName)? "" : $" ({rangeName})");
    }

    TestTargetInRangeNode()
    {
        description = "Test if target is in/out of host range \n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
}