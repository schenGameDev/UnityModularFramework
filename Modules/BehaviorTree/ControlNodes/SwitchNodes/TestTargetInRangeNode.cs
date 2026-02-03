using System.Linq;
using UnityEngine;

public class TestTargetInRangeNode : SwitchNode
{
    [SerializeField] private string rangeName;
    private BTRange _btRange;
    
    public override void Prepare()
    {
        if (!rangeName.IsEmpty())
        {
            _btRange = GetComponentInMe<BTRange>(rangeName);
        }
        
        if (_btRange == null)
        {
            Debug.LogError($"BTRange of {rangeName} component not found on {tree.Me.name}");
        }
    }
    protected override bool Condition()
    {
        return IsTargetInRange();
    }
    
    private bool IsTargetInRange()
    {
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