using UnityEngine;

public class FleeTargetNode : AstarAINode
{
    [SerializeField] private float distance;
    
    private Transform _target;

    protected override void OnEnter()
    {
        base.OnEnter();
        _target = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET)?[0];
        if(_target) {
            tree.AI.SetNewTargetUnFixed(GetFleeTarget(_target.position),BtMove.speed,true);
            BtMove.Move();
        }
    }


    protected override State OnUpdate()
    {
        if(_target == null || tree.AI.PathNotFound) return State.Failure;
        if (Vector3.SqrMagnitude(tree.Me.position - _target.position) > distance * distance)
        {
            return State.Success;
            
        }
        tree.AI.UpdateTarget(GetFleeTarget(_target.position));
        
        return State.Running;
    }

    private Vector3 GetFleeTarget(Vector3 targetToFleeFrom) {
        var dir = (tree.Me.position - targetToFleeFrom).normalized;
        return distance * dir + tree.Me.position;
    }

    public override BTNode Clone() {
        FleeTargetNode node = Instantiate(this);
        return node;
    }

    FleeTargetNode()
    {
        description = "Flee from target \n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
}
