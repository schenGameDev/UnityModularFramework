using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

public abstract class FindTargetInAbilityRangeNode<TTarget> : SwitchNode where TTarget : Component
{
    public string abilityName;
    public bool faceTargetBeforeCheck = true;
    
    private BTAbility _btAbility;
    private List<TTarget> targets = new ();
    
    public override void Prepare()
    {
        _btAbility = GetComponentInMe<BTAbility>(abilityName);
        if (_btAbility == null)
        {
            Debug.LogError($"BTAbility of {abilityName} component not found on {tree.Me.name}");
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

    protected override bool Condition() {
        return AnyTargetInRightRange();
    }

    private bool AnyTargetInRightRange()
    {
        if (faceTargetBeforeCheck)
        {
            var rangeFilterWithoutViewCone = new RangeFilter()
            {
                rangeType = _btAbility.rangeFilter.rangeType,
                minMaxRange = _btAbility.rangeFilter.minMaxRange,
                viewAngle = 360
            };
            
            
            var potentialTargets = Registry<TTarget>.Get(_btAbility.targetSelector.GetStrategy<TTarget>(tree.Me, _btAbility.targetNumber), 
                ((ITransformTargetFilter)rangeFilterWithoutViewCone).GetStrategy<TTarget>(tree.Me)).ToList(); 
            if (potentialTargets.Count == 0) return false;
            tree.runner.FaceTarget(potentialTargets[0].transform, false);
        }
        
        targets = Registry<TTarget>.Get(_btAbility.targetSelector.GetStrategy<TTarget>(tree.Me, _btAbility.targetNumber), 
            ((ITransformTargetFilter)_btAbility.rangeFilter).GetStrategy<TTarget>(tree.Me)).ToList(); 
        if (targets == null || targets.Count == 0) return false;
        
        if(!started) Debug.Log("Target: " + string.Join(",",targets.Select(t => t.name)));
        
        return true;
    }
    
    public override BTNode Clone()
    {
        var clone = base.Clone() as FindTargetInAbilityRangeNode<TTarget>;
        clone.abilityName = abilityName;
        clone.faceTargetBeforeCheck = faceTargetBeforeCheck;
        return clone;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(abilityName)? "" : $" ({abilityName})");
    }

    FindTargetInAbilityRangeNode()
    {
        description = "Find target in host ability range \n\n" +
                      "<b>Requires</b>: AnonymousAbilityNode as child node\n\n" +
                      "<b>Sends</b>: 'AbilityName' and 'Target' to blackboard";
    }
}
