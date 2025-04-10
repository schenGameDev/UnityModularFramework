using UnityEngine;

public class DelayExitNode : DecoratorNode
{
    public float Duration=1;
    private float _startTime;
    private bool _childComplete, _start;
    private State _childState;

    protected override void OnEnter() {
        _childComplete = false;
        _start = false;
        _childState = State.Running;
    }
    protected override State OnUpdate()
    {
        if(_childComplete) {
            if(!_start) {
                _startTime = Time.time;
                _start = true;
            } else {
                if(Time.time - _startTime > Duration) {
                    return _childState;
                }
            }
        } else {
            _childState = Child.Run();
            _childComplete = _childState != State.Running;
        }

        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as DelayExitNode;
        clone.Duration = Duration;
        return clone;
    }

    public override string Description() => "Child exit delay for duration";
}