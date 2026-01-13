using System.Collections.Generic;
using UnityEngine;

public abstract class TriggerTargetMethodNode<TTarget> : ActionNode where TTarget : Component
{
    private List<TTarget> _targets;

    protected override void OnEnter()
    {
        base.OnEnter();
        _targets = tree.blackboard.Get<TTarget>(BTBlackboard.KEYWORD_TARGET);
    }

    protected override State OnUpdate()
    {
        if(_targets == null) return State.Failure;
        _targets.ForEach(TriggerMethodOnTargets);
        return State.Success;
    }

    protected abstract void TriggerMethodOnTargets(TTarget t);
    
    public override BTNode Clone() {
        TriggerTargetMethodNode<TTarget> node = Instantiate(this);
        return node;
    }
    
    protected TriggerTargetMethodNode()
    {
        description = "trigger target method\n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
}