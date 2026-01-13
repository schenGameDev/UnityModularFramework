using UnityEngine;

public class MinRuntimeNode : DecoratorNode
{
    public float duration = 1;

    private float _startTime;
    private State _childState;

    protected override void OnEnter() {
        _startTime = Time.time;
        _childState = State.Running;
    }
    protected override State OnUpdate()
    {
        if(_childState==State.Running) _childState = child.Run();
        else if( Time.time - _startTime > duration) {
            return _childState;
        }

        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as MinRuntimeNode;
        clone.duration = duration;
        return clone;
    }

    MinRuntimeNode()
    {
        description = "Child node runs for at least duration seconds";
    }
}