using UnityEngine;

public class FollowTargetNode : AstarAINode
{
    public bool isStaticTarget = false;
    public bool exitWhenReached = false;
    private Transform _target;

    protected override void OnEnter()
    {
        base.OnEnter();
        var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
        if (targets !=null && targets.Count > 0) 
        {
            _target = targets[0];
            if (isStaticTarget)
            {
                tree.AI.SetNewTarget(GetCloseToMePosition(_target.position), BtMove.speed,true);
            }
            else
            {
                tree.AI.SetNewTarget(_target, BtMove.speed,true);
            }
            
            BtMove.Move();
        }
        else
        {
            _target = null;
        }
    }


    protected override State OnUpdate()
    {
        if(_target == null || tree.AI.PathNotFound) return State.Failure;
        if(exitWhenReached && tree.AI.TargetReached) return State.Success;
        return State.Running;
    }

    public override BTNode Clone() {
        FollowTargetNode node = Instantiate(this);
        return node;
    }
    
    private Vector3 GetCloseToMePosition(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - tree.Me.position;
        direction.y = 0;
        var cc = tree.Me.GetComponent<CharacterController>();
        return targetPosition - direction * (cc? cc.radius : 0.5f);
    }

    FollowTargetNode()
    {
        description = "Follow target \n\n" +
                      "<b>Requires</b>: 'Target' in blackboard";
    }
}
