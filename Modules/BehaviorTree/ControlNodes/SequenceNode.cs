public class SequenceNode : ControlNode
{
    private int _current;

    public override string Description() => "Children run from left to right";

    protected override void OnEnter()
    {
        _current = 0;
        currentRunningChild = Children[_current];
    }


    protected override State OnUpdate()
    {
        switch (currentRunningChild.Run()) {
            case State.Running:
                return State.Running;
            case State.Failure:
                return State.Failure;
            case State.Success:
                _current++;
                if(_current == Children.Count) return State.Success;
                currentRunningChild = Children[_current];
                break;
        }

        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as SequenceNode;
        return clone;
    }

}