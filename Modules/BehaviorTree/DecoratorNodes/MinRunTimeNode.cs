using UnityEngine;

public class MinRunTimeNode : DecoratorNode
{
    public float Duration=1;

    private float _startTime;
    private State _childState;

    protected override void OnEnter() {
        _startTime = Time.time;
        _childState = State.Running;
    }
    protected override State OnUpdate()
    {
        if(_childState==State.Running) _childState = Child.Run();
        else if( Time.time - _startTime > Duration) {
            return _childState;
        }

        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as MinRunTimeNode;
        clone.Duration = Duration;
        return clone;
    }

    public override string Description() => "Child runs at least the duration";
}