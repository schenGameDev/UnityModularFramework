using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

public abstract class FindTargetInRangeNode<TTarget> : SwitchNode where TTarget : Component
{
    public string rangeName;
    [Tooltip("Number of targets to select"),Min(0)] public int number = 1;

    protected BTRange BtRange;
    
    private List<TTarget> targets = new ();
    protected override void OnEnter()
    {
        targets.Clear();
        base.OnEnter(); // condition() called here
        if (targets is { Count: > 0 })
        {
            tree.blackboard.Add(BTBlackboard.KEYWORD_TARGET, targets);
        }
    }

    protected override bool Condition() {
        BtRange??= GetComponentInMe<BTRange>(rangeName);
        if (BtRange == null)
        {
            Debug.LogError($"EnemyRange of {rangeName} component not found on {tree.Me.name}");
            return false;
        }
        return AnyTargetInRightRange();
    }

    private bool AnyTargetInRightRange() {
        if (BtRange == null)
        {
            Debug.LogError($"EnemyRange of {rangeName} component not found on {tree.Me.name}");
            return false;
        }
        targets = Registry<TTarget>.Get(BtRange.targetSelector.GetStrategy<TTarget>(tree.Me, number), 
            BtRange.targetFilters?.Select(f => f.GetStrategy<TTarget>(tree.Me)).ToArray()).ToList(); 
        if (targets == null || targets.Count == 0) return false;

        return true;
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as FindTargetInRangeNode<TTarget>;
        clone.rangeName = rangeName;
        clone.number = number;
        return clone;
    }
    
    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(rangeName) || base.ToString().Contains(rangeName)? "" : $" ({rangeName})") + (number <= 1 ? "" : $" [{number}]");
    }

    protected FindTargetInRangeNode()
    {
        description = "Find targets of type " + typeof(TTarget) + " in/out of host range\n\n" +
                      "<b>Sends</b>: 'Target' to blackboard";
    }
}