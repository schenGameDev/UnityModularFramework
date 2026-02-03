using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEngine;

public class FindTagInRangeNode : FindTargetInRangeNode<Transform>
{
    [TagDropdown] public string tag;
        
    private List<Transform> _targets = new ();
    protected override void OnEnter()
    {
        _targets.Clear();
        base.OnEnter();
        if (_targets is { Count: > 0 })
        {
            tree.blackboard.Add(BTBlackboard.KEYWORD_TARGET, _targets);
        }
    }
    protected override bool Condition()
    {
        if (btRange == null) return  false;
        var tfWithTag = GameObject.FindGameObjectsWithTag(tag).Select(go => go.transform);
        var filteredTargets = ITransformTargetFilter.Filter(tfWithTag, tree.Me, btRange.targetFilters);
        _targets = btRange.targetSelector.GetStrategy(tree.Me)(filteredTargets).ToList();
        return _targets is { Count: > 0 };
    }
    

    public override BTNode Clone()
    {
        var clone = base.Clone() as FindTagInRangeNode;
        clone.tag=tag;
        return clone;
    }

    FindTagInRangeNode()
    {
        description = "Find the closest target with Tag in/out of host range\n\n" +
                      "<b>Sends</b>: 'Target' to blackboard";
    }
}