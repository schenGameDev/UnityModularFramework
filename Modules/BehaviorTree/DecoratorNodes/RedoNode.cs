using UnityEngine;

public class RedoNode : DecoratorNode
{
    public bool OnlyWhenFailed = false;
    protected override State OnUpdate()
    {
        var res = Child.Run();
        if(OnlyWhenFailed && res == State.Success) return res;

        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as RedoNode;
        clone.OnlyWhenFailed = OnlyWhenFailed;
        return clone;
    }

    public override string Description() => "Redo";
}