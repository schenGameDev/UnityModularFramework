using UnityEngine;

public class WanderNode : AstarAINode
{
    [SerializeField] private Vector2 movementBox;
    

    protected override void OnEnter()
    {
        base.OnEnter();
        Vector3 movePos = GetNewDestination();
        tree.AI.SetNewTarget(movePos, BtMove.speed, true);
        BtMove.Move();
    }

    private Vector3 GetNewDestination() {
        float randomX = Random.Range(-movementBox.x, movementBox.x),
            randomY = Random.Range(-movementBox.y, movementBox.y);
        return tree.Me.position + new Vector3(randomX, 0, randomY);
    }

    protected override State OnUpdate()
    {
        if(tree.AI.TargetReached) return State.Success;
        if(tree.AI.PathNotFound) return State.Failure;
        return State.Running;
    }

    public override BTNode Clone() {
        WanderNode node = Instantiate(this);
        return node;
    }

    WanderNode()
    {
        description = "Move randomly in a box";
    }
}
