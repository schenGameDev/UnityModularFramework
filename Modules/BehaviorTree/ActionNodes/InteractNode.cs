using UnityEngine;

public class InteractNode : ActionNode
{
    [SerializeField] private string interactName;
    
    private BTInteract _btInteract;
    private State _interactEndState = State.Running;

    protected override void OnEnter()
    {
        base.OnEnter();
        _btInteract ??= GetComponentInMe<BTInteract>(interactName);
        _interactEndState = State.Running;
        Interact();
    }

    protected override State OnUpdate()
    {
        return _interactEndState;
    }

    private void Interact() {
        var target = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET)?[0];
        _btInteract.Interact(target, OnInteractComplete);
    }
    
    private void OnInteractComplete(bool success) {
        _interactEndState = success ? State.Success : State.Failure;
    }

    public override BTNode Clone() {
        InteractNode node = Instantiate(this);
        return node;
    }

    public override string ToString()
    {
        return base.ToString() + (string.IsNullOrEmpty(interactName)? "" : $" ({interactName})");
    }

    public InteractNode()
    {
        description = "Interact with in-scene object";
    }
}