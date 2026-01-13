using UnityEngine;

public class LookAtTargetNode : ActionNode
{
    private Transform _target;

    protected override void OnEnter()
    {
        base.OnEnter();
        _target = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET)?[0];
    }

    protected override State OnUpdate()
    {
        if(_target == null) return  State.Failure;
        tree.runner.FaceTarget(_target, false);
        return State.Running;
    }

    public override BTNode Clone() {
        LookAtTargetNode node = Instantiate(this);
        return node;
    }

    LookAtTargetNode()
    {
        description =  "Look at target and stand still\n\n" +
                       "<b>Requires</b>: 'Target' in blackboard";
    }
}