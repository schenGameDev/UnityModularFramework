using UnityEngine;

public class DelayStartNode : DecoratorNode
{
    public float duration=1;
    private float _startTime;

    protected override void OnEnter() {
        _startTime = Time.time;
    }
    protected override State OnUpdate()
    {
        if(Time.time - _startTime > duration) {
            return child.Run();
        }
        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as DelayStartNode;
        clone.duration = duration;
        return clone;
    }

    protected DelayStartNode()
    {
        description  = "Child start delay for duration";
    }
}