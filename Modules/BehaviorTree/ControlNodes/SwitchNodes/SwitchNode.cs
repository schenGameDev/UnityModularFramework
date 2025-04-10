public abstract class SwitchNode : ControlNode
{
    private int _current = 0;
    protected override void OnEnter()
    {
        _current = 0;
        currentRunningChild = Children[_current];
    }

    protected abstract bool IsSwitchState();

    private State _lastState;
    protected override State OnUpdate()
    {
        bool isSwitchState = _lastState!=State.Running || IsSwitchState();
        if((_current == 0 && isSwitchState) ||
           (_current == 1 && !isSwitchState)) {
            _current = _current == 0? 1 : 0;
            currentRunningChild.Exit();
            currentRunningChild = Children[_current];
        }

        _lastState = currentRunningChild.Run();

        return State.Running;
    }
    protected override int MaxChildrenNum() => 2;

}
