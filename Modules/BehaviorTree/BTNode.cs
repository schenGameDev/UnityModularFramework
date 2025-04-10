using System.Collections.Generic;
using UnityEngine;

public abstract class BTNode : ScriptableObject
{
    public enum State {
        Running,Success,Failure
    }

    public State NodeState = State.Running;
    [HideInInspector] public string Guid;
    [HideInInspector] public Vector2 Position;
    [HideInInspector] public BehaviorTreeSO tree;

    public abstract string Description();
    public virtual bool IsNodeChildrenFull() => false;

    public bool Started = false;
    public virtual State Run() {
        if(!Started) {
            OnEnter();
            Started = true;
        }

        NodeState = OnUpdate();

        if(NodeState != State.Running) {
            OnExit();
            Started = false;
        }

        return NodeState;
    }

    public virtual void Exit() {
        OnExit();
        Started = false;
    }

    protected virtual void OnEnter() {}
    protected abstract State OnUpdate();
    // public virtual void OnFixedUpdate() {}
    protected virtual void OnExit() {}

    public virtual BTNode Clone() {
        return Instantiate(this);
    }

    public virtual void Register() {

    }

}
