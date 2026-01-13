using UnityEngine;

public class FaceTargetNode : DecoratorNode
{
    public bool oneTime = true;
    
    protected override void OnEnter()
    {
        var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        
        if(targets.Count > 0) tree.runner.FaceTarget(targets[0].transform, oneTime);
    }

    protected override State OnUpdate()
    {
        return child.Run();
    }

    protected override void OnExit()
    {
        tree.runner.ResetFace();
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as FaceTargetNode;
        clone.oneTime = oneTime;
        return clone;
    }
    
    FaceTargetNode()
    {
        description = "Face the target\n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
}