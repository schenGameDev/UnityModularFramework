using UnityEngine;

public class RandomSelectNode : ControlNode
{
    public bool IsLoop = false;
    private int _index;
    protected override void OnEnter()
    {
        _index = Random.Range(0,Children.Count);
        currentRunningChild = Children[_index];
    }


    protected override State OnUpdate()
    {
        State s = currentRunningChild.Run();
        if(IsLoop && s!=State.Running) {
            int newIdx;
            do {
                newIdx = Random.Range(0,Children.Count);
            } while (newIdx == _index && Children.Count > 1);
            _index = newIdx;
            currentRunningChild = Children[_index];
        }
        return s;
    }
    public override string Description() => "Run a random child";

    public override BTNode Clone()
    {
        var clone = base.Clone() as RandomSelectNode;
        clone.IsLoop = IsLoop;
        return clone;
    }

}