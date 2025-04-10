using UnityEngine;

public class IdleNode : ActionNode
{
    public override string Description() => "Idle";

    protected override State OnUpdate()
    {
        return State.Running;
    }

    public override BTNode Clone() {
        IdleNode node = Instantiate(this);
        return node;
    }

}
