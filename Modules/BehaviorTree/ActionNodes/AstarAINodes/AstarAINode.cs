using UnityEngine;

public abstract class AstarAINode : ActionNode {
    [SerializeField] protected float speed;

    protected bool isActive;

    protected override void OnEnter()
    {
        base.OnEnter();
        isActive = true;
    }

    protected override void OnExit()
    {
        tree.AI.Stop();
        isActive = false;
    }
}