using UnityEngine;

public class RedoNode : DecoratorNode
{
    public enum RedoCondition
    {
        Always,
        WhenFailed,
        WhenSucceeded
    }
    
    [Tooltip("only redo under given condition")]
    public RedoCondition condition = RedoCondition.Always;
    [Tooltip("Number of times to redo the child node, negative means infinity")]
    public int redoTimes=-1;
    private int _count;

    protected override void OnEnter()
    {
        _count = 0;
    }

    protected override State OnUpdate()
    {
        var res = child.Run();
        if (res == State.Running)
        {
            return State.Running;
        }
        // conditions
        if (condition == RedoCondition.WhenFailed && res == State.Success ||
            condition == RedoCondition.WhenSucceeded && res == State.Failure)
        {
            return res;
        }
        // times
        if (redoTimes < 0) return State.Running;
        return ++_count >= redoTimes ? res : State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as RedoNode;
        clone.condition = condition;
        clone.redoTimes = redoTimes;
        return clone;
    }
    
    public override string ToString()
    {
        return base.ToString() + $" ({condition.ToString()})" + (redoTimes >= 0 ? $" [{redoTimes}]" : "");
    }

    private RedoNode()
    {
        description = "Rerun child node";
        titleCustomizable = false;
    }
}