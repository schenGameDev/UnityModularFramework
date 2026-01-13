using UnityEngine;

public class FindTargetByKeyword : SwitchNode
{
    public string keyword;
    

    protected override bool Condition()
    {
        var targets = tree.blackboard.Get<Transform>(keyword);
        if (targets is { Count: > 0 })
        {
            tree.blackboard.Add(BTBlackboard.KEYWORD_TARGET, targets);
            return true;
        }
        return false;
    }
    

    public override BTNode Clone()
    {
        var clone = base.Clone() as FindTargetByKeyword;
        clone.keyword = keyword;
        return clone;
    }
    
    FindTargetByKeyword()
    {
        description = "Replace target with the transforms labelled by keyword \n\n" +
                      "<b>Requires</b>: keyword in blackboard";
    }
}