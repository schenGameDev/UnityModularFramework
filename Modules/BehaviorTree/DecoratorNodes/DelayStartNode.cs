using UnityEngine;

public class DelayStartNode : DecoratorNode
{
    public float Duration=1;
    private float _startTime;

    protected override void OnEnter() {
        _startTime = Time.time;
    }
    protected override State OnUpdate()
    {
        if(Time.time - _startTime > Duration) {
            return Child.Run();
        }
        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as DelayStartNode;
        clone.Duration = Duration;
        return clone;
    }

    public override string Description() => "Child start delay for duration";
}