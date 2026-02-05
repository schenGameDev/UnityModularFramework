using UnityEngine;

public class InteractNode : ActionNode
{
    [SerializeField] private string interactName;
    
    private BTInteract _btInteract;
    private State _interactEndState = State.Running;

    public override void Prepare()
    {
        base.Prepare();
        _btInteract = GetComponentInMe<BTInteract>(interactName);
        if (_btInteract == null)
        {
            Debug.LogError($"EnemyInteract of {interactName} component not found on {tree.Me.name}");
        }
    }
    
    protected override void OnEnter()
    {
        base.OnEnter();
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