using UnityEngine;

public class WanderNode : AstarAINode
{
    [SerializeField] private Vector2 _movementBox;


    public override string Description() => "Move randomly in a box";

    protected override void OnEnter()
    {
        base.OnEnter();
        Vector3 movePos = GetNewDestination();
        tree.AI.SetNewTarget(movePos, speed, true);

    }

    private Vector3 GetNewDestination() {
        float randomX = Random.Range(-_movementBox.x, _movementBox.x),
            randomY = Random.Range(-_movementBox.y, _movementBox.y);
        return tree.Me.position + new Vector3(randomX, 0, randomY);
    }

    protected override State OnUpdate()
    {
        if(tree.AI.FixedTargetReached) return State.Success;
        if(tree.AI.PathNotFound) return State.Failure;
        return State.Running;
    }

    public override BTNode Clone() {
        WanderNode node = Instantiate(this);
        return node;
    }
}
