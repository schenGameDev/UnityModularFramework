using EditorAttributes;
using UnityEngine;

public class SequenceNode : ControlNode
{
    [Tooltip("If true, the sequence will ignore failures and continue to the next child")]
    public bool ignoreFailure;
    [ShowField(nameof(ignoreFailure))] public bool returnOnSuccess;
    public bool onlyIncludeReadyChildren;
    private int _current;

    protected override void OnEnter()
    {
        _current = 0;
        currentRunningChild = Children[_current];
    }


    protected override State OnUpdate()
    {
        // precheck if child is ready
        while (onlyIncludeReadyChildren)
        {
            if (IsReady(currentRunningChild))
                break;

            // if ignoring failure, move to next child
            if (++_current == Children.Count)
                return State.Failure;

            currentRunningChild = Children[_current];
        }
        
        switch (currentRunningChild.Run()) {
            case State.Running:
                break;
            case State.Failure when ignoreFailure:
                _current++;
                if(_current == Children.Count) return State.Success; 
                currentRunningChild = Children[_current];
                break;
            case State.Success:
                _current++;
                if(_current == Children.Count || returnOnSuccess && ignoreFailure) return State.Success;
                currentRunningChild = Children[_current];
                break;
            case State.Failure:
                return State.Failure;
        }

        return State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as SequenceNode;
        clone.ignoreFailure = ignoreFailure;
        clone.onlyIncludeReadyChildren = onlyIncludeReadyChildren;
        clone.returnOnSuccess = returnOnSuccess;
        return clone;
    }

    public override string ToString()
    {
        return base.ToString() + (ignoreFailure ? " (Ignore Failure)" : "");
    }
    
    SequenceNode() 
    {
        description = "Children run from left to right until one child fails";
    }
}